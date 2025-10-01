using Microsoft.AspNetCore.Mvc;
using LexiCore.Infrastructure.Persistence;
using LexiCore.Domain.Entities;

namespace LexiCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UploadController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Subida de un archivo TXT y guardado en la BD.
        /// </summary>
        /// <param name="file">Archivo en formato .txt</param>
        [HttpPost]
        [RequestSizeLimit(5_000_000)] // Máx 5 MB
        public async Task<IActionResult> UploadTxt([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Debe enviar un archivo .txt válido.");

            if (!file.FileName.EndsWith(".txt"))
                return BadRequest("Solo se permiten archivos con extensión .txt.");

            string contenido;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                contenido = await reader.ReadToEndAsync();
            }

            var archivo = new Archivo
            {
                Nombre = file.FileName,
                Contenido = contenido,
                FechaSubida = DateTime.UtcNow,
                UsuarioId = 1 // ⚠️ Temporal hasta tener autenticación real
            };

            _context.Archivos.Add(archivo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "Archivo cargado exitosamente ✅",
                ArchivoId = archivo.Id,
                Nombre = archivo.Nombre,
                FechaSubida = archivo.FechaSubida
            });
        }
    }
}
