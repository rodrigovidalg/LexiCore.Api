using Microsoft.EntityFrameworkCore;
using LexiCore.Domain.Entities;

namespace LexiCore.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    // DbSets (todas las entidades)
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AutenticacionFacial> AutenticacionFacial => Set<AutenticacionFacial>();
    public DbSet<CodigoQr> CodigosQr => Set<CodigoQr>();
    public DbSet<MetodoNotificacion> MetodosNotificacion => Set<MetodoNotificacion>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();

    public DbSet<Archivo> Archivos => Set<Archivo>();
    public DbSet<Analisis> Analisis => Set<Analisis>();
    public DbSet<Reporte> Reportes => Set<Reporte>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // (Opcional) Charset/Collation
        mb.HasCharSet("utf8mb4").UseCollation("utf8mb4_0900_ai_ci");

        // ====== USUARIOS (creada por la migración: "Usuarios") ======
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");

            e.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();

            // Columnas que YA existen por la migración:
            e.Property(p => p.Email).HasColumnName("Email");
            e.Property(p => p.PasswordHash).HasColumnName("PasswordHash");
            e.Property(p => p.Telefono).HasColumnName("Telefono");
            // Mapeamos propiedades de C a las columnas reales de la migración de R:
            e.Property(p => p.NombreCompleto).HasColumnName("Nombre");
            e.Property(p => p.FechaCreacion).HasColumnName("FechaRegistro");

            // Columnas NUEVAS que añadiremos con una migración incremental:
            e.Property(p => p.UsuarioNombre).HasColumnName("Usuario").HasMaxLength(50);
            e.Property(p => p.Activo).HasColumnName("Activo");
        });

        // ====== ARCHIVOS (creada por la migración: "Archivos") ======
        mb.Entity<Archivo>(e =>
        {
            e.ToTable("Archivos");

            e.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(p => p.Nombre).HasColumnName("Nombre");
            e.Property(p => p.Ruta).HasColumnName("Ruta");
            e.Property(p => p.Contenido).HasColumnName("Contenido").HasColumnType("LONGTEXT");
            e.Property(p => p.FechaSubida).HasColumnName("FechaSubida");
            e.Property(p => p.UsuarioId).HasColumnName("UsuarioId");

            e.HasOne(p => p.Usuario)
             .WithMany()
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== ANALISIS (creada por la migración: "Analisis") ======
        mb.Entity<Analisis>(e =>
        {
            e.ToTable("Analisis");

            e.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(p => p.ArchivoId).HasColumnName("ArchivoId");
            e.Property(p => p.TotalPalabras).HasColumnName("TotalPalabras");
            e.Property(p => p.PalabrasFrecuentes).HasColumnName("PalabrasFrecuentes");
            e.Property(p => p.PalabrasRaras).HasColumnName("PalabrasRaras");
            e.Property(p => p.Pronombres).HasColumnName("Pronombres");
            e.Property(p => p.Verbos).HasColumnName("Verbos");
            e.Property(p => p.Sustantivos).HasColumnName("Sustantivos");
            e.Property(p => p.FechaAnalisis).HasColumnName("FechaAnalisis");

            e.HasOne(p => p.Archivo)
             .WithMany()
             .HasForeignKey(p => p.ArchivoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== REPORTES (creada por la migración: "Reportes") ======
        mb.Entity<Reporte>(e =>
        {
            e.ToTable("Reportes");

            e.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(p => p.AnalisisId).HasColumnName("AnalisisId");
            e.Property(p => p.RutaPdf).HasColumnName("RutaPdf");
            e.Property(p => p.FechaGeneracion).HasColumnName("FechaGeneracion");
            e.Property(p => p.MedioNotificacion).HasColumnName("MedioNotificacion");
            e.Property(p => p.Destino).HasColumnName("Destino");

            e.HasOne(p => p.Analisis)
             .WithMany()
             .HasForeignKey(p => p.AnalisisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Las entidades de seguridad (AutenticacionFacial, CodigoQr, MetodoNotificacion, Sesion)
        // las dejamos por convención (sin configuración específica) y luego creamos migración
        // cuando toque habilitarlas. No afecta la prueba de /api/upload.
    }
}
