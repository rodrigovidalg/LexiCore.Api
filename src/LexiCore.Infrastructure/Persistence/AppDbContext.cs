using Microsoft.EntityFrameworkCore;
using LexiCore.Domain.Entities;

namespace LexiCore.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    // DbSets
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

        // Charset/Collation (Pomelo MySQL)
        mb.HasCharSet("utf8mb4").UseCollation("utf8mb4_0900_ai_ci");

        // ===========================
        //  Esquema estilo "R" (PascalCase)
        // ===========================

        // USUARIOS
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();

            // columnas existentes (R)
            e.Property(p => p.Email).HasColumnName("Email");
            e.Property(p => p.PasswordHash).HasColumnName("PasswordHash");
            e.Property(p => p.Telefono).HasColumnName("Telefono");

            // mapeos compatibilidad (C -> R)
            e.Property(p => p.NombreCompleto).HasColumnName("Nombre");
            e.Property(p => p.FechaCreacion).HasColumnName("FechaRegistro");

            // columnas nuevas
            e.Property(p => p.UsuarioNombre).HasColumnName("Usuario").HasMaxLength(50);
            e.Property(p => p.Activo).HasColumnName("Activo");
        });

        // ARCHIVOS
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

        // ANALISIS
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

        // REPORTES
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

        // ===========================
        //  Esquema estilo "C" (snake_case)
        // ===========================

        // AUTENTICACION FACIAL -> autenticacion_facial
        mb.Entity<AutenticacionFacial>(e =>
        {
            e.ToTable("autenticacion_facial");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.EncodingFacial).HasColumnName("encoding_facial").HasColumnType("TEXT");
            e.Property(p => p.ImagenReferencia).HasColumnName("imagen_referencia").HasColumnType("TEXT");
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);
            // ✅ timestamp + CURRENT_TIMESTAMP (evita "Invalid default value")
            e.Property(p => p.FechaCreacion)
             .HasColumnName("fecha_creacion")
             .HasColumnType("timestamp")
             .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.AutenticacionesFaciales)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CODIGOS QR -> codigos_qr
        mb.Entity<CodigoQr>(e =>
        {
            e.ToTable("codigos_qr");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.Codigo).HasColumnName("codigo_qr").HasMaxLength(555);
            e.Property(p => p.QrHash).HasColumnName("qr_hash").HasMaxLength(555);
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.CodigosQr)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // METODOS NOTIFICACION -> metodos_notificacion
        mb.Entity<MetodoNotificacion>(e =>
        {
            e.ToTable("metodos_notificacion");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.Destino).HasColumnName("destino").HasMaxLength(150);
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);
            // ✅ timestamp + CURRENT_TIMESTAMP
            e.Property(p => p.FechaCreacion)
             .HasColumnName("fecha_creacion")
             .HasColumnType("timestamp")
             .HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.Tipo)
             .HasConversion<string>()
             .HasColumnName("tipo_notificacion");

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.MetodosNotificacion)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SESIONES -> sesiones
        mb.Entity<Sesion>(e =>
        {
            e.ToTable("sesiones");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.SessionTokenHash).HasColumnName("session_token").HasMaxLength(255);
            e.Property(p => p.MetodoLogin).HasConversion<string>().HasColumnName("metodo_login");
            // ✅ timestamp + CURRENT_TIMESTAMP
            e.Property(p => p.FechaLogin)
             .HasColumnName("fecha_login")
             .HasColumnType("timestamp")
             .HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.Activa).HasColumnName("activa").HasDefaultValue(true);

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.Sesiones)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
