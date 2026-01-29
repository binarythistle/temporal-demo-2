using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace marketplacer.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookEvents : Migration
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}
