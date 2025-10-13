namespace Lexico.Application.Contracts.Email
{
    public interface IEmailService
    {
        Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentName = null,
            string? ct = null);
    }
}
