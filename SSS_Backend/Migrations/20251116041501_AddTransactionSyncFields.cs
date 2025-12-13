using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaidTransactionsCursor",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfidenceLevel",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "Transactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaidCategoryDetailed",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaidCategoryPrimary",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaidTransactionId",
                table: "Transactions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaidTransactionsCursor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConfidenceLevel",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PlaidCategoryDetailed",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PlaidCategoryPrimary",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PlaidTransactionId",
                table: "Transactions");
        }
    }
}
