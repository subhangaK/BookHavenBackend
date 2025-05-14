using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class database : Migration
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
                values: new object[] { "959d7a3e-35bd-4981-8daa-2e31ae8315b4", "AQAAAAIAAYagAAAAEMSexm14dsGGka/Po2i0kYdV0mwTRS0QEiTYfszviS1GRaS5mVbb12N84pHpQgabxw==", "38e4a5e2-4d2e-4e35-9f5c-ba77ceabd2fb" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "25cca101-a44d-4d3c-a251-8ef571e6bce8", "AQAAAAIAAYagAAAAEB916hfPA7lpLGsUfSAKoDj7UosKiDoKoz2it47ViyZCirKr4ZGPyPQsSq6iBTLxhA==", "f7ff1ce1-5659-4ddc-9a99-1d83226e2b5c" });

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
