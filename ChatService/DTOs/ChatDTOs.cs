using System.ComponentModel.DataAnnotations;

namespace ChatService.DTOs
{
    public class CreateChatRequest
    {
        [StringLength(200, ErrorMessage = "Тема не должна превышать 200 символов")]
        public string? Topic { get; set; }

        [Required(ErrorMessage = "Первое сообщение обязательно")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Сообщение должно быть от 1 до 2000 символов")]
        public string InitialMessage { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        [Required(ErrorMessage = "ID чата обязателен")]
        public int ChatId { get; set; }

        [Required(ErrorMessage = "Сообщение обязательно")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Сообщение должно быть от 1 до 2000 символов")]
        public string Message { get; set; } = string.Empty;
    }

    public class ChatMessageResponse
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsFromSupport { get; set; }
    }

    public class ChatResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string Topic { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
        public int UnreadCount { get; set; }
        public ChatMessageResponse? LastMessage { get; set; }
    }

    public class ChatListResponse
    {
        public List<ChatResponse> Chats { get; set; } = new();
        public bool IsAdmin { get; set; }
        public int TotalCount { get; set; }
    }

    public class ChatStatsResponse
    {
        public int TotalChats { get; set; }
        public int ActiveChats { get; set; }
        public int UnreadMessages { get; set; }
        public int ClosedChats { get; set; }
    }
}