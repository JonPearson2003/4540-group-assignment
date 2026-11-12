using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECULostAndFound.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261110013443_AddProcessedChanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "ServerVersion",
            table: "LostAndFoundItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateTable(
            name: "ProcessedChanges",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ChangeId = table.Column<string>(type: "TEXT", nullable: false),
                RecordId = table.Column<int>(type: "INTEGER", nullable: false),
                OperationType = table.Column<string>(type: "TEXT", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ItemJson = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcessedChanges", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProcessedChanges_ChangeId",
            table: "ProcessedChanges",
            column: "ChangeId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProcessedChanges");

        migrationBuilder.DropColumn(
            name: "ServerVersion",
            table: "LostAndFoundItems");
    }
}
