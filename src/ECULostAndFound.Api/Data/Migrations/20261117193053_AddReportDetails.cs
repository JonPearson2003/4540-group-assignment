using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECULostAndFound.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261117193053_AddReportDetails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CollectionDetails",
            table: "LostAndFoundItems",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "CollectionLocation",
            table: "LostAndFoundItems",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "DateFound",
            table: "LostAndFoundItems",
            type: "TEXT",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.Sql(
            "UPDATE LostAndFoundItems SET DateFound = DateReported");

        migrationBuilder.AddColumn<string>(
            name: "PhotoData",
            table: "LostAndFoundItems",
            type: "TEXT",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CollectionDetails",
            table: "LostAndFoundItems");

        migrationBuilder.DropColumn(
            name: "CollectionLocation",
            table: "LostAndFoundItems");

        migrationBuilder.DropColumn(
            name: "DateFound",
            table: "LostAndFoundItems");

        migrationBuilder.DropColumn(
            name: "PhotoData",
            table: "LostAndFoundItems");
    }
}
