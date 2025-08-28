using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class AgregandoGrupoEnReservaEspecial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GrupoEstudiantes",
                table: "Reservas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrupoEstudiantes",
                table: "Reservas");
        }
    }
}
