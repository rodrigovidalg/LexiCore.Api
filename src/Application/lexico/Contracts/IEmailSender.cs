namespace Lexico.Application.Contracts.Email
{
    public interface IEmailSender
    {
        Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentName = null,
            string? cc = null);
    }
}
