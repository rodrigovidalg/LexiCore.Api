// src/Infrastructure/lexico/Email/EmailOptions.cs
namespace Lexico.Infrastructure.Email;

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 25;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "Lexico API <no-reply@localhost>";
    public bool UseStartTls { get; set; } = false;
}

public class SendGridOptions
{
    public string? ApiKey { get; set; }
    public string From { get; set; } = "Lexico API <no-reply@domain.com>";
}
