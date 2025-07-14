using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionModulosHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionHoras",
                table: "ModulosHorario",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracionHoras",
                table: "ModulosHorario");
        }
    }
}
