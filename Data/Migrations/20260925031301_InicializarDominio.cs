using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicializarDominio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    IngresosMensuales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.CheckConstraint("CK_Cliente_IngresosMensuales", "\"IngresosMensuales\" > 0");
                    table.ForeignKey(
                        name: "FK_Clientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCredito", x => x.Id);
                    table.CheckConstraint("CK_SolicitudCredito_MontoSolicitado", "\"MontoSolicitado\" > 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "1", "STATIC_ROLE_STAMP", "Analista", "ANALISTA" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "1", 0, "STATIC_CONCURRENCY_STAMP_1", "analista@banco.com", true, false, null, "ANALISTA@BANCO.COM", "ANALISTA@BANCO.COM", "AQAAAAIAAYagAAAAEAabc1234567890", null, false, "STATIC_SECURITY_STAMP_1", false, "analista@banco.com" },
                    { "2", 0, "STATIC_CONCURRENCY_STAMP_2", "cliente1@test.com", true, false, null, "CLIENTE1@TEST.COM", "CLIENTE1@TEST.COM", "AQAAAAIAAYagAAAAEBabc1234567890", null, false, "STATIC_SECURITY_STAMP_2", false, "cliente1@test.com" },
                    { "3", 0, "STATIC_CONCURRENCY_STAMP_3", "cliente2@test.com", true, false, null, "CLIENTE2@TEST.COM", "CLIENTE2@TEST.COM", "AQAAAAIAAYagAAAAECabc1234567890", null, false, "STATIC_SECURITY_STAMP_3", false, "cliente2@test.com" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "1", "1" });

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Activo", "IngresosMensuales", "UsuarioId" },
                values: new object[,]
                {
                    { 1, true, 5000m, "2" },
                    { 2, true, 8000m, "3" }
                });

            migrationBuilder.InsertData(
                table: "SolicitudesCredito",
                columns: new[] { "Id", "ClienteId", "Estado", "FechaSolicitud", "MontoSolicitado", "MotivoRechazo" },
                values: new object[,]
                {
                    { 1, 1, 0, new DateTime(2023, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1000m, null },
                    { 2, 2, 1, new DateTime(2023, 10, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 2500m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_ClienteId",
                table: "SolicitudesCredito",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCredito");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1", "1" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "3");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1");
        }
    }
}
