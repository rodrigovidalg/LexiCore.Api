using ProyectoAnalisisLexico.Domain.Entities;
using System.Text.RegularExpressions;

namespace ProyectoAnalisisLexico.Application.Services
{
    /// <summary>
    /// Servicio encargado de realizar el análisis léxico de un archivo de texto.
    /// Soporta múltiples idiomas (español, inglés, ruso).
    /// </summary>
    public class AnalysisService
    {
        /// <summary>
        /// Procesa el texto de un archivo y genera estadísticas léxicas.
        /// </summary>
        /// <param name="archivo">Archivo que contiene el texto a analizar.</param>
        /// <param name="idioma">Idioma del texto (es, en, ru).</param>
        /// <returns>Objeto Analisis con métricas del texto.</returns>
        public Analisis ProcesarTexto(Archivo archivo, string idioma = "es")
        {
            if (archivo == null || string.IsNullOrWhiteSpace(archivo.Contenido))
                throw new ArgumentException("El archivo está vacío o no contiene texto.");

            // --- 1. Normalización del texto ---
            var palabras = archivo.Contenido
                .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?', '\"', '(', ')', '[', ']', '{', '}' },
                        StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.ToLower().Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            int totalPalabras = palabras.Count;

            // --- 2. Palabras más frecuentes ---
            var masFrecuentes = palabras
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            // --- 3. Palabras menos frecuentes ---
            var menosFrecuentes = palabras
                .GroupBy(p => p)
                .OrderBy(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            // --- 4. Diccionarios por idioma ---
            var pronombres = DetectarPronombres(archivo.Contenido, idioma);
            var verbos = DetectarVerbos(archivo.Contenido, idioma);
            var sustantivos = DetectarSustantivos(palabras, idioma);

            // --- 5. Construcción del resultado ---
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

        // -----------------------------------------------------------------------
        // Métodos auxiliares para detección léxica
        // -----------------------------------------------------------------------

        /// <summary>
        /// Detecta pronombres personales según el idioma.
        /// </summary>
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

        /// <summary>
        /// Detecta verbos en forma raíz simple (heurística básica por idioma).
        /// </summary>
        private List<string> DetectarVerbos(string texto, string idioma)
        {
            string pattern = idioma switch
            {
                "es" => @"\b\w+(ar|er|ir)\b",          // infinitivos en español
                "en" => @"\b\w+(ing|ed)\b",            // gerundio/pasado simple en inglés
                "ru" => @"\b\w+(ть|л|ют|ем)\b",        // terminaciones típicas en ruso
                _ => @"\b\w+\b"
            };

            return Regex.Matches(texto, pattern, RegexOptions.IgnoreCase)
                        .Select(m => m.Value.ToLower())
                        .Distinct()
                        .Take(10)
                        .ToList();
        }

        /// <summary>
        /// Detecta sustantivos (heurística básica).
        /// </summary>
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
