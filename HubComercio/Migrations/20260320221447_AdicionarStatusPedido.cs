using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubComercio.Migrations
{
    public partial class AdicionarStatusPedido : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET Status =
                    CASE
                        WHEN Status = 'Pendente' THEN '1'
                        WHEN Status = 'EmPreparacao' THEN '2'
                        WHEN Status = 'Concluido' THEN '3'
                        WHEN Status = 'Finalizado' THEN '3'
                        WHEN Status = 'Cancelado' THEN '4'
                        ELSE '1'
                    END
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Pedidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET Status =
                    CASE
                        WHEN Status = '1' THEN 'Pendente'
                        WHEN Status = '2' THEN 'EmPreparacao'
                        WHEN Status = '3' THEN 'Concluido'
                        WHEN Status = '4' THEN 'Cancelado'
                        ELSE 'Pendente'
                    END
            ");
        }
    }
}