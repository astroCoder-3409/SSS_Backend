using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSyncFieldsSecond : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ConfidenceLevel",
                table: "Transactions",
                newName: "PlaidCategoryConfidenceLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlaidCategoryConfidenceLevel",
                table: "Transactions",
                newName: "ConfidenceLevel");
        }
    }
}
