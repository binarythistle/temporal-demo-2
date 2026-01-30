using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubSpotService.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookId = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookBody = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookHeaders = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookObjectId = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookObjectType = table.Column<string>(type: "TEXT", nullable: false),
                    WebhookEventType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentFromSourceAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HubSpotResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WebhookEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    HubSpotObjectId = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HubSpotResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HubSpotResponses_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "WebhookEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HubSpotResponses_WebhookEventId",
                table: "HubSpotResponses",
                column: "WebhookEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HubSpotResponses");

            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}
