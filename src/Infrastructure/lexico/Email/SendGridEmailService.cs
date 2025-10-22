using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Lexico.Application.Contracts.Email;

namespace Lexico.Infrastructure.Email
{
    /// <summary>
    /// Implementación de email usando SendGrid API
    /// Compatible con Railway (no usa SMTP bloqueado)
    /// </summary>
    public class SendGridEmailService : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(string apiKey, string fromEmail, string fromName = "Lexico API")
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
            _fromName = fromName;
        }

        public async Task SendAsync(
            string to, 
            string subject, 
            string htmlBody, 
            byte[]? attachmentBytes = null, 
            string? attachmentName = null,
            string? cc = null)  // ✅ Agregado el parámetro cc
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var toAddress = new EmailAddress(to);
                
                var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlBody);

                // ✅ Agregar CC si existe
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    msg.AddCc(new EmailAddress(cc));
                }

                // Agregar adjunto si existe
                if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentName))
                {
                    var base64Content = Convert.ToBase64String(attachmentBytes);
                    msg.AddAttachment(attachmentName, base64Content);
                }

                var response = await client.SendEmailAsync(msg);

                // Verificar respuesta
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    throw new Exception($"SendGrid error ({response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al enviar email con SendGrid: {ex.Message}", ex);
            }
        }
    }
}