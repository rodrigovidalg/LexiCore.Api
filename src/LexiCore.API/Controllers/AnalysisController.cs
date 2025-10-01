using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LexiCore.Infrastructure.Persistence;   // AppDbContext
using LexiCore.Domain.Entities;             // Archivo, Analisis
using LexiCore.Application.Services;        // AnalysisService

namespace LexiCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AnalysisService _service;

        public AnalysisController(AppDbContext context)
        {
            _context = context;
            _service = new AnalysisService();
        }

        [HttpPost("{archivoId}")]
        public async Task<IActionResult> Procesar(int archivoId, [FromQuery] string idioma = "es")
        {
            var archivo = await _context.Archivos.FindAsync(archivoId);
            if (archivo == null)
                return NotFound("Archivo no encontrado.");

            var analisis = _service.ProcesarTexto(archivo, idioma);

            _context.Analisis.Add(analisis);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = $"Análisis realizado en idioma {idioma} ✅",
                analisis.TotalPalabras,
                analisis.PalabrasFrecuentes,
                analisis.PalabrasRaras,
                analisis.Pronombres,
                analisis.Verbos,
                analisis.Sustantivos
            });
        }

        [HttpGet("{archivoId}")]
        public IActionResult Obtener(int archivoId)
        {
            var analisis = _context.Analisis
                                   .Where(a => a.ArchivoId == archivoId)
                                   .OrderByDescending(a => a.FechaAnalisis)
                                   .FirstOrDefault();

            if (analisis == null)
                return NotFound("No existe análisis para este archivo.");

            return Ok(analisis);
        }
    }
}
