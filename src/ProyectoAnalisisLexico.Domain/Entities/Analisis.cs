namespace ProyectoAnalisisLexico.Domain.Entities
{
    public class Analisis
    {
        public int Id { get; set; }
        public int ArchivoId { get; set; }
        public Archivo Archivo { get; set; }

        public int TotalPalabras { get; set; }
        public string PalabrasFrecuentes { get; set; } = string.Empty;
        public string PalabrasRaras { get; set; } = string.Empty;
        public string Pronombres { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Sustantivos { get; set; } = string.Empty;
        public string Verbos { get; set; } = string.Empty;
        public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;
    }
}
