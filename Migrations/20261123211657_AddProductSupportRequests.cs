using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShop.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSupportRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductSupportRequestId",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductSupportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSupportRequests_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSupportRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ProductSupportRequestId",
                table: "ChatMessages",
                column: "ProductSupportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupportRequests_OrderId",
                table: "ProductSupportRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupportRequests_OrderItemId",
                table: "ProductSupportRequests",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupportRequests_RequestNumber",
                table: "ProductSupportRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ProductSupportRequests_ProductSupportRequestId",
                table: "ChatMessages",
                column: "ProductSupportRequestId",
                principalTable: "ProductSupportRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ProductSupportRequests_ProductSupportRequestId",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ProductSupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ProductSupportRequestId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ProductSupportRequestId",
                table: "ChatMessages");
        }
    }
}
