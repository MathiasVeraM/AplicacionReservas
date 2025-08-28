using AplicacionReservas.Models;
using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.ViewModels
{
    public class ReservaViewModel
    {
        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int LaboratorioId { get; set; }

        [Required]
        public TimeSpan? HoraInicio { get; set; }

        [Required]
        public TimeSpan? HoraFin { get; set; }

        public TipoReserva Tipo { get; set; } = TipoReserva.Especial;

    }
}
