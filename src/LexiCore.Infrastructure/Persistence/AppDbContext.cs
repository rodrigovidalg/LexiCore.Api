using Microsoft.EntityFrameworkCore;
using ProyectoAnalisisLexico.Domain.Entities;

namespace ProyectoAnalisisLexico.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Archivo> Archivos { get; set; }
        public DbSet<Analisis> Analisis { get; set; }
        public DbSet<Reporte> Reportes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
