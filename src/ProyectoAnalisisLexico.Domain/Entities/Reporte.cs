namespace ProyectoAnalisisLexico.Domain.Entities
{
    public class Reporte
    {
        public int Id { get; set; }
        public int AnalisisId { get; set; }
        public Analisis Analisis { get; set; }

        public string RutaPdf { get; set; } = string.Empty; // dónde se guardó el reporte
        public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

        // Medio de envío
        public string MedioNotificacion { get; set; } = "Email"; // Email o SMS
        public string Destino { get; set; } = string.Empty; // correo o teléfono
    }
}
