using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class CambioEstadosAprobacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoReserva",
                table: "Reservas");

            migrationBuilder.AlterColumn<int>(
                name: "Aprobado",
                table: "Reservas",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Aprobado",
                table: "Reservas",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TipoReserva",
                table: "Reservas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
