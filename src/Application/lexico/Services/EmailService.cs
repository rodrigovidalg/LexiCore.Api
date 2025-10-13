using Lexico.Application.Contracts.Email;
using System;
using System.Threading.Tasks;

namespace Lexico.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IEmailSender _sender;

        // Constructor que recibe IEmailSender, ya sea SMTP o SendGrid.
        public EmailService(IEmailSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentName = null,
            string? cc = null)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("El destinatario 'to' es obligatorio.", nameof(to));

            // Si no hay asunto o cuerpo, asignamos un valor por defecto
            subject ??= "(sin asunto)";
            htmlBody ??= "<p>(sin contenido)</p>";

            // Delegamos la tarea de envío al servicio de bajo nivel (SMTP o SendGrid)
            return _sender.SendAsync(
                to: to,
                subject: subject,
                htmlBody: htmlBody,
                attachmentBytes: attachmentBytes,
                attachmentName: attachmentName,
                cc: cc
            );
        }
    }
}
