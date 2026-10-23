using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PluviAlerta.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Regioes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Vulnerabilidade = table.Column<int>(type: "int", nullable: false),
                    GeoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regioes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertasEnviados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegiaoId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnviadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasEnviados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasEnviados_Regioes_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "Regioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Inscricoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    RegiaoId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscricoes_Regioes_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "Regioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeiturasChuva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegiaoId = table.Column<int>(type: "int", nullable: false),
                    Momento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MmUltimaHora = table.Column<double>(type: "double", nullable: false),
                    MmAcum6h = table.Column<double>(type: "double", nullable: false),
                    MmPrevisao3h = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeiturasChuva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeiturasChuva_Regioes_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "Regioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Relatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    RegiaoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(280)", maxLength: 280, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatos_Regioes_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "Regioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScoresRisco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegiaoId = table.Column<int>(type: "int", nullable: false),
                    Momento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoresRisco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoresRisco_Regioes_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "Regioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Regioes",
                columns: new[] { "Id", "GeoJson", "Latitude", "Longitude", "Nome", "Vulnerabilidade" },
                values: new object[,]
                {
                    { 1, null, -19.975000000000001, -44.024999999999999, "Barreiro", 50 },
                    { 2, null, -19.93, -43.939999999999998, "Centro-Sul", 50 },
                    { 3, null, -19.91, -43.899999999999999, "Leste", 50 },
                    { 4, null, -19.859999999999999, -43.909999999999997, "Nordeste", 50 },
                    { 5, null, -19.899999999999999, -43.969999999999999, "Noroeste", 50 },
                    { 6, null, -19.82, -43.950000000000003, "Norte", 50 },
                    { 7, null, -19.93, -44.0, "Oeste", 50 },
                    { 8, null, -19.850000000000001, -43.969999999999999, "Pampulha", 50 },
                    { 9, null, -19.809999999999999, -43.960000000000001, "Venda Nova", 50 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasEnviados_RegiaoId_EnviadoEm",
                table: "AlertasEnviados",
                columns: new[] { "RegiaoId", "EnviadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_RegiaoId",
                table: "Inscricoes",
                column: "RegiaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_TelegramChatId_RegiaoId",
                table: "Inscricoes",
                columns: new[] { "TelegramChatId", "RegiaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeiturasChuva_RegiaoId_Momento",
                table: "LeiturasChuva",
                columns: new[] { "RegiaoId", "Momento" });

            migrationBuilder.CreateIndex(
                name: "IX_Regioes_Nome",
                table: "Regioes",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relatos_RegiaoId_ExpiraEm",
                table: "Relatos",
                columns: new[] { "RegiaoId", "ExpiraEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoresRisco_RegiaoId_Momento",
                table: "ScoresRisco",
                columns: new[] { "RegiaoId", "Momento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasEnviados");

            migrationBuilder.DropTable(
                name: "Inscricoes");

            migrationBuilder.DropTable(
                name: "LeiturasChuva");

            migrationBuilder.DropTable(
                name: "Relatos");

            migrationBuilder.DropTable(
                name: "ScoresRisco");

            migrationBuilder.DropTable(
                name: "Regioes");
        }
    }
}
