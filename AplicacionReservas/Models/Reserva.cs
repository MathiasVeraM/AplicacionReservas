using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AplicacionReservas.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }
        public string? Materia { get; set; }
        public string? NombreProyecto { get; set; }
        public string? Actividad { get; set; }
        public string? ConsideracionesEspeciales { get; set; }
        public int? ModuloHorarioId { get; set; }
        public ModuloHorario? ModuloHorario { get; set; }
        public int LaboratorioId { get; set; }
        public Laboratorio? Laboratorio { get; set; }
        public int? DocenteId { get; set; }
        public Docente? Docente { get; set; }
        public string? EvidenciaCorreoRuta { get; set; }
        public EstadoAprobacion Aprobado { get; set; } = EstadoAprobacion.Pendiente;
        public TipoReserva Tipo { get; set; } = TipoReserva.Estandar;
        public TimeSpan? HoraInicioA { get; set; }
        public TimeSpan? HoraFinA { get; set; }
        [Required]
        [Range(1, 3, ErrorMessage = "Duración debe ser entre 1 y 3 horas")]
        public int DuracionHoras { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public string? ObservacionesFinales { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [MaxLength(10)]
        public string? CodigoReserva { get; set; }
        // Comprobacion de que es una reserva finalizada
        [NotMapped]
        public bool EstaFinalizada
        {
            get
            {
                DateTime fin;

                if ((Tipo == TipoReserva.Mantenimiento || Tipo == TipoReserva.Especial) && HoraFinA.HasValue)
                {
                    fin = Fecha.Date + HoraFinA.Value;
                }
                else if (ModuloHorario != null)
                {
                    fin = Fecha.Date + ModuloHorario.HoraFin;
                }
                else
                {
                    return false;
                }

                return DateTime.Now >= fin;
            }
        }

        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
        public ICollection<ReservaReactivo> ReservaReactivos { get; set; } = new List<ReservaReactivo>();
        public ICollection<MiembroEquipo> MiembrosEquipo { get; set; } = new List<MiembroEquipo>();
        public ICollection<Insumo> Insumos { get; set; }

    }

    public enum EstadoAprobacion
    {
        NoAprobado,
        Revision,
        Aprobado,
        Pendiente
    }

    public enum TipoReserva
    {
        Estandar,
        Especial,
        Mantenimiento
    }
    
}
