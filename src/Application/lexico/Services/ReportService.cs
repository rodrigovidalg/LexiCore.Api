using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lexico.Application.Contracts;
using Lexico.Application.Rules;
using Lexico.Domain.Entities;

// QuestPDF
using QuestPDF.Fluent;         // Document.Create, .Text(), .Table(), etc.
using QuestPDF.Helpers;        // Colors
using QuestPDF.Infrastructure; // IContainer, LicenseType, PageSizes

namespace Lexico.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IDocumentoRepository _docs;
        private readonly IConfiguracionAnalisisRepository _cfg;

        public ReportService(IDocumentoRepository docs, IConfiguracionAnalisisRepository cfg)
        {
            _docs = docs;
            _cfg  = cfg;
        }

        public async Task<byte[]> GenerarAnalisisPdfAsync(int documentoId, CancellationToken ct = default)
        {
            var doc = await _docs.GetByIdAsync(documentoId);
            if (doc == null)
                throw new InvalidOperationException($"Documento {documentoId} no encontrado.");

            var texto = doc.ContenidoOriginal ?? string.Empty;

            // Determinar idioma ISO (fallback: es)
            var iso = doc.IdiomaId switch
            {
                2 => "en",
                3 => "ru",
                1 => "es",
                _ => "es"
            };
            var rules = RegexRulesFactory.For(iso);

            // Stopwords desde BD (si no hay, set vacío)
            var stop = await _cfg.GetStopwordsAsync(doc.IdiomaId);

            // Conteos
            var (counter, total, unicas) = ContarPalabras(texto, rules.WordRegex);

            // Top / Low (filtrando stopwords)
            var sinStop = counter.Where(kv => !stop.Contains(kv.Key));
            var top = sinStop.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(30).ToList();
            var low = sinStop.Where(kv => kv.Value == 1).OrderBy(kv => kv.Key).Take(30).ToList();

            // Clasificación gramatical simple
            var clasif = Clasificar(counter, rules);
            var pronombres  = clasif.Where(x => x.Categoria == "pronombre_personal").OrderByDescending(x => x.Frecuencia).ThenBy(x => x.Palabra).ToList();
            var verbos      = clasif.Where(x => x.Categoria == "verbo").OrderByDescending(x => x.Frecuencia).ThenBy(x => x.Palabra).ToList();
            var sustantivos = clasif.Where(x => x.Categoria == "sustantivo").OrderByDescending(x => x.Frecuencia).ThenBy(x => x.Palabra).ToList();

            // Patrones útiles
            var patrones = DetectarPatrones(texto, rules);

            // ============================
            // Construir PDF con QuestPDF
            // ============================
            QuestPDF.Settings.License = LicenseType.Community;

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header (aparece en todas las páginas)
                    page.Header().Row(row =>
                    {
                        // Lado izquierdo: Universidad + subtítulo
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Universidad Mariano Gálvez de Guatemala").FontSize(20).Bold();
                            col.Item().Text($"Lexico Report — Análisis léxico del documento #{documentoId}").FontSize(12).Light();
                        });

                        // Lado derecho: “UMG” como marca lateral en cada página
                        row.ConstantItem(80).AlignRight().Column(c =>
                        {
                            c.Item().Text("UMG").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Portada resumida
                        col.Item().BorderBottom(1).PaddingBottom(10).Column(section =>
                        {
                            section.Item().Text((doc.NombreArchivo ?? $"Documento {documentoId}")).FontSize(16).Bold();

                            // Metadatos: Idioma y Longitud (se eliminó UsuarioId)
                            section.Item().Element(e =>
                                e.DefaultTextStyle(s => s.FontSize(10).FontColor(Colors.Grey.Darken2))
                                 .Text(t =>
                                 {
                                     t.Span("Idioma: ").SemiBold();
                                     t.Span(iso.ToUpper());

                                     t.Span("   ·   Longitud: ").SemiBold();
                                     t.Span((texto.Length).ToString("N0"));
                                 })
                            );
                        });

                        // Métricas principales
                        col.Item().PaddingVertical(8).Background(Colors.Grey.Lighten4).Padding(10).Column(m =>
                        {
                            m.Item().Text("Métricas generales").Bold().FontSize(13);
                            m.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Total de palabras: ").SemiBold();
                                    t.Span(total.ToString("N0"));
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Palabras únicas: ").SemiBold();
                                    t.Span(unicas.ToString("N0"));
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Stopwords usadas: ").SemiBold();
                                    t.Span(stop.Count.ToString("N0"));
                                });
                            });
                        });

                        // Top Frecuentes
                        col.Item().PaddingTop(10).Column(s =>
                        {
                            s.Item().Text("Top 30 palabras (sin stopwords)").Bold().FontSize(13);
                            s.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(40);
                                    c.RelativeColumn(6);
                                    c.ConstantColumn(80);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(CellHeader).Text(" # ");
                                    h.Cell().Element(CellHeader).Text("Palabra");
                                    h.Cell().Element(CellHeader).Text("Frecuencia");
                                });

                                var index = 1;
                                foreach (var kv in top)
                                {
                                    table.Cell().Element(Cell).Text(index.ToString());
                                    table.Cell().Element(Cell).Text(kv.Key);
                                    table.Cell().Element(Cell).Text(kv.Value.ToString());
                                    index++;
                                }
                            });
                        });

                        // Menos frecuentes (hapax)
                        col.Item().PaddingTop(10).Column(s =>
                        {
                            s.Item().Text("Palabras con frecuencia 1 (muestra 30)").Bold().FontSize(13);
                            s.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(40);
                                    c.RelativeColumn(6);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(CellHeader).Text(" # ");
                                    h.Cell().Element(CellHeader).Text("Palabra");
                                });

                                var index = 1;
                                foreach (var kv in low)
                                {
                                    table.Cell().Element(Cell).Text(index.ToString());
                                    table.Cell().Element(Cell).Text(kv.Key);
                                    index++;
                                }
                            });
                        });

                        // Clasificaciones
                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Pronombres personales").Bold().FontSize(13);
                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(80);
                                    });
                                    t.Header(h =>
                                    {
                                        h.Cell().Element(CellHeader).Text("Palabra");
                                        h.Cell().Element(CellHeader).Text("Frecuencia");
                                    });
                                    foreach (var x in pronombres.Take(25))
                                    {
                                        t.Cell().Element(Cell).Text(x.Palabra);
                                        t.Cell().Element(Cell).Text(x.Frecuencia.ToString());
                                    }
                                });
                            });

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Verbos (forma raíz aprox.)").Bold().FontSize(13);
                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(80);
                                    });
                                    t.Header(h =>
                                    {
                                        h.Cell().Element(CellHeader).Text("Palabra");
                                        h.Cell().Element(CellHeader).Text("Frecuencia");
                                    });
                                    foreach (var x in verbos.Take(25))
                                    {
                                        t.Cell().Element(Cell).Text(x.Palabra);
                                        t.Cell().Element(Cell).Text(x.Frecuencia.ToString());
                                    }
                                });
                            });

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Sustantivos (aprox.)").Bold().FontSize(13);
                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(80);
                                    });
                                    t.Header(h =>
                                    {
                                        h.Cell().Element(CellHeader).Text("Palabra");
                                        h.Cell().Element(CellHeader).Text("Frecuencia");
                                    });
                                    foreach (var x in sustantivos.Take(25))
                                    {
                                        t.Cell().Element(Cell).Text(x.Palabra);
                                        t.Cell().Element(Cell).Text(x.Frecuencia.ToString());
                                    }
                                });
                            });
                        });

                        // Patrones
                        col.Item().PaddingTop(10).Column(s =>
                        {
                            s.Item().Text("Patrones detectados").Bold().FontSize(13);

                            void PatternTable(string title, IEnumerable<string> items)
                            {
                                s.Item().PaddingTop(4).Text(title).SemiBold();
                                s.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(40);
                                        cols.RelativeColumn();
                                    });
                                    t.Header(h =>
                                    {
                                        h.Cell().Element(CellHeader).Text(" # ");
                                        h.Cell().Element(CellHeader).Text("Coincidencia");
                                    });
                                    int i = 1;
                                    foreach (var v in items.Take(50))
                                    {
                                        t.Cell().Element(Cell).Text(i.ToString());
                                        t.Cell().Element(Cell).Text(v);
                                        i++;
                                    }
                                });
                            }

                            PatternTable("Emails",    patrones.Emails);
                            PatternTable("URLs",      patrones.Urls);
                            PatternTable("Fechas",    patrones.Fechas);
                            PatternTable("Monedas",   patrones.Dinero);
                            PatternTable("Hashtags",  patrones.Hashtags);
                            PatternTable("Menciones", patrones.Menciones);
                            PatternTable("Nombres de persona", patrones.NombresPersona);
                        });

                        // Nota
                        col.Item().PaddingTop(8)
                           .Text("Nota: la clasificación es heurística basada en expresiones regulares por idioma.")
                           .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    // Footer (paginación)
                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();

            return bytes; // Devuelves el byte[]
        }

        // ======================= Helpers de análisis =======================
        private static (Dictionary<string,int> Counter, int Total, int Unicas) ContarPalabras(string texto, string wordRegex)
        {
            var counter = new Dictionary<string, int>(16_384, StringComparer.OrdinalIgnoreCase);
            var opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline;

            foreach (Match m in Regex.Matches(texto ?? string.Empty, wordRegex, opts))
            {
                var t = m.Value.Trim().ToLowerInvariant();
                if (t.Length == 0) continue;
                if (counter.TryGetValue(t, out var c)) counter[t] = c + 1;
                else counter[t] = 1;
            }

            int total = counter.Values.Sum();
            int unicas = counter.Count;
            return (counter, total, unicas);
        }

        private static List<Clasif> Clasificar(IDictionary<string, int> counter, IRegexRules rules)
        {
            var list = new List<Clasif>();
            var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            foreach (var kv in counter)
            {
                var w = kv.Key;
                var f = kv.Value;

                if (Regex.IsMatch(w, rules.PronounsRegex, options))
                {
                    list.Add(new Clasif { Palabra = w, Categoria = "pronombre_personal", Frecuencia = f });
                    continue;
                }
                if (Regex.IsMatch(w, rules.VerbRegex, options))
                    list.Add(new Clasif { Palabra = w, Categoria = "verbo", Frecuencia = f });

                if (Regex.IsMatch(w, rules.NounRegex, options))
                    list.Add(new Clasif { Palabra = w, Categoria = "sustantivo", Frecuencia = f });
            }
            return list;
        }

        private static (IEnumerable<string> Emails,
                        IEnumerable<string> Urls,
                        IEnumerable<string> Fechas,
                        IEnumerable<string> Dinero,
                        IEnumerable<string> Hashtags,
                        IEnumerable<string> Menciones,
                        IEnumerable<string> NombresPersona)
            DetectarPatrones(string texto, IRegexRules rules)
        {
            var opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            IEnumerable<string> Extract(string pattern)
                => Regex.Matches(texto ?? string.Empty, pattern, opts).Cast<Match>().Select(m => m.Value).Distinct().Take(200);

            return (Extract(rules.EmailRegex),
                    Extract(rules.UrlRegex),
                    Extract(rules.DateRegex),
                    Extract(rules.MoneyRegex),
                    Extract(rules.HashtagRegex),
                    Extract(rules.MentionRegex),
                    Extract(rules.PersonNameRegex));
        }

        // ======================= DTOs / estilos =======================
        private class Clasif
        {
            public string Palabra { get; set; } = "";
            public string Categoria { get; set; } = "";
            public int Frecuencia { get; set; }
        }

        // Estilos de celda (para tablas QuestPDF)
        private static IContainer Cell(IContainer c) => c.BorderBottom(1).PaddingVertical(4).PaddingRight(4);
        private static IContainer CellHeader(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4).BorderBottom(1);
    }
}
