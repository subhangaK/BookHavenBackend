using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class discount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c293ac91-9d65-4ceb-a6c3-a3d18b162cf8", "AQAAAAIAAYagAAAAELFGRW74w49wLHsX6FSyix/xA6VuVy0xjkeeLmniUyKTr/TE62WB163T60V3bhziAw==", "896b3c6a-1e61-4567-af48-ab4bee6b859f" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c7316f52-83e2-4b24-82e5-df3ee28ae250", "AQAAAAIAAYagAAAAENa/x9sReUmoKZZKCYZXJmTGc5Yc1HKfABDtj4MnlT4URok3hp/srOC8exWePtCrAw==", "29d5ca0e-5dd9-4d1c-ac09-0a761a68ac38" });
        }
    }
}
