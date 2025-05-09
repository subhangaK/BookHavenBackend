using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class database1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c7316f52-83e2-4b24-82e5-df3ee28ae250", "AQAAAAIAAYagAAAAENa/x9sReUmoKZZKCYZXJmTGc5Yc1HKfABDtj4MnlT4URok3hp/srOC8exWePtCrAw==", "29d5ca0e-5dd9-4d1c-ac09-0a761a68ac38" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "646612b6-33e0-47f9-a828-8f7a3f3a08ee", "AQAAAAIAAYagAAAAEMnrSND5WjdCaVYXRNJgRfG0hmouxDiv56KJi02ZswT4V1QyVmBEQyaKjveflr2kbw==", "82fa91ef-1ae1-47d8-a47c-be40fb66475e" });
        }
    }
}
