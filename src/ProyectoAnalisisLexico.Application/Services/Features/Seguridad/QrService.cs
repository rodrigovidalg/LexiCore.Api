using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Seguridad.Api.Domain.Entities;
using Seguridad.Api.Infrastructure;

namespace Seguridad.Api.Services;

public interface IQrService
{
    Task<CodigoQr> GetOrCreateUserQrAsync(int usuarioId, string? qrContenido = null);
    string ComputeSha256(string input);
}

public class QrService : IQrService
{
    private readonly AppDbContext _db;
    public QrService(AppDbContext db) { _db = db; }

    public async Task<CodigoQr> GetOrCreateUserQrAsync(int usuarioId, string? qrContenido = null)
    {
        var existente = await _db.CodigosQr
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Activo);
        if (existente != null) return existente;

        // Contenido del QR: puedes usar un GUID o un payload JSON/JWT corto
        var contenido = qrContenido ?? $"QR-{Guid.NewGuid():N}-{usuarioId}";
        var hash = ComputeSha256(contenido);

        var nuevo = new CodigoQr
        {
            UsuarioId = usuarioId,
            Codigo = contenido,
            QrHash = hash,
            Activo = true
        };
        _db.CodigosQr.Add(nuevo);
        await _db.SaveChangesAsync();
        return nuevo;
    }

    public string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
