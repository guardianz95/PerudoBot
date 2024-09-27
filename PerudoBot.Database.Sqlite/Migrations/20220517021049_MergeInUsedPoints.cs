using Microsoft.EntityFrameworkCore.Migrations;

namespace PerudoBot.Database.Sqlite.Migrations
{
    public partial class MergeInUsedPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Players] SET [Points] = [Points] - [UsedPoints]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
