using Lexico.Application.Contracts;      // ICurrentUser
using Microsoft.AspNetCore.Http;        // IHttpContextAccessor
using System.Security.Claims;

namespace Lexico.Infrastructure.Data
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUser(IHttpContextAccessor http)
        {
            _http = http;
        }

        public bool IsAuthenticated =>
            _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public string? UserId =>
            _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _http.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _http.HttpContext?.User?.FindFirst("uid")?.Value;

        public string? Email =>
            _http.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _http.HttpContext?.User?.FindFirst("email")?.Value;
    }
}
