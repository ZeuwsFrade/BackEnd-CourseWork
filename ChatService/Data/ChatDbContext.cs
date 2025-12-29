using Microsoft.EntityFrameworkCore;
using ChatService.Models;

namespace ChatService.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация для Chat
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Topic)
                    .HasMaxLength(200)
                    .HasDefaultValue("Общий вопрос");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UserName)
                    .HasMaxLength(100);

                entity.Property(e => e.UserEmail)
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.IsClosed)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Индексы
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsClosed);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastMessageAt);

                // Связь с ChatMessages
                entity.HasMany(e => e.Messages)
                    .WithOne(m => m.Chat)
                    .HasForeignKey(m => m.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация для ChatMessage
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ChatId)
                    .IsRequired();

                entity.Property(e => e.SenderId)
                    .IsRequired();

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.SentAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.IsFromSupport)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.SenderName)
                    .HasMaxLength(100);

                // Индексы
                entity.HasIndex(e => e.ChatId);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.SentAt);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.IsFromSupport);
            });

            // Добавляем тестовые данные
            modelBuilder.Entity<Chat>().HasData(
                new Chat
                {
                    Id = 1,
                    UserId = 2,
                    Topic = "Вопрос о доставке",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    LastMessageAt = DateTime.UtcNow.AddHours(-3),
                    IsActive = true,
                    IsClosed = false,
                    UserName = "customer1",
                    UserEmail = "customer1@example.com"
                },
                new Chat
                {
                    Id = 2,
                    UserId = 3,
                    Topic = "Проблема с товаром",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    LastMessageAt = DateTime.UtcNow.AddHours(-1),
                    IsActive = true,
                    IsClosed = false,
                    UserName = "customer2",
                    UserEmail = "customer2@example.com"
                }
            );

            modelBuilder.Entity<ChatMessage>().HasData(
                new ChatMessage
                {
                    Id = 1,
                    ChatId = 1,
                    SenderId = 2,
                    Message = "Здравствуйте! Когда будет доставлен мой заказ?",
                    SentAt = DateTime.UtcNow.AddDays(-2).AddHours(2),
                    IsRead = true,
                    IsFromSupport = false,
                    SenderName = "customer1"
                },
                new ChatMessage
                {
                    Id = 2,
                    ChatId = 1,
                    SenderId = 1, // Админ
                    Message = "Добрый день! Ваш заказ будет доставлен завтра с 10:00 до 14:00.",
                    SentAt = DateTime.UtcNow.AddDays(-2).AddHours(3),
                    IsRead = true,
                    IsFromSupport = true,
                    SenderName = "admin"
                },
                new ChatMessage
                {
                    Id = 3,
                    ChatId = 2,
                    SenderId = 3,
                    Message = "Получил товар с дефектом. Что делать?",
                    SentAt = DateTime.UtcNow.AddDays(-1).AddHours(1),
                    IsRead = false,
                    IsFromSupport = false,
                    SenderName = "customer2"
                }
            );
        }
    }
}