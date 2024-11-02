using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityBillingSystem.Infrastructure.Migrations
{
    public partial class updatebill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Bills",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Bills");
        }
    }
}
