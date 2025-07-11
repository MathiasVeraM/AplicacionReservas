using AplicacionReservas.Models;
using Microsoft.EntityFrameworkCore;


namespace AplicacionReservas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // Tablas
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Reactivo> Reactivos { get; set; }
        public DbSet<ReservaReactivo> ReservaReactivos { get; set; }
        public DbSet<Docente> Docentes { get; set; }
        public DbSet<Laboratorio> Laboratorios { get; set; }
        public DbSet<ModuloHorario> ModulosHorario { get; set; }
        public DbSet<Unidad> Unidades { get; set; }
        public DbSet<MiembroEquipo> MiembrosEquipo { get; set; }
        public DbSet<Insumo> Insumos {  get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MiembroEquipo -> Reserva (uno a muchos)
            modelBuilder.Entity<MiembroEquipo>()
                .HasOne(m => m.Reserva)
                .WithMany(r => r.MiembrosEquipo)
                .HasForeignKey(m => m.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reserva <-> Equipos (muchos a muchos)
            modelBuilder.Entity<Reserva>()
                .HasMany(r => r.Equipos)
                .WithMany();

            // ReservaReactivo (muchos a muchos con cantidad/unidad)
            modelBuilder.Entity<ReservaReactivo>()
                .HasOne(rr => rr.Reserva)
                .WithMany(r => r.ReservaReactivos)
                .HasForeignKey(rr => rr.ReservaId);

            modelBuilder.Entity<ReservaReactivo>()
                .HasOne(rr => rr.Reactivo)
                .WithMany()
                .HasForeignKey(rr => rr.ReactivoId);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.DuracionHoras)
                .HasDefaultValue(1);

            modelBuilder.Entity<Insumo>()
                .HasOne(i => i.Reserva)
                .WithMany(r => r.Insumos)
                .HasForeignKey(i => i.ReservaId);
        }
    }
}
