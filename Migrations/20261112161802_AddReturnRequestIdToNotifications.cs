using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShop.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnRequestIdToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnRequestId",
                table: "UserNotifications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ReturnRequestId",
                table: "UserNotifications",
                column: "ReturnRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_ReturnRequests_ReturnRequestId",
                table: "UserNotifications",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserNotifications_ReturnRequests_ReturnRequestId",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_ReturnRequestId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "UserNotifications");
        }
    }
}
