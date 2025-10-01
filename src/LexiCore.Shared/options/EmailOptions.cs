// src/LexiCore.Shared/Options/EmailOptions.cs
namespace LexiCore.Shared.Options;

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;          // 587 (StartTLS) por defecto
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // usamos "Password" (no "Pass")
    public string From { get; set; } = string.Empty;     // "Nombre <correo@dominio>"
    public bool UseStartTls { get; set; } = true;        // true para 587; si usas 465 => false
}
