namespace LexiCore.Application.Contracts.Seguridad;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public string? Telefono { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
    public UsuarioDto Usuario { get; set; } = default!;
}
