using LexiCore.Domain.Entities;
using System.Text.RegularExpressions;

namespace LexiCore.Application.Services
{
    /// <summary>
    /// Servicio encargado de realizar el análisis léxico de un archivo de texto.
    /// Soporta múltiples idiomas (español, inglés, ruso).
    /// </summary>
    public class AnalysisService
    {
        public Analisis ProcesarTexto(Archivo archivo, string idioma = "es")
        {
            if (archivo == null || string.IsNullOrWhiteSpace(archivo.Contenido))
                throw new ArgumentException("El archivo está vacío o no contiene texto.");

            var palabras = archivo.Contenido
                .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?', '\"', '(', ')', '[', ']', '{', '}' },
                        StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.ToLower().Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            int totalPalabras = palabras.Count;

            var masFrecuentes = palabras
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var menosFrecuentes = palabras
                .GroupBy(p => p)
                .OrderBy(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var pronombres = DetectarPronombres(archivo.Contenido, idioma);
            var verbos = DetectarVerbos(archivo.Contenido, idioma);
            var sustantivos = DetectarSustantivos(palabras, idioma);

            return new Analisis
            {
                ArchivoId = archivo.Id,
                TotalPalabras = totalPalabras,
                PalabrasFrecuentes = string.Join(", ", masFrecuentes),
                PalabrasRaras = string.Join(", ", menosFrecuentes),
                Pronombres = string.Join(", ", pronombres),
                Verbos = string.Join(", ", verbos),
                Sustantivos = string.Join(", ", sustantivos),
                FechaAnalisis = DateTime.UtcNow
            };
        }

        private List<string> DetectarPronombres(string texto, string idioma)
        {
            string pattern = idioma switch
            {
                "es" => @"\b(yo|tú|vos|usted|él|ella|nosotros|nosotras|vosotros|vosotras|ustedes|ellos|ellas)\b",
                "en" => @"\b(i|you|he|she|it|we|they)\b",
                "ru" => @"\b(я|ты|он|она|оно|мы|вы|они)\b",
                _ => @"\b\w+\b"
            };

            return Regex.Matches(texto, pattern, RegexOptions.IgnoreCase)
                        .Select(m => m.Value.ToLower())
                        .Distinct()
                        .ToList();
        }

        private List<string> DetectarVerbos(string texto, string idioma)
        {
            string pattern = idioma switch
            {
                "es" => @"\b\w+(ar|er|ir)\b",
                "en" => @"\b\w+(ing|ed)\b",
                "ru" => @"\b\w+(ть|л|ют|ем)\b",
                _ => @"\b\w+\b"
            };

            return Regex.Matches(texto, pattern, RegexOptions.IgnoreCase)
                        .Select(m => m.Value.ToLower())
                        .Distinct()
                        .Take(10)
                        .ToList();
        }

        private List<string> DetectarSustantivos(List<string> palabras, string idioma)
        {
            return idioma switch
            {
                "es" => palabras.Where(p => p.EndsWith("o") || p.EndsWith("a")).Distinct().Take(10).ToList(),
                "en" => palabras.Where(p => p.EndsWith("tion") || p.EndsWith("ness")).Distinct().Take(10).ToList(),
                "ru" => palabras.Where(p => p.EndsWith("ия") || p.EndsWith("ость")).Distinct().Take(10).ToList(),
                _ => palabras.Distinct().Take(10).ToList()
            };
        }
    }
}
