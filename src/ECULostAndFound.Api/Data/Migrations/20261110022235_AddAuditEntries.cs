using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECULostAndFound.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261110022235_AddAuditEntries : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEntries",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ChangeId = table.Column<string>(type: "TEXT", nullable: false),
                ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                ColumnName = table.Column<string>(type: "TEXT", nullable: false),
                OldValue = table.Column<string>(type: "TEXT", nullable: false),
                NewValue = table.Column<string>(type: "TEXT", nullable: false),
                ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ActorId = table.Column<string>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEntries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEntries_ChangeId",
            table: "AuditEntries",
            column: "ChangeId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditEntries");
    }
}
