using GreenLeafTeaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GreenLeafTeaAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ---- Core Tables ----
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<StaffTask> StaffTasks { get; set; }

        // ---- Public Form Tables (existing) ----
        public DbSet<QuoteRequest> QuoteRequests { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============================================
            // Table configurations
            // =============================================

            // Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(r => r.Name).IsUnique();
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
                entity.Property(u => u.Phone).HasMaxLength(20);
                entity.Property(u => u.Address).HasMaxLength(500);

                entity.HasOne(u => u.Role)
                    .WithMany()
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.ImageUrl).HasMaxLength(300);
            });

            // Product (enhanced)
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Grade).HasMaxLength(50);
                entity.Property(p => p.Badge).HasMaxLength(50);
                entity.Property(p => p.ImageUrl).HasMaxLength(300);
                entity.Property(p => p.PricePerKg).HasPrecision(10, 2);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Inventory
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.Property(i => i.QuantityKg).HasPrecision(10, 2);
                entity.Property(i => i.ReorderLevelKg).HasPrecision(10, 2);

                entity.HasOne(i => i.Product)
                    .WithOne()
                    .HasForeignKey<Inventory>(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalAmount).HasPrecision(12, 2);
                entity.Property(o => o.Status).IsRequired().HasMaxLength(50);
                entity.Property(o => o.ShippingAddress).HasMaxLength(500);
                entity.Property(o => o.PaymentMethod).HasMaxLength(50);
                entity.Property(o => o.PaymentStatus).HasMaxLength(50);

                entity.HasOne(o => o.Customer)
                    .WithMany()
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.QuantityKg).HasPrecision(10, 2);
                entity.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
                entity.Property(oi => oi.Subtotal).HasPrecision(12, 2);

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CartItem
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.Property(ci => ci.QuantityKg).HasPrecision(10, 2);

                entity.HasOne(ci => ci.Customer)
                    .WithMany()
                    .HasForeignKey(ci => ci.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StaffTask
            modelBuilder.Entity<StaffTask>(entity =>
            {
                entity.Property(t => t.TaskType).IsRequired().HasMaxLength(50);
                entity.Property(t => t.Status).IsRequired().HasMaxLength(50);
                entity.Property(t => t.Notes).HasMaxLength(500);

                entity.HasOne(t => t.Staff)
                    .WithMany()
                    .HasForeignKey(t => t.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Order)
                    .WithMany()
                    .HasForeignKey(t => t.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Contact Message (enhanced)
            modelBuilder.Entity<ContactMessage>(entity =>
            {
                entity.Property(m => m.SenderName).HasMaxLength(100);
                entity.Property(m => m.SenderEmail).IsRequired().HasMaxLength(150);
                entity.Property(m => m.Subject).HasMaxLength(200);
                entity.Property(m => m.Message).IsRequired().HasMaxLength(2000);
            });

            // Quote Request (enhanced with QuotedAmount)
            modelBuilder.Entity<QuoteRequest>(entity =>
            {
                entity.Property(q => q.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(q => q.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(q => q.Email).HasMaxLength(150);
                entity.Property(q => q.Phone).HasMaxLength(20);
                entity.Property(q => q.Status).HasMaxLength(50);
                entity.Property(q => q.AdminNotes).HasMaxLength(500);
                entity.Property(q => q.QuotedAmount).HasPrecision(12, 2);
            });

            // =============================================
            // Seed Data
            // =============================================

            // Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Staff" },
                new Role { Id = 3, Name = "Customer" }
            );

            // Default Admin + Staff Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "System Admin",
                    Email = "admin@greenleaf.com",
                    PasswordHash = HashPasswordForSeed("Admin@123"),
                    Phone = "+94 77 000 0000",
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    FullName = "Factory Staff",
                    Email = "staff@greenleaf.com",
                    PasswordHash = HashPasswordForSeed("Staff@123"),
                    Phone = "+94 77 111 1111",
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Black Tea",
                    Description = "Fully oxidized tea with strong color, flavor, and aroma.",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 2,
                    Name = "Green Tea",
                    Description = "Minimally oxidized tea with light taste and clean flavor.",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 3,
                    Name = "Tea Dust & Fannings",
                    Description = "Fine particles ideal for tea bags and quick brewing.",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Products (updated with CategoryId)
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Black Tea (OP/BOP)",
                    Description = "Strong color and aroma, ideal for daily tea. Our flagship product.",
                    Grade = "OP/BOP",
                    PricePerKg = 4.50m,
                    IsAvailable = true,
                    Badge = "Best Seller",
                    CategoryId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 2,
                    Name = "Green Tea",
                    Description = "Light taste, minimal oxidation, clean flavor. Carefully processed.",
                    Grade = "Green",
                    PricePerKg = 6.00m,
                    IsAvailable = true,
                    Badge = "Fresh",
                    CategoryId = 2,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 3,
                    Name = "Dust / Fannings",
                    Description = "Fast brewing, suitable for tea bags and bulk supply.",
                    Grade = "Dust",
                    PricePerKg = 2.80m,
                    IsAvailable = true,
                    Badge = "Bulk",
                    CategoryId = 3,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Inventory for each product
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory
                {
                    Id = 1,
                    ProductId = 1,
                    QuantityKg = 500.00m,
                    ReorderLevelKg = 50.00m,
                    LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Inventory
                {
                    Id = 2,
                    ProductId = 2,
                    QuantityKg = 300.00m,
                    ReorderLevelKg = 30.00m,
                    LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Inventory
                {
                    Id = 3,
                    ProductId = 3,
                    QuantityKg = 1000.00m,
                    ReorderLevelKg = 100.00m,
                    LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        /// <summary>
        /// Hash password for seed data. Uses a fixed salt so the seed is deterministic.
        /// </summary>
        private static string HashPasswordForSeed(string password)
        {
            // Use a deterministic salt for seed data so migrations stay consistent
            var salt = Encoding.UTF8.GetBytes("GreenLeafTeaSeedSalt1234567890!!");
            using var hmac = new HMACSHA512(salt);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
    }
}
