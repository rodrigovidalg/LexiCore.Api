using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace LexiCore.Application.Features.Seguridad
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        private readonly SymmetricSecurityKey _key;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _opt = options.Value;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        }

        public (string token, string jti) CreateToken(IEnumerable<Claim> claims)
        {
            var jti = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,                      // puede ser null si no validas Audience
                claims: claims.Append(new Claim(JwtRegisteredClaimNames.Jti, jti)),
                notBefore: now,
                expires: now.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwt, jti);
        }

        public string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}
