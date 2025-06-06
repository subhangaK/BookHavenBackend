﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Book_Haven.Entities;
using System.Reflection;

namespace Book_Haven
{
    public class ApplicationDbContext : IdentityDbContext<User, Roles, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Wishlist relationships
            builder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Wishlist>()
                .HasOne(w => w.Book)
                .WithMany()
                .HasForeignKey(w => w.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.BookId })
                .IsUnique();

            // Configure Cart relationships
            builder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Cart>()
                .HasOne(c => c.Book)
                .WithMany()
                .HasForeignKey(c => c.BookId)
                .OnDelete(DeleteBehavior.Cascade);

           
            // Configure Order relationships
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasOne(o => o.Book)
                .WithMany()
                .HasForeignKey(o => o.BookId)
                .OnDelete(DeleteBehavior.Cascade);

       

            builder.Entity<Roles>().HasData(
                new Roles
                {
                    Id = 1,
                    Name = "SuperAdmin",
                    NormalizedName = "SUPERADMIN"
                },
                new Roles
                {
                    Id = 2,
                    Name = "Staff",
                    NormalizedName = "STAFF"
                }
            );

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

            var staff = new User
            {
                Id = 2,
                UserName = "staff",
                NormalizedUserName = "STAFF",
                Email = "staff@gmail.com",
                NormalizedEmail = "STAFF@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            staff.PasswordHash = new PasswordHasher<User>()
                .HashPassword(staff, "Staff@123");

            builder.Entity<User>().HasData(superAdmin, staff);

            builder.Entity<IdentityUserRole<long>>().HasData(
                new IdentityUserRole<long>
                {
                    RoleId = 1,
                    UserId = 1
                },
                new IdentityUserRole<long>
                {
                    RoleId = 2,
                    UserId = 2
                }
            );
        }
    }
}