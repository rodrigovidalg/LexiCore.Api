namespace LexiCore.Application.Contracts.Seguridad;

public class RegisterRequest
{
    public string Usuario { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Telefono { get; set; }
}

public class LoginRequest
{
    public string UsuarioOrEmail { get; set; } = default!;
    public string Password { get; set; } = default!;
}
