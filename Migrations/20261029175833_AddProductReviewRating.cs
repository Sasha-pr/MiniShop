using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShop.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReviewRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBookmarks_UserId_ProductId",
                table: "ProductBookmarks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductWaitlists",
                table: "ProductWaitlists");

            migrationBuilder.RenameTable(
                name: "ProductWaitlists",
                newName: "ProductWaitlist");

            migrationBuilder.RenameIndex(
                name: "IX_ProductWaitlists_UserId_ProductId",
                table: "ProductWaitlist",
                newName: "IX_ProductWaitlist_UserId_ProductId");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "ProductReviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductWaitlist",
                table: "ProductWaitlist",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductWaitlist",
                table: "ProductWaitlist");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "ProductReviews");

            migrationBuilder.RenameTable(
                name: "ProductWaitlist",
                newName: "ProductWaitlists");

            migrationBuilder.RenameIndex(
                name: "IX_ProductWaitlist_UserId_ProductId",
                table: "ProductWaitlists",
                newName: "IX_ProductWaitlists_UserId_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductWaitlists",
                table: "ProductWaitlists",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBookmarks_UserId_ProductId",
                table: "ProductBookmarks",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }
    }
}
