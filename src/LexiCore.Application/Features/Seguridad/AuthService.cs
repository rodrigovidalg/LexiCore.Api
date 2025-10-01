using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LexiCore.Domain.Entities;
using LexiCore.Infrastructure.Persistence;

// DTOs
using LexiCore.Application.Contracts.Seguridad;              // RegisterRequest, LoginRequest, AuthResponse, UsuarioDto
using LexiCore.Application.Features.Seguridad;               // IAuthService, IJwtTokenService, IQrService, IQrCardGenerator
using LexiCore.Application.Features.Seguridad.Notifications; // INotificationService

namespace LexiCore.Application.Features.Seguridad
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IQrService _qr;
        private readonly IQrCardGenerator _card;
        private readonly INotificationService _notify;

        public AuthService(
            AppDbContext db,
            IJwtTokenService jwt,
            IQrService qr,
            IQrCardGenerator card,
            INotificationService notify)
        {
            _db = db;
            _jwt = jwt;
            _qr = qr;
            _card = card;
            _notify = notify;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest dto)
        {
            if (await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == dto.Usuario || u.Email == dto.Email))
                throw new InvalidOperationException("Usuario o email ya existen.");

            // BCrypt.Net-Next
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            var user = new Usuario
            {
                UsuarioNombre = dto.Usuario,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                PasswordHash = hash,
                Telefono = dto.Telefono,
                Activo = true
            };

            _db.Usuarios.Add(user);
            await _db.SaveChangesAsync();

            // QR + carnet + email (no bloquea registro si falla)
            try
            {
                var qr = await _qr.GetOrCreateUserQrAsync(user.Id);
                var pdf = _card.CreateCardPdf(user.NombreCompleto, user.UsuarioNombre, user.Email, qr.Codigo);

                var bodyHtml = $@"
                    <p>Hola <b>{user.NombreCompleto}</b>,</p>
                    <p>Adjuntamos tu <b>carnet de acceso con código QR</b>. Guárdalo y preséntalo cuando se te solicite.</p>
                    <p>Si no solicitaste este registro, contacta a soporte.</p>";

                await _notify.SendEmailAsync(
                    user.Email,
                    "Tu carnet de acceso con código QR",
                    bodyHtml,
                    ("carnet_qr.pdf", pdf, "application/pdf")
                );
            }
            catch
            {
                // TODO: ILogger<AuthService> si quieres loguear
            }

            return await LoginInternalAsync(user, MetodoLogin.password);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest dto)
        {
            var user = await _db.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.UsuarioNombre == dto.UsuarioOrEmail || u.Email == dto.UsuarioOrEmail);

            if (user is null || !user.Activo)
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!ok) throw new UnauthorizedAccessException("Credenciales inválidas.");

            return await LoginInternalAsync(user, MetodoLogin.password);
        }

        public async Task LogoutAsync(string bearerToken)
        {
            var token = bearerToken?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? bearerToken[7..].Trim()
                : bearerToken?.Trim();

            if (string.IsNullOrWhiteSpace(token)) return;

            var hash = _jwt.ComputeSha256(token);
            var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.SessionTokenHash == hash && s.Activa);
            if (sesion != null)
            {
                sesion.Activa = false;
                await _db.SaveChangesAsync();
            }
        }

        private async Task<AuthResponse> LoginInternalAsync(Usuario user, MetodoLogin metodo)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UsuarioNombre),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var (token, jti) = _jwt.CreateToken(claims);
            var tokenHash = _jwt.ComputeSha256(token);

            _db.Sesiones.Add(new Sesion
            {
                UsuarioId = user.Id,
                SessionTokenHash = tokenHash,
                MetodoLogin = metodo,
                Activa = true
            });

            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = token,
                ExpiresInSeconds = 60 * 60,
                Usuario = new UsuarioDto
                {
                    Id = user.Id,
                    Usuario = user.UsuarioNombre,
                    Email = user.Email,
                    NombreCompleto = user.NombreCompleto,
                    Telefono = user.Telefono
                }
            };
        }
    }
}
