using Seguridad.Api.Transport;

namespace Seguridad.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest dto); //creación de un nuevo usuario
    Task<AuthResponse> LoginAsync(LoginRequest dto); //autentica y emite el JWT
    Task LogoutAsync(string bearerToken); //revoca la sesión asociada al token
}
