using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LexiCore.Application.Features.Seguridad;                 // IAuthService
using LexiCore.Application.Contracts.Seguridad;               // RegisterRequest, LoginRequest, AuthResponse

namespace LexiCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) { _auth = auth; }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
    {
        var res = await _auth.RegisterAsync(dto);
        return Ok(res);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        var res = await _auth.LoginAsync(dto);
        return Ok(res);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var bearer = Request.Headers.Authorization.ToString();
        await _auth.LogoutAsync(bearer);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = new
        {
            Id = id,
            Usuario = User.Identity?.Name,
            Email = User.FindFirstValue(ClaimTypes.Email)
        };
        return Ok(user);
    }
}
