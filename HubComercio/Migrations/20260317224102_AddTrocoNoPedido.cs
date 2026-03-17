using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubComercio.Migrations
{
    /// <inheritdoc />
    public partial class AddTrocoNoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrecisaTroco",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TrocoPara",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecisaTroco",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TrocoPara",
                table: "Pedidos");
        }
    }
}
