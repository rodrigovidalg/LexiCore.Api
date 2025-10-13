namespace Lexico.Domain.Entities
{
    public class Reporte
    {
        public int Id { get; set; }                     // id
        public int AnalisisId { get; set; }             // analisis_id
        public int UsuarioId { get; set; }              // usuario_id
        public string TipoReporte { get; set; } = "pdf"; // enum('pdf','json','csv')
        public string RutaArchivo { get; set; } = "";   // ruta_archivo (puede ser URL)
        public int TamanoArchivo { get; set; }          // tamaño_archivo (bytes)
        public DateTime FechaGeneracion { get; set; }   // fecha_generacion (UTC)
        public bool Enviado { get; set; }               // enviado (0/1)
        public DateTime? FechaEnvio { get; set; }       // fecha_envio
        public string? MetodoEnvio { get; set; }        // enum('email','whatsapp')
        public string? Destinatario { get; set; }       // destinatario
    }
}
