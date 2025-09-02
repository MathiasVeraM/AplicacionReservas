using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class Unidad
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Nombre { get; set; }

        public ICollection<Reactivo> Reactivos { get; set; } = new List<Reactivo>();
    }
}
