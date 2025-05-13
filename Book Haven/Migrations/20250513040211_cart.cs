using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class cart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId_BookId",
                table: "Carts");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "135892df-3dab-4338-ac47-ffb6926f4616", "AQAAAAIAAYagAAAAECX8VsAGcJi+aCzF5RWwO9e0rRZU0i9SMDgsaZp4k7lR81xfevqetdBuDb6dWYE4mg==", "dd745716-7b52-48a0-83a3-d774f840b62d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "27c8d343-7897-44df-b40a-f75a17c6492e", "AQAAAAIAAYagAAAAEFFDe16UB+qXWXmieXzbXnWnEloUCBCqlQxx85Aub9Go3qE+rBgMETfsCIkx5/M3zg==", "73164f55-894b-43e0-83d5-a479c898b12c" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ba99c049-3bc0-4271-a0a0-d606073d415c", "AQAAAAIAAYagAAAAEBAFrJGjd+xybCHuEVbhS4juw/HTz0aYR0OaMUA3f3ir7hLhSC9RN0JpotCOxXtpxA==", "12138871-9e5d-436f-a10e-1817c13b5615" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "62528a11-07e3-4519-9137-f04e84926d0f", "AQAAAAIAAYagAAAAEJmcldmAWnD0zaoD+QwAmuxR+Kc+9ZUqVs1SCQ9aVhZw8uzrEJ1xpIzluux56fg//A==", "769def97-d4ed-407b-8717-def6ef372c37" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId_BookId",
                table: "Carts",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }
    }
}
