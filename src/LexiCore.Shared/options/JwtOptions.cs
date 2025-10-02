using LexiCore.Shared.Options; // <-- para JwtOptions


namespace LexiCore.Shared.Options
{
    public class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "lexicore";
        public string? Audience { get; set; }
        public int AccessTokenMinutes { get; set; } = 60;
    }
}
