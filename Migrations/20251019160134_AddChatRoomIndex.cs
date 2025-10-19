using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadeHava.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRoomIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserGroupChat_GroupChatId",
                table: "UserGroupChat",
                column: "GroupChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserGroupChat_GroupChatId",
                table: "UserGroupChat");
        }
    }
}
