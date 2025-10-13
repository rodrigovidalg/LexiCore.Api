using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Lexico.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Lexico.Application.Contracts.Email; // Para resolver IEmailService
using Microsoft.Extensions.Logging; // Para logging

namespace Lexico.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly IEmailService _email; // Servicio de correo
        private readonly ILogger<ReportesController> _logger; // Logger para debug

        // Constructor con inyección de dependencias
        public ReportesController(IReportService reports, IEmailService email, ILogger<ReportesController> logger)
        {
            _reports = reports;
            _email = email;
            _logger = logger;
        }

        /// <summary>
        /// Genera y descarga el PDF del análisis para el documento indicado.
        /// </summary>
        [HttpGet("analisis/{documentoId:int}")]
        public async Task<IActionResult> DescargarAnalisis(int documentoId)
        {
            var ct = HttpContext.RequestAborted;
            var pdf = await _reports.GenerarAnalisisPdfAsync(documentoId, ct);
            var fileName = $"analisis_lexico_doc_{documentoId}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        /// <summary>
        /// Genera el PDF del análisis y lo envía por correo.
        /// Toma el email del usuario autenticado (claim 'email' / 'preferred_username' / 'upn')
        /// o se puede indicar explícitamente con ?to=correo@dominio.com
        /// </summary>
        [HttpPost("analisis/{documentoId:int}/enviar")]
        public async Task<IActionResult> EnviarAnalisis(int documentoId, [FromQuery] string? to = null)
        {
            var ct = HttpContext.RequestAborted;

            // Debug: Log del email recibido
            _logger.LogInformation($"Email recibido (raw): '{to}'");
            _logger.LogInformation($"Email length: {to?.Length ?? 0}");
            if (to != null)
            {
                _logger.LogInformation($"Email bytes: {string.Join(",", System.Text.Encoding.UTF8.GetBytes(to).Select(b => b.ToString()))}");
            }

            // 1) Resolver destinatario: claim o querystring
            var emailTo = !string.IsNullOrWhiteSpace(to) ? to : GetUserEmailFromClaims(User);
            
            _logger.LogInformation($"Email final a usar: '{emailTo}'");

            if (string.IsNullOrWhiteSpace(emailTo))
                return BadRequest(new
                {
                    mensaje = "No se encontró el correo del usuario autenticado. " +
                              "Pase ?to=correo@dominio.com o incluya el claim 'email' en el token."
                });

            // 2) Generar PDF
            var pdf = await _reports.GenerarAnalisisPdfAsync(documentoId, ct);
            var filename = $"analisis_lexico_doc_{documentoId}.pdf";

            // 3) Enviar correo
            var subject = $"Reporte de análisis léxico #{documentoId}";
            var body = $@"<p>Hola,</p>
            <p>Adjuntamos el <b>reporte PDF del análisis léxico</b> para el documento <b>#{documentoId}</b>.</p>
            <p>Saludos,</p>
            <p>Lexico API</p>";

            // Enviar email usando el servicio de correo
            await _email.SendAsync(
                to: emailTo,
                subject: subject,
                htmlBody: body,
                attachmentBytes: pdf,
                attachmentName: filename
            );

            return Ok(new { mensaje = "Reporte enviado", documentoId, to = emailTo });
        }

        private static string? GetUserEmailFromClaims(ClaimsPrincipal user)
        {
            var claim = user?.Claims?.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email ||
                c.Type == "email" ||
                c.Type == "preferred_username" ||
                c.Type == "upn");

            return claim?.Value;
        }
    }
}