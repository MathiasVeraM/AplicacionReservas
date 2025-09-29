using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class EliminandoUnidadesdeBD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactivos_Unidades_UnidadId",
                table: "Reactivos");

            migrationBuilder.DropTable(
                name: "Unidades");

            migrationBuilder.DropIndex(
                name: "IX_Reactivos_UnidadId",
                table: "Reactivos");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "ReservaReactivos");

            migrationBuilder.DropColumn(
                name: "UnidadId",
                table: "Reactivos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unidad",
                table: "ReservaReactivos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UnidadId",
                table: "Reactivos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Unidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reactivos_UnidadId",
                table: "Reactivos",
                column: "UnidadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reactivos_Unidades_UnidadId",
                table: "Reactivos",
                column: "UnidadId",
                principalTable: "Unidades",
                principalColumn: "Id");
        }
    }
}
