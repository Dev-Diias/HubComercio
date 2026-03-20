using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubComercio.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarTelefoneClientePedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelefoneCliente",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelefoneCliente",
                table: "Pedidos");
        }
    }
}
