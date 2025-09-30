using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Seguridad.Api.Infrastructure;
using Seguridad.Api.Services;
using Seguridad.Api.Services.Notifications;
using Seguridad.Api.Transport;

namespace Seguridad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IQrService _qrService;
    private readonly IQrCardGenerator _card;
    private readonly INotificationService _notify;

    public QrController(
        AppDbContext db,
        IQrService qrService,
        IQrCardGenerator card,
        INotificationService notify)
    {
        _db = db; _qrService = qrService; _card = card; _notify = notify;
    }

    // Envía el carnet con QR al email del usuario
    [Authorize] // quítalo si lo vas a llamar sin token
    [HttpPost("send-card")]
    public async Task<IActionResult> SendCard([FromBody] QrSendRequest dto)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.UsuarioNombre == dto.UsuarioOrEmail || u.Email == dto.UsuarioOrEmail);

        if (user is null || !user.Activo)
            return NotFound("Usuario no encontrado o inactivo.");

        // 1) Obtener/crear código QR
        var qr = await _qrService.GetOrCreateUserQrAsync(user.Id);

        // 2) Generar PDF del carnet con el QR
        var pdf = _card.CreateCardPdf(user.NombreCompleto, user.UsuarioNombre, user.Email, qr.Codigo);

        // 3) Enviar correo
        var subject = "Tu carnet de acceso con código QR";
        var body = $@"
            <p>Hola <b>{user.NombreCompleto}</b>,</p>
            <p>Adjuntamos tu carnet de acceso con código QR.</p>
            <p>Si no solicitaste este correo, por favor contacta a soporte.</p>
        ";

        await _notify.SendEmailAsync(
            user.Email, subject, body,
            ("carnet_qr.pdf", pdf, "application/pdf")
        );

        return Ok(new { message = "Carnet enviado al correo del usuario." });
    }
}
