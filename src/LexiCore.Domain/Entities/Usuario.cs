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

    // ==== Datos de acceso / perfil (de C) ====
    [Required, StringLength(50)]
    public string UsuarioNombre { get; set; } = default!;   // columna "Usuario" (migración incremental)

    [Required, StringLength(100)]
    public string Email { get; set; } = default!;

    [Required, StringLength(150)]
    public string NombreCompleto { get; set; } = default!;  // mapeado a columna "Nombre"

    // COMPAT: algunos controladores viejos de R podían leer "Nombre"
    [NotMapped]
    public string Nombre => NombreCompleto;

    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = default!;

    [StringLength(20)]
    public string? Telefono { get; set; }

    // C usa FechaCreacion; la migración de R creó "FechaRegistro".
    // En el DbContext mapeamos FechaCreacion -> "FechaRegistro".
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // COMPAT para código que aún consulte "FechaRegistro"
    [NotMapped]
    public DateTime FechaRegistro => FechaCreacion;

    public bool Activo { get; set; } = true; // columna "Activo" (migración incremental)

    // ==== Navegaciones (C) ====
    public ICollection<AutenticacionFacial> AutenticacionesFaciales { get; set; } = new List<AutenticacionFacial>();
    public ICollection<CodigoQr>            CodigosQr               { get; set; } = new List<CodigoQr>();
    public ICollection<MetodoNotificacion>  MetodosNotificacion     { get; set; } = new List<MetodoNotificacion>();
    public ICollection<Sesion>              Sesiones                { get; set; } = new List<Sesion>();
}
