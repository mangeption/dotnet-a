using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bench.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestObjects",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    T1 = table.Column<string>(nullable: true),
                    T2 = table.Column<string>(nullable: true),
                    T3 = table.Column<string>(nullable: true),
                    T4 = table.Column<string>(nullable: true),
                    T5 = table.Column<string>(nullable: true),
                    T6 = table.Column<string>(nullable: true),
                    T7 = table.Column<string>(nullable: true),
                    T8 = table.Column<string>(nullable: true),
                    T9 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestObjects", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestObjects");
        }
    }
}
