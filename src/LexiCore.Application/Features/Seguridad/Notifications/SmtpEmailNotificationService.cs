using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace LexiCore.Application.Features.Seguridad.Notifications
{
    public class SmtpEmailNotificationService : INotificationService
    {
        private readonly IConfiguration _cfg;
        public SmtpEmailNotificationService(IConfiguration cfg) => _cfg = cfg;

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            (string FileName, byte[] Content, string ContentType)? attachment = null)
        {
            // Sección Email en appsettings.json
            var sec = _cfg.GetSection("Email");
            var host = sec["Host"];
            var portStr = sec["Port"];
            var user = sec["User"];
            var pass = sec["Password"]; // tu clave está como "Password"
            var from = sec["From"];
            bool.TryParse(sec["UseStartTls"], out var useStartTls);

            if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Email.Host no configurado.");
            if (!int.TryParse(portStr, out var port) || port <= 0) throw new InvalidOperationException("Email.Port inválido.");
            if (string.IsNullOrWhiteSpace(user)) throw new InvalidOperationException("Email.User no configurado.");
            if (string.IsNullOrWhiteSpace(pass)) throw new InvalidOperationException("Email.Password no configurado.");
            if (string.IsNullOrWhiteSpace(from)) throw new InvalidOperationException("Email.From no configurado.");
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentException("El email de destino está vacío.", nameof(toEmail));

            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder { HtmlBody = htmlBody ?? string.Empty };
            if (attachment.HasValue)
            {
                builder.Attachments.Add(
                    attachment.Value.FileName,
                    attachment.Value.Content,
                    ContentType.Parse(attachment.Value.ContentType)
                );
            }
            msg.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            // 465 => SSL directo; 587 => StartTLS
            SecureSocketOptions security =
                useStartTls ? SecureSocketOptions.StartTls :
                (port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto);

            await smtp.ConnectAsync(host, port, security);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
    }
}
