using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubComercio.Migrations
{
    /// <inheritdoc />
    public partial class AddDataResetFinanceiroNoTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataResetFinanceiro",
                table: "Tenants",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataResetFinanceiro",
                table: "Tenants");
        }
    }
}
