using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Lexico.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Lexico.Application.Contracts.Email;
using Microsoft.Extensions.Logging;

namespace Lexico.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly IEmailService _email;
        private readonly ILogger<ReportesController> _logger;

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
            try
            {
                var ct = HttpContext.RequestAborted;
                var pdf = await _reports.GenerarAnalisisPdfAsync(documentoId, ct);
                var fileName = $"analisis_lexico_doc_{documentoId}.pdf";
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al generar PDF para documento {documentoId}");
                return StatusCode(500, new { mensaje = "Error al generar el PDF", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Genera el PDF del análisis y lo envía por correo.
        /// Toma el email del usuario autenticado (claim 'email' / 'preferred_username' / 'upn')
        /// o se puede indicar explícitamente con ?to=correo@dominio.com
        /// </summary>
        [HttpPost("analisis/{documentoId:int}/enviar")]
        public async Task<IActionResult> EnviarAnalisis(int documentoId, [FromQuery] string? to = null)
        {
            try
            {
                var ct = HttpContext.RequestAborted;

                // ✅ Debug mejorado
                _logger.LogInformation($"=== INICIO EnviarAnalisis ===");
                _logger.LogInformation($"DocumentoId: {documentoId}");
                _logger.LogInformation($"Query param 'to' (raw): '{to}'");
                _logger.LogInformation($"Query param 'to' length: {to?.Length ?? 0}");
                
                // ✅ Limpiar espacios en blanco del email
                var emailTo = !string.IsNullOrWhiteSpace(to) 
                    ? to.Trim() 
                    : GetUserEmailFromClaims(User);

                _logger.LogInformation($"Email después de limpieza: '{emailTo}'");

                // ✅ Validación del email
                if (string.IsNullOrWhiteSpace(emailTo))
                {
                    _logger.LogWarning("No se encontró email del usuario");
                    return BadRequest(new
                    {
                        mensaje = "No se encontró el correo del usuario autenticado. " +
                                  "Pase ?to=correo@dominio.com o incluya el claim 'email' en el token."
                    });
                }

                // ✅ Validación básica de formato de email
                if (!IsValidEmail(emailTo))
                {
                    _logger.LogWarning($"Email inválido: '{emailTo}'");
                    return BadRequest(new
                    {
                        mensaje = $"El correo proporcionado no tiene un formato válido: '{emailTo}'"
                    });
                }

                _logger.LogInformation($"Email validado correctamente: '{emailTo}'");

                // ✅ Generar PDF con manejo de errores
                byte[] pdf;
                try
                {
                    pdf = await _reports.GenerarAnalisisPdfAsync(documentoId, ct);
                    _logger.LogInformation($"PDF generado exitosamente. Tamaño: {pdf.Length} bytes");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al generar PDF para documento {documentoId}");
                    return StatusCode(500, new
                    {
                        mensaje = "Error al generar el PDF",
                        detalle = ex.Message
                    });
                }

                var filename = $"analisis_lexico_doc_{documentoId}.pdf";

                // ✅ Enviar correo con manejo de errores
                var subject = $"Reporte de análisis léxico #{documentoId}";
                var body = $@"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2e5e54;'>Reporte de Análisis Léxico</h2>
        <p>Hola,</p>
        <p>Adjuntamos el <strong>reporte PDF del análisis léxico</strong> para el documento <strong>#{documentoId}</strong>.</p>
        <p style='margin-top: 20px;'>Saludos,<br/>
        <strong>Lexico API</strong></p>
    </div>
</body>
</html>";

                try
                {
                    _logger.LogInformation($"Intentando enviar email a: '{emailTo}'");
                    
                    await _email.SendAsync(
                        to: emailTo,
                        subject: subject,
                        htmlBody: body,
                        attachmentBytes: pdf,
                        attachmentName: filename
                    );

                    _logger.LogInformation($"✅ Email enviado exitosamente a '{emailTo}'");
                    
                    return Ok(new 
                    { 
                        mensaje = "Reporte enviado correctamente", 
                        documentoId, 
                        to = emailTo,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al enviar email a '{emailTo}'");
                    return StatusCode(500, new
                    {
                        mensaje = "Error al enviar el correo electrónico",
                        detalle = ex.Message,
                        destinatario = emailTo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error general en EnviarAnalisis para documento {documentoId}");
                return StatusCode(500, new
                {
                    mensaje = "Error inesperado al procesar la solicitud",
                    detalle = ex.Message
                });
            }
        }

        private static string? GetUserEmailFromClaims(ClaimsPrincipal user)
        {
            var claim = user?.Claims?.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email ||
                c.Type == "email" ||
                c.Type == "preferred_username" ||
                c.Type == "upn");

            return claim?.Value?.Trim();
        }

        /// <summary>
        /// Validación básica de formato de email
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }
}