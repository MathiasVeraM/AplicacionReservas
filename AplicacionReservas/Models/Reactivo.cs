using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace AplicacionReservas.Models
{
    public class Reactivo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [NotNull]
        public string? Nombre { get; set; }

        [Required]
        [AllowNull]
        public string? Codigo { get; set; }
    }
}
