using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LexiCore.Domain.Entities;

[Index(nameof(UsuarioNombre), IsUnique = true, Name = "idx_usuario")]
[Index(nameof(Email),        IsUnique = true, Name = "idx_email")]
[Index(nameof(Activo),                       Name = "idx_activo")]
public class Usuario
{
    [Key] 
    public int Id { get; set; }

    // ==== Datos de acceso / perfil (usa lo de C para no romper Auth) ====
    [Required, StringLength(50)]
    public string UsuarioNombre { get; set; } = default!;   // antes: 'usuario'

    [Required, StringLength(100)]
    public string Email { get; set; } = default!;

    [Required, StringLength(150)]
    public string NombreCompleto { get; set; } = default!;  // C usaba NombreCompleto

    // COMPAT con el código de R que esperaba "Nombre"
    [NotMapped]
    public string Nombre => NombreCompleto;

    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = default!;

    [StringLength(20)]
    public string? Telefono { get; set; }

    // Fechas/estado (unificamos convenciones de C)
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // COMPAT con el modelo de R que tenía "FechaRegistro"
    [NotMapped]
    public DateTime FechaRegistro => FechaCreacion;

    public bool Activo { get; set; } = true;

    // ==== Navegaciones (C) ====
    public ICollection<AutenticacionFacial> AutenticacionesFaciales { get; set; } = new List<AutenticacionFacial>();
    public ICollection<CodigoQr>            CodigosQr               { get; set; } = new List<CodigoQr>();
    public ICollection<MetodoNotificacion>  MetodosNotificacion     { get; set; } = new List<MetodoNotificacion>();
    public ICollection<Sesion>              Sesiones                { get; set; } = new List<Sesion>();
}
