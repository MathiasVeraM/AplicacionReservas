using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class ModuloHorario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Nombre { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }
        [Required]
        [Range(1, 3)]
        public int DuracionHoras { get; set; } // 1, 2 o 3
        public ICollection<Reserva> Reservas { get; set; }
    }
}
