using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "ProfilePicture", "SecurityStamp" },
                values: new object[] { "0f01310a-485c-415c-89f5-cee1e00a2b85", "AQAAAAIAAYagAAAAECfkjCz2UzhB/4An1ZDJwM7AVAgZYvGPjvFRBmcjc4/qpXsTRCYwamp3CvYAgnbjAQ==", null, "1cdb4ac2-4d74-41eb-96ba-fd67ef28e6f7" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d9d6a23c-0a81-405d-b235-aae9b7330f14", "AQAAAAIAAYagAAAAEE2z3ezW9Uw6oRG37t63cvvWqItQPS8QyTX8xQE7TG4iCRWObK55XHUIrta0Yr0LTg==", "6c4cf10f-828d-4b45-91c4-a486d53c1db4" });
        }
    }
}
