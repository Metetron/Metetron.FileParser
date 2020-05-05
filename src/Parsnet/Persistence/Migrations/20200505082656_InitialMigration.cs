using Microsoft.EntityFrameworkCore.Migrations;

namespace Parsnet.Persistence.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreationTimeWatchers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParserName = table.Column<string>(nullable: false),
                    LastCreationTimeUtc = table.Column<long>(nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreationTimeWatchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WriteTimeWatchers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParserName = table.Column<string>(nullable: false),
                    LastWriteTimeUtc = table.Column<long>(nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteTimeWatchers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreationTimeWatchers");

            migrationBuilder.DropTable(
                name: "WriteTimeWatchers");
        }
    }
}
