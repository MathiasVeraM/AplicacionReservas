using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicacionReservas.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoCodigoyFechaCreacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoReserva",
                table: "Reservas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Reservas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoReserva",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Reservas");
        }
    }
}
