using LexiCore.Application.Contracts.Seguridad;

namespace LexiCore.Application.Features.Seguridad
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest dto);
        Task<AuthResponse> LoginAsync(LoginRequest dto);
        Task LogoutAsync(string bearerToken);
    }
}
