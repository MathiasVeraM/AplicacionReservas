using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class MiembroEquipo
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? Nombre { get; set; }
        public int ReservaId { get; set; }
        public Reserva Reserva { get; set; }
    }
}
