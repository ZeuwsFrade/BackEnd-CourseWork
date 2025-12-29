using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // Индексы для оптимизации поиска
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.InStock);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Добавляем тестовые данные при создании БД
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Ноутбук Lenovo IdeaPad",
                    Description = "15.6-дюймовый ноутбук с процессором Intel Core i5",
                    Price = 54999.99m,
                    Category = "Электроника",
                    ImageUrl = "https://example.com/images/laptop.jpg",
                    InStock = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    IsActive = true
                },
                new Product
                {
                    Id = 2,
                    Name = "Смартфон Samsung Galaxy S23",
                    Description = "Флагманский смартфон с камерой 200 МП",
                    Price = 89999.99m,
                    Category = "Электроника",
                    ImageUrl = "https://example.com/images/phone.jpg",
                    InStock = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                },
                new Product
                {
                    Id = 3,
                    Name = "Кроссовки Nike Air Max",
                    Description = "Спортивная обувь для бега и повседневной носки",
                    Price = 12999.99m,
                    Category = "Одежда и обувь",
                    ImageUrl = "https://example.com/images/shoes.jpg",
                    InStock = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    IsActive = true
                }
            );
        }
    }
}