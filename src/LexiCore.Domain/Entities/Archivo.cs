namespace ProyectoAnalisisLexico.Domain.Entities
{
    public class Archivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty; // opcional si guardas ruta física
        public string Contenido { get; set; } = string.Empty; // opcional si guardas texto
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        // Relación con Usuario
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}
