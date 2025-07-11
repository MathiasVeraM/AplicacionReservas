using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class ReservaReactivo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReservaId { get; set; }
        public Reserva Reserva { get; set; }

        [Required]
        public int ReactivoId { get; set; }
        public Reactivo Reactivo { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public string Unidad { get; set; }
    }
}
