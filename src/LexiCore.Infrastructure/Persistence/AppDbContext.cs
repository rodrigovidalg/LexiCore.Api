using Microsoft.EntityFrameworkCore;
using LexiCore.Domain.Entities;

namespace LexiCore.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    // DbSets (todas las tablas)
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

        // Charset/collation (Pomelo)
        mb.HasCharSet("utf8mb4").UseCollation("utf8mb4_0900_ai_ci");

        // ====== USUARIOS ======
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioNombre).HasColumnName("usuario");
            e.Property(p => p.Email).HasColumnName("email");
            e.Property(p => p.NombreCompleto).HasColumnName("nombre_completo");
            e.Property(p => p.PasswordHash).HasColumnName("password_hash");
            e.Property(p => p.Telefono).HasColumnName("telefono");
            e.Property(p => p.FechaCreacion).HasColumnName("fecha_creacion")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);
        });

        // ====== AUTENTICACION FACIAL ======
        mb.Entity<AutenticacionFacial>(e =>
        {
            e.ToTable("autenticacion_facial");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.EncodingFacial).HasColumnName("encoding_facial").HasColumnType("TEXT");
            e.Property(p => p.ImagenReferencia).HasColumnName("imagen_referencia").HasColumnType("TEXT");
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);
            e.Property(p => p.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.AutenticacionesFaciales)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== CODIGOS QR ======
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

        // ====== METODOS NOTIFICACION ======
        mb.Entity<MetodoNotificacion>(e =>
        {
            e.ToTable("metodos_notificacion");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.Destino).HasColumnName("destino").HasMaxLength(150);
            e.Property(p => p.Activo).HasColumnName("activo").HasDefaultValue(true);
            e.Property(p => p.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(p => p.Tipo).HasConversion<string>().HasColumnName("tipo_notificacion");

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.MetodosNotificacion)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== SESIONES ======
        mb.Entity<Sesion>(e =>
        {
            e.ToTable("sesiones");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.SessionTokenHash).HasColumnName("session_token").HasMaxLength(255);
            e.Property(p => p.MetodoLogin).HasConversion<string>().HasColumnName("metodo_login");
            e.Property(p => p.FechaLogin).HasColumnName("fecha_login").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.Activa).HasColumnName("activa").HasDefaultValue(true);

            e.HasOne(p => p.Usuario)
             .WithMany(u => u.Sesiones)
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== ARCHIVOS ======
        mb.Entity<Archivo>(e =>
        {
            e.ToTable("archivos");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.Nombre).HasColumnName("nombre");
            e.Property(p => p.Ruta).HasColumnName("ruta");
            e.Property(p => p.Contenido).HasColumnName("contenido").HasColumnType("LONGTEXT");
            e.Property(p => p.FechaSubida).HasColumnName("fecha_subida").HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(p => p.Usuario)
             .WithMany()
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== ANALISIS ======
        mb.Entity<Analisis>(e =>
        {
            e.ToTable("analisis");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.ArchivoId).HasColumnName("archivo_id");
            e.Property(p => p.TotalPalabras).HasColumnName("total_palabras");
            e.Property(p => p.PalabrasFrecuentes).HasColumnName("palabras_frecuentes");
            e.Property(p => p.PalabrasRaras).HasColumnName("palabras_raras");
            e.Property(p => p.Pronombres).HasColumnName("pronombres");
            e.Property(p => p.Verbos).HasColumnName("verbos");
            e.Property(p => p.Sustantivos).HasColumnName("sustantivos");
            e.Property(p => p.FechaAnalisis).HasColumnName("fecha_analisis").HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(p => p.Archivo)
             .WithMany()
             .HasForeignKey(p => p.ArchivoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ====== REPORTES ======
        mb.Entity<Reporte>(e =>
        {
            e.ToTable("reportes");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(p => p.AnalisisId).HasColumnName("analisis_id");
            e.Property(p => p.RutaPdf).HasColumnName("ruta_pdf");
            e.Property(p => p.FechaGeneracion).HasColumnName("fecha_generacion").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.MedioNotificacion).HasColumnName("medio_notificacion");
            e.Property(p => p.Destino).HasColumnName("destino");

            e.HasOne(p => p.Analisis)
             .WithMany()
             .HasForeignKey(p => p.AnalisisId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
