using Microsoft.AspNetCore.Mvc;
using ProyectoAnalisisLexico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ProyectoAnalisisLexico.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestConexionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestConexionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                // Probar si la conexión responde con un SELECT sencillo
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new
                    {
                        Estado = "OK",
                        Mensaje = "Conexión a MySQL exitosa 🚀",
                        Servidor = _context.Database.GetDbConnection().DataSource,
                        BaseDeDatos = _context.Database.GetDbConnection().Database
                    });
                }

                return StatusCode(500, new
                {
                    Estado = "Error",
                    Mensaje = "No se pudo conectar a MySQL ❌"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Estado = "Error",
                    Mensaje = "Fallo al intentar conectar a MySQL",
                    Detalle = ex.Message
                });
            }
        }
    }
}
