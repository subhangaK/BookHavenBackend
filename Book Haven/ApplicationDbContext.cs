using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Book_Haven.Entities;

namespace Book_Haven
{
    public class ApplicationDbContext : IdentityDbContext<User, Roles, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; } // Add Books DbSet

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed Role
            builder.Entity<Roles>().HasData(
                new Roles
                {
                    Id = 1,
                    Name = "SuperAdmin",
                    NormalizedName = "SUPERADMIN"
                }
            );

            // Seed User
            var superAdmin = new User
            {
                Id = 1,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@gmail.com",
                NormalizedEmail = "ADMIN@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            superAdmin.PasswordHash = new PasswordHasher<User>()
                .HashPassword(superAdmin, "Test@123");

            builder.Entity<User>().HasData(superAdmin);

            // Seed UserRole
            builder.Entity<IdentityUserRole<long>>().HasData(
                new IdentityUserRole<long>
                {
                    RoleId = 1,
                    UserId = 1
                }
            );
        }
    }
}