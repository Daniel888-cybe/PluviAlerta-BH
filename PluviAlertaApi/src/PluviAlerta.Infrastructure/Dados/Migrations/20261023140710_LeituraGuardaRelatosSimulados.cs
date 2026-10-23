using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluviAlerta.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LeituraGuardaRelatosSimulados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatosExtras",
                table: "LeiturasChuva",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatosExtras",
                table: "LeiturasChuva");
        }
    }
}
