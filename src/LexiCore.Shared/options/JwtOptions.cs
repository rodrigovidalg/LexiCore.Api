namespace LexiCore.Application.Features.Seguridad
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = "lexicore";
        public string? Audience { get; set; }           // opcional si no validas audiencia
        public string Key { get; set; } = default!;
        public int AccessTokenMinutes { get; set; } = 60;
    }
}
