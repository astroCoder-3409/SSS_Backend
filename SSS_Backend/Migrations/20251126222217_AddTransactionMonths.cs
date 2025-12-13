using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionMonths",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionMonths",
                table: "Users");
        }
    }
}
