﻿using System.ComponentModel.DataAnnotations;

namespace AplicacionReservas.Models
{
    public class Docente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Nombre { get; set; }

        public ICollection<Reserva> Reservas { get; set; }
    }
}
