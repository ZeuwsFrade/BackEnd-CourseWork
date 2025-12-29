using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatService.Models
{
    public class Chat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // ID из Auth Service

        [StringLength(200)]
        public string Topic { get; set; } = "Общий вопрос";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastMessageAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsClosed { get; set; } = false;

        // Информация о пользователе (кэшированная из Auth Service)
        [StringLength(100)]
        public string? UserName { get; set; }

        [StringLength(100)]
        public string? UserEmail { get; set; }

        // Навигационное свойство
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}