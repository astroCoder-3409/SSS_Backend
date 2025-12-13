using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncTime",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialName",
                table: "Accounts",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaidAccountId",
                table: "Accounts",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaidMask",
                table: "Accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OfficialName",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PlaidAccountId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PlaidMask",
                table: "Accounts");
        }
    }
}
