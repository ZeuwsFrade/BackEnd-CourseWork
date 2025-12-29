using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация для Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OrderNumber)
                    .IsRequired();

                entity.Property(e => e.DeliveryAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.DeliveryDate)
                    .IsRequired();

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.UserEmail)
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("New");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // Индексы
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UserEmail);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.DeliveryDate);
                entity.HasIndex(e => e.IsActive);

                // Связь с OrderItems
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация для OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductId)
                    .IsRequired();

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                // Индексы
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
            });

            // Добавляем тестовые данные
            modelBuilder.Entity<Order>().HasData(
                new Order
                {
                    Id = 1,
                    OrderNumber = 1001,
                    DeliveryAddress = "ул. Примерная, д. 1, кв. 10",
                    DeliveryDate = DateTime.UtcNow.AddDays(3),
                    Phone = "+79991234567",
                    TotalAmount = 67999.98m,
                    UserId = 1,
                    UserEmail = "customer@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Status = "Processing",
                    IsActive = true
                }
            );

            modelBuilder.Entity<OrderItem>().HasData(
                new OrderItem
                {
                    Id = 1,
                    OrderId = 1,
                    ProductId = 1,
                    ProductName = "Ноутбук Lenovo IdeaPad",
                    Quantity = 1,
                    Price = 54999.99m
                },
                new OrderItem
                {
                    Id = 2,
                    OrderId = 1,
                    ProductId = 3,
                    ProductName = "Кроссовки Nike Air Max",
                    Quantity = 1,
                    Price = 12999.99m
                }
            );
        }
    }
}