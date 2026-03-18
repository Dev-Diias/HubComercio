using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubComercio.Migrations
{
    /// <inheritdoc />
    public partial class AlterarQtdeProdutoParaDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Qtde",
                table: "Produtos",
                type: "decimal(10,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Qtde",
                table: "Produtos",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)");
        }
    }
}
