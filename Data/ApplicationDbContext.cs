using Microsoft.EntityFrameworkCore;
using QcChapWai.Models;

namespace QcChapWai.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<DocumentInspection> DocumentInspections { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DocumentInspection
            modelBuilder.Entity<DocumentInspection>(entity =>
            {
                entity.HasKey(e => e.DocId);
                entity.Property(e => e.DocCreateDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.DocPassed).HasDefaultValue(false);
                entity.Property(e => e.DocHide).HasDefaultValue(false);
                entity.Property(e => e.DocIsLr).HasDefaultValue(false);
                entity.Property(e => e.DocIsMm).HasDefaultValue(false);
                entity.Property(e => e.DocOrderSort).HasDefaultValue(0);
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Role).HasDefaultValue("User");
            });

            // Seed default admin user
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("user123");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@chapwai.com",
                    PasswordHash = adminPasswordHash,
                    Role = "Admin",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Username = "user",
                    Email = "user@chapwai.com",
                    PasswordHash = userPasswordHash,
                    Role = "User",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                }
            );
        }
    }
}