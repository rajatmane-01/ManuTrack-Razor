using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManuTrackAPI.Migrations
{
    /// <inheritdoc />
    public partial class Module1_2_IAM_Product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    ComponentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.ComponentID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOMs_ComponentID",
                table: "BOMs",
                column: "ComponentID");

            migrationBuilder.AddForeignKey(
                name: "FK_BOMs_Components_ComponentID",
                table: "BOMs",
                column: "ComponentID",
                principalTable: "Components",
                principalColumn: "ComponentID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BOMs_Components_ComponentID",
                table: "BOMs");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropIndex(
                name: "IX_BOMs_ComponentID",
                table: "BOMs");
        }
    }
}
