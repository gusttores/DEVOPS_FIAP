using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovAmbiental.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeOperacional = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Responsavel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScoreConformidade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Normas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrgaoEmissor = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    DataVigencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Normas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormaAmbientalId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Peso = table.Column<int>(type: "int", nullable: false),
                    Criticidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisitos_Normas_NormaAmbientalId",
                        column: x => x.NormaAmbientalId,
                        principalTable: "Normas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditoriaId = table.Column<int>(type: "int", nullable: false),
                    RequisitoNormaId = table.Column<int>(type: "int", nullable: false),
                    Atendido = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Evidencia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensAuditoria_Auditorias_AuditoriaId",
                        column: x => x.AuditoriaId,
                        principalTable: "Auditorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensAuditoria_Requisitos_RequisitoNormaId",
                        column: x => x.RequisitoNormaId,
                        principalTable: "Requisitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NaoConformidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditoriaId = table.Column<int>(type: "int", nullable: false),
                    RequisitoNormaId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Criticidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdentificadaEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NaoConformidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NaoConformidades_Auditorias_AuditoriaId",
                        column: x => x.AuditoriaId,
                        principalTable: "Auditorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NaoConformidades_Requisitos_RequisitoNormaId",
                        column: x => x.RequisitoNormaId,
                        principalTable: "Requisitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcoesCorretivas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NaoConformidadeId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Responsavel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PrazoLimite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcoesCorretivas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcoesCorretivas_NaoConformidades_NaoConformidadeId",
                        column: x => x.NaoConformidadeId,
                        principalTable: "NaoConformidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcoesCorretivas_NaoConformidadeId",
                table: "AcoesCorretivas",
                column: "NaoConformidadeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcoesCorretivas_Status_PrazoLimite",
                table: "AcoesCorretivas",
                columns: new[] { "Status", "PrazoLimite" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_DataInicio",
                table: "Auditorias",
                column: "DataInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Status",
                table: "Auditorias",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Status_UnidadeOperacional",
                table: "Auditorias",
                columns: new[] { "Status", "UnidadeOperacional" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensAuditoria_AuditoriaId_RequisitoNormaId",
                table: "ItensAuditoria",
                columns: new[] { "AuditoriaId", "RequisitoNormaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensAuditoria_RequisitoNormaId",
                table: "ItensAuditoria",
                column: "RequisitoNormaId");

            migrationBuilder.CreateIndex(
                name: "IX_NaoConformidades_AuditoriaId",
                table: "NaoConformidades",
                column: "AuditoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_NaoConformidades_RequisitoNormaId",
                table: "NaoConformidades",
                column: "RequisitoNormaId");

            migrationBuilder.CreateIndex(
                name: "IX_Normas_Ativa_Categoria",
                table: "Normas",
                columns: new[] { "Ativa", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_Normas_Categoria",
                table: "Normas",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Normas_Codigo",
                table: "Normas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitos_NormaAmbientalId",
                table: "Requisitos",
                column: "NormaAmbientalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcoesCorretivas");

            migrationBuilder.DropTable(
                name: "ItensAuditoria");

            migrationBuilder.DropTable(
                name: "NaoConformidades");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Requisitos");

            migrationBuilder.DropTable(
                name: "Normas");
        }
    }
}
