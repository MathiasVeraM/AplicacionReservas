using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class Equipo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Nombre { get; set; }

        [Required]
        public string NombreLaboratorio { get; set; }
    }
}
