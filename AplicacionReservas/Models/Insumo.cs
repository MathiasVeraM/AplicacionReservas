using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class Insumo
    {
        [Key]
        public int Id { get; set; }

        public int ReservaId { get; set; }
        public Reserva Reserva { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
    }
}
