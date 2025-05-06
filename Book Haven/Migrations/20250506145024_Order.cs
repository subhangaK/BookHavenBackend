using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Book_Haven.Migrations
{
    /// <inheritdoc />
    public partial class Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BookId = table.Column<long>(type: "bigint", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "646612b6-33e0-47f9-a828-8f7a3f3a08ee", "AQAAAAIAAYagAAAAEMnrSND5WjdCaVYXRNJgRfG0hmouxDiv56KJi02ZswT4V1QyVmBEQyaKjveflr2kbw==", "82fa91ef-1ae1-47d8-a47c-be40fb66475e" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BookId",
                table: "Orders",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_BookId",
                table: "Orders",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ed357c77-3f46-4060-9828-5adf99dbe012", "AQAAAAIAAYagAAAAEJLmhlqoOpWI+0EbnY+QnYaXvpEjua0RqfszI+RhEVSDx1k5Qt/Ln5XAIIqUlGGKoA==", "9bcfe43a-2138-4546-8909-3525cd666ce7" });
        }
    }
}
