using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Lexico.Application.Services.Email;
using Lexico.Application.Contracts.Email;

namespace Lexico.Infrastructure.Email
{
    public sealed class SmtpEmailService : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string? _user;
        private readonly string? _pass;
        private readonly string _from;
        private readonly bool _useStartTls;

        public SmtpEmailService(string host, int port, string? user, string? pass, string from, bool useStartTls)
        {
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
            _from = from;
            _useStartTls = useStartTls;
        }

        public async Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes,
            string? attachmentName,
            string? cc)
        {
            // Validar que el email 'to' no sea nulo o vacío
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("El email destinatario (to) no puede estar vacío.", nameof(to));

            using var message = new MailMessage
            {
                From = new MailAddress(_from),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            // Agregar el destinatario usando el constructor de MailAddress
            message.To.Add(new MailAddress(to.Trim()));

            // Agregar CC si está especificado
            if (!string.IsNullOrWhiteSpace(cc))
                message.CC.Add(new MailAddress(cc.Trim()));

            // Agregar adjunto si está disponible
            if (attachmentBytes is not null && attachmentBytes.Length > 0)
            {
                var name = string.IsNullOrWhiteSpace(attachmentName) ? "attachment.bin" : attachmentName!.Trim();
                var stream = new MemoryStream(attachmentBytes);
                var attachment = new Attachment(stream, name);
                message.Attachments.Add(attachment);
            }

            // Configurar cliente SMTP
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _useStartTls
            };

            // Agregar credenciales si están disponibles
            if (!string.IsNullOrWhiteSpace(_user))
            {
                client.Credentials = new NetworkCredential(_user, _pass ?? string.Empty);
            }

            // Enviar el email
            await client.SendMailAsync(message);
        }
    }
}