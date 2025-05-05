using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Identity;

namespace Book_Haven
{
    public class ApplicationDbContext : IdentityDbContext<User, Roles, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed Role
            builder.Entity<Roles>().HasData(
                new Roles
                {
                    Id = 1,
                    Name = "User",
                    NormalizedName = "USER"
                }
            );

            // Seed User
            var defaultUser = new User
            {
                Id = 1,
                UserName = "defaultuser",
                NormalizedUserName = "DEFAULTUSER",
                Email = "user@example.com",
                NormalizedEmail = "USER@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            defaultUser.PasswordHash = new PasswordHasher<User>()
                .HashPassword(defaultUser, "Password123");

            builder.Entity<User>().HasData(defaultUser);

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