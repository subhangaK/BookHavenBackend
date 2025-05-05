using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class addimage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "72c72239-6dcf-46ec-b8a7-264777f4f6cd", "AQAAAAIAAYagAAAAENVRD8SxzaF5/Q7IXtzfeYT4hAsKkU1TQMUvQuHtmxMLww5K8Zq5CjJ/FKx7yFUGDg==", "bfb71363-2c0c-4039-983b-2b1cbb250845" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Books");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "52f683dd-fc2d-4ca5-9b2e-921414402555", "AQAAAAIAAYagAAAAEBW8Q/Ud4PbLJW1LPdwR2IcgKz6HOyIShY0v6VAC9Fdidp28lidvrqBYP/nHpU43JA==", "b1a7064f-4ac2-4f3b-b182-d8fe6adf8b14" });
        }
    }
}
