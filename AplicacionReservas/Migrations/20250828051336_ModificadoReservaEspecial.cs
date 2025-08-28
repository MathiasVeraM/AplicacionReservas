using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class ModificadoReservaEspecial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsMantenimiento",
                table: "Reservas");

            migrationBuilder.RenameColumn(
                name: "HoraInicioMantenimiento",
                table: "Reservas",
                newName: "HoraInicioA");

            migrationBuilder.RenameColumn(
                name: "HoraFinMantenimiento",
                table: "Reservas",
                newName: "HoraFinA");

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Reservas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Reservas");

            migrationBuilder.RenameColumn(
                name: "HoraInicioA",
                table: "Reservas",
                newName: "HoraInicioMantenimiento");

            migrationBuilder.RenameColumn(
                name: "HoraFinA",
                table: "Reservas",
                newName: "HoraFinMantenimiento");

            migrationBuilder.AddColumn<bool>(
                name: "EsMantenimiento",
                table: "Reservas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
