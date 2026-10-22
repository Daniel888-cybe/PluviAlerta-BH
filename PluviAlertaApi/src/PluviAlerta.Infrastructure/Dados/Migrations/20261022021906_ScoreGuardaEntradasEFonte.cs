using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluviAlerta.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class ScoreGuardaEntradasEFonte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ChuvaAcum6hMm",
                table: "ScoresRisco",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Fonte",
                table: "ScoresRisco",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Previsao3hMm",
                table: "ScoresRisco",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RelatosAtivos",
                table: "ScoresRisco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Vulnerabilidade",
                table: "ScoresRisco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Fonte",
                table: "LeiturasChuva",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChuvaAcum6hMm",
                table: "ScoresRisco");

            migrationBuilder.DropColumn(
                name: "Fonte",
                table: "ScoresRisco");

            migrationBuilder.DropColumn(
                name: "Previsao3hMm",
                table: "ScoresRisco");

            migrationBuilder.DropColumn(
                name: "RelatosAtivos",
                table: "ScoresRisco");

            migrationBuilder.DropColumn(
                name: "Vulnerabilidade",
                table: "ScoresRisco");

            migrationBuilder.DropColumn(
                name: "Fonte",
                table: "LeiturasChuva");
        }
    }
}
