using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SgdBaldan.Data.Migrations
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
                name: "categorias",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cor = table.Column<string>(type: "char(7)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id_categoria);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id_produto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.id_produto);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    re = table.Column<string>(type: "varchar(5)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    senha_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    perfil = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id_usuario);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dispositivos",
                columns: table => new
                {
                    id_dispositivo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_produto = table.Column<int>(type: "int", nullable: false),
                    id_categoria = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispositivos", x => x.id_dispositivo);
                    table.ForeignKey(
                        name: "FK_dispositivos_categorias_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispositivos_produtos_id_produto",
                        column: x => x.id_produto,
                        principalTable: "produtos",
                        principalColumn: "id_produto",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laudos",
                columns: table => new
                {
                    id_laudo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_dispositivo = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    ordem_servico = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_responsavel_projeto = table.Column<int>(type: "int", nullable: true),
                    id_conferente = table.Column<int>(type: "int", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_envio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    custo_material = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    custo_mao_obra = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    chave_liberacao_unica = table.Column<int>(type: "int", nullable: true, computedColumnSql: "IF(`tipo` = 'Liberacao', `id_dispositivo`, NULL)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laudos", x => x.id_laudo);
                    table.ForeignKey(
                        name: "FK_laudos_dispositivos_id_dispositivo",
                        column: x => x.id_dispositivo,
                        principalTable: "dispositivos",
                        principalColumn: "id_dispositivo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_laudos_usuarios_id_conferente",
                        column: x => x.id_conferente,
                        principalTable: "usuarios",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_laudos_usuarios_id_responsavel_projeto",
                        column: x => x.id_responsavel_projeto,
                        principalTable: "usuarios",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "anexos",
                columns: table => new
                {
                    id_anexo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    categoria = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nome_original = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_mime = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tamanho_bytes = table.Column<int>(type: "int", nullable: false),
                    caminho_arquivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_envio = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexos", x => x.id_anexo);
                    table.ForeignKey(
                        name: "FK_anexos_laudos_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudos",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "checklist",
                columns: table => new
                {
                    id_checklist = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false),
                    descricao = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resultado = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist", x => x.id_checklist);
                    table.ForeignKey(
                        name: "FK_checklist_laudos_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudos",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laudo_corretiva",
                columns: table => new
                {
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    descricao_falha = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    componente_afetado = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_falha = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    causas_sugeridas = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    relato_ia = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resumo_ia = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    outras_informacoes = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laudo_corretiva", x => x.id_laudo);
                    table.ForeignKey(
                        name: "FK_laudo_corretiva_laudos_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudos",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laudo_historico",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_hora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laudo_historico", x => x.id_historico);
                    table.ForeignKey(
                        name: "FK_laudo_historico_laudos_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudos",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_laudo_historico_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medidas_especificacoes",
                columns: table => new
                {
                    id_medida = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false),
                    descricao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    especificacao = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    limite_min = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    limite_max = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    metodo_instrumento = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    medicao = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    resultado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medidas_especificacoes", x => x.id_medida);
                    table.ForeignKey(
                        name: "FK_medidas_especificacoes_laudos_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudos",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acao_corretiva",
                columns: table => new
                {
                    id_acao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false),
                    oque = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    como = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acao_corretiva", x => x.id_acao);
                    table.ForeignKey(
                        name: "FK_acao_corretiva_laudo_corretiva_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudo_corretiva",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ishikawa",
                columns: table => new
                {
                    id_laudo = table.Column<int>(type: "int", nullable: false),
                    metodo = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    maquina = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    material = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    medicao = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    meio_ambiente = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mao_de_obra = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    causa_raiz = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ishikawa", x => x.id_laudo);
                    table.ForeignKey(
                        name: "FK_ishikawa_laudo_corretiva_id_laudo",
                        column: x => x.id_laudo,
                        principalTable: "laudo_corretiva",
                        principalColumn: "id_laudo",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_acao_corretiva_id_laudo_numero",
                table: "acao_corretiva",
                columns: new[] { "id_laudo", "numero" });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_id_laudo",
                table: "anexos",
                column: "id_laudo");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_cor",
                table: "categorias",
                column: "cor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categorias_nome",
                table: "categorias",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checklist_id_laudo_numero",
                table: "checklist",
                columns: new[] { "id_laudo", "numero" });

            migrationBuilder.CreateIndex(
                name: "IX_dispositivos_codigo",
                table: "dispositivos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispositivos_id_categoria",
                table: "dispositivos",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_dispositivos_id_produto",
                table: "dispositivos",
                column: "id_produto");

            migrationBuilder.CreateIndex(
                name: "IX_laudo_historico_id_laudo_data_hora",
                table: "laudo_historico",
                columns: new[] { "id_laudo", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "IX_laudo_historico_id_usuario",
                table: "laudo_historico",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_laudos_chave_liberacao_unica",
                table: "laudos",
                column: "chave_liberacao_unica",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_laudos_id_conferente",
                table: "laudos",
                column: "id_conferente");

            migrationBuilder.CreateIndex(
                name: "IX_laudos_id_dispositivo_tipo",
                table: "laudos",
                columns: new[] { "id_dispositivo", "tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_laudos_id_responsavel_projeto",
                table: "laudos",
                column: "id_responsavel_projeto");

            migrationBuilder.CreateIndex(
                name: "IX_laudos_status",
                table: "laudos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_medidas_especificacoes_id_laudo_numero",
                table: "medidas_especificacoes",
                columns: new[] { "id_laudo", "numero" });

            migrationBuilder.CreateIndex(
                name: "IX_produtos_nome",
                table: "produtos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_re",
                table: "usuarios",
                column: "re",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acao_corretiva");

            migrationBuilder.DropTable(
                name: "anexos");

            migrationBuilder.DropTable(
                name: "checklist");

            migrationBuilder.DropTable(
                name: "ishikawa");

            migrationBuilder.DropTable(
                name: "laudo_historico");

            migrationBuilder.DropTable(
                name: "medidas_especificacoes");

            migrationBuilder.DropTable(
                name: "laudo_corretiva");

            migrationBuilder.DropTable(
                name: "laudos");

            migrationBuilder.DropTable(
                name: "dispositivos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "produtos");
        }
    }
}
