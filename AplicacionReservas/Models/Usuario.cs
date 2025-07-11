using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AplicacionReservas.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [NotNull]
        [EmailAddress]
        public string? Email {  get; set; }
        [Required]
        [NotNull]
        public string? Password { get; set; }
        [Required]
        [NotNull]
        public string? IDBanner { get; set; }
        [Required]
        [NotNull]
        public string? Rol {  get; set; }
        public ICollection<Reserva> Reservas { get; set; }

    }
}
