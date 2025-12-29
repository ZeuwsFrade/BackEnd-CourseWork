using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatService.Data;
using ChatService.DTOs;
using ChatService.Services;
using ChatService.Models;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatsController : ControllerBase
    {
        private readonly ChatDbContext _context;
        private readonly IAuthValidationService _authValidationService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(
            ChatDbContext context,
            IAuthValidationService authValidationService,
            ILogger<ChatsController> logger)
        {
            _context = context;
            _authValidationService = authValidationService;
            _logger = logger;
        }

        // Вспомогательный метод для получения информации о пользователе
        private async Task<(bool isValid, UserInfo? userInfo)> GetUserInfoFromTokenAsync()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return (false, null);
                }

                var token = authHeader.Replace("Bearer ", "");

                var isValid = await _authValidationService.ValidateTokenAsync(token);
                if (!isValid)
                {
                    return (false, null);
                }

                var userInfo = await _authValidationService.GetUserInfoAsync(token);
                return (true, userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о пользователе");
                return (false, null);
            }
        }

        // POST: api/chats/create (создание нового чата)
        [HttpPost("create")]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null)
                    return Unauthorized(new { message = "Необходима авторизация" });

                // Валидация модели
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Создание чата
                var chat = new Chat
                {
                    UserId = userInfo.Id,
                    UserName = userInfo.UserName,
                    UserEmail = userInfo.Email,
                    Topic = request.Topic ?? "Общий вопрос",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsClosed = false
                };

                await _context.Chats.AddAsync(chat);
                await _context.SaveChangesAsync();

                // Добавление первого сообщения
                if (!string.IsNullOrEmpty(request.InitialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = userInfo.Id,
                        SenderName = userInfo.UserName,
                        Message = request.InitialMessage,
                        SentAt = DateTime.UtcNow,
                        IsRead = false,
                        IsFromSupport = false
                    };

                    await _context.ChatMessages.AddAsync(message);

                    // Обновляем время последнего сообщения
                    chat.LastMessageAt = DateTime.UtcNow;
                    _context.Chats.Update(chat);

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Чат успешно создан",
                    data = new
                    {
                        chatId = chat.Id,
                        topic = chat.Topic,
                        createdAt = chat.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании чата");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера",
                    error = ex.Message
                });
            }
        }

        // POST: api/chats/send (отправка сообщения)
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null)
                    return Unauthorized(new { message = "Необходима авторизация" });

                // Валидация модели
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var isAdmin = userInfo.Role == "Admin";

                // Поиск чата
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.Id == request.ChatId && c.IsActive);

                if (chat == null)
                    return NotFound(new { message = "Чат не найден" });

                // Проверка прав доступа
                if (!isAdmin && chat.UserId != userInfo.Id)
                    return Forbid();

                // Проверка, закрыт ли чат
                if (chat.IsClosed && !isAdmin)
                    return BadRequest(new { message = "Чат закрыт. Только администратор может отправлять сообщения в закрытый чат" });

                // Создание сообщения
                var message = new ChatMessage
                {
                    ChatId = request.ChatId,
                    SenderId = userInfo.Id,
                    SenderName = userInfo.UserName,
                    Message = request.Message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsFromSupport = isAdmin
                };

                await _context.ChatMessages.AddAsync(message);

                // Обновляем время последнего сообщения
                chat.LastMessageAt = DateTime.UtcNow;
                _context.Chats.Update(chat);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Сообщение успешно отправлено",
                    data = new
                    {
                        messageId = message.Id,
                        sentAt = message.SentAt,
                        isFromSupport = message.IsFromSupport
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/chats/my (получение списка чатов пользователя)
        [HttpGet("my")]
        public async Task<IActionResult> GetMyChats()
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null)
                    return Unauthorized(new { message = "Необходима авторизация" });

                var isAdmin = userInfo.Role == "Admin";

                IQueryable<Chat> query;

                if (isAdmin)
                {
                    // Администратор видит все активные чаты
                    query = _context.Chats
                        .Include(c => c.Messages)
                        .Where(c => c.IsActive);
                }
                else
                {
                    // Пользователь видит только свои чаты
                    query = _context.Chats
                        .Include(c => c.Messages)
                        .Where(c => c.UserId == userInfo.Id && c.IsActive);
                }

                var chats = await query
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Select(c => new ChatResponse
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = isAdmin ? c.UserName : "Вы",
                        UserEmail = isAdmin ? c.UserEmail : null,
                        Topic = c.Topic,
                        CreatedAt = c.CreatedAt,
                        LastMessageAt = c.LastMessageAt,
                        IsActive = c.IsActive,
                        IsClosed = c.IsClosed,
                        UnreadCount = c.Messages.Count(m => !m.IsRead &&
                            (isAdmin ? !m.IsFromSupport : m.IsFromSupport)),
                        LastMessage = c.Messages
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => new ChatMessageResponse
                            {
                                Id = m.Id,
                                ChatId = m.ChatId,
                                SenderId = m.SenderId,
                                SenderName = m.SenderName ?? "Неизвестно",
                                Message = m.Message,
                                SentAt = m.SentAt,
                                IsRead = m.IsRead,
                                IsFromSupport = m.IsFromSupport
                            })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(new ChatListResponse
                {
                    Chats = chats,
                    IsAdmin = isAdmin,
                    TotalCount = chats.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка чатов");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/chats/messages/{chatId} (получение сообщений чата)
        [HttpGet("messages/{chatId}")]
        public async Task<IActionResult> GetChatMessages(int chatId)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null)
                    return Unauthorized(new { message = "Необходима авторизация" });

                var isAdmin = userInfo.Role == "Admin";

                // Поиск чата
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.Id == chatId && c.IsActive);

                if (chat == null)
                    return NotFound(new { message = "Чат не найден" });

                // Проверка прав доступа
                if (!isAdmin && chat.UserId != userInfo.Id)
                    return Forbid();

                // Получение сообщений
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new ChatMessageResponse
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        SenderId = m.SenderId,
                        SenderName = m.SenderName ?? "Неизвестно",
                        Message = m.Message,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        IsFromSupport = m.IsFromSupport
                    })
                    .ToListAsync();

                // Помечаем непрочитанные сообщения как прочитанные (для администратора)
                if (isAdmin)
                {
                    var unreadMessages = messages
                        .Where(m => !m.IsRead && m.SenderId != userInfo.Id)
                        .ToList();

                    foreach (var msg in unreadMessages)
                    {
                        var dbMessage = await _context.ChatMessages.FindAsync(msg.Id);
                        if (dbMessage != null)
                        {
                            dbMessage.IsRead = true;
                            _context.ChatMessages.Update(dbMessage);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        messages,
                        chat = new
                        {
                            chat.Id,
                            chat.UserId,
                            chat.Topic,
                            chat.IsClosed
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении сообщений чата {chatId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // POST: api/chats/close/{chatId} (закрытие чата)
        [HttpPost("close/{chatId}")]
        public async Task<IActionResult> CloseChat(int chatId)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null)
                    return Unauthorized(new { message = "Необходима авторизация" });

                var isAdmin = userInfo.Role == "Admin";

                // Поиск чата
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.Id == chatId && c.IsActive);

                if (chat == null)
                    return NotFound(new { message = "Чат не найден" });

                // Проверка прав доступа
                if (!isAdmin && chat.UserId != userInfo.Id)
                    return Forbid();

                // Закрытие чата
                chat.IsClosed = true;
                chat.IsActive = false;
                _context.Chats.Update(chat);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Чат успешно закрыт"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при закрытии чата {chatId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // POST: api/chats/reopen/{chatId} (повторное открытие чата - только для администратора)
        [HttpPost("reopen/{chatId}")]
        public async Task<IActionResult> ReopenChat(int chatId)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null || userInfo.Role != "Admin")
                    return Unauthorized(new { message = "Требуются права администратора" });

                // Поиск чата
                var chat = await _context.Chats.FindAsync(chatId);

                if (chat == null)
                    return NotFound(new { message = "Чат не найден" });

                // Повторное открытие чата
                chat.IsClosed = false;
                chat.IsActive = true;
                _context.Chats.Update(chat);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Чат успешно открыт"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при открытии чата {chatId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/chats/stats (статистика по чатам - только для администратора)
        [HttpGet("stats")]
        public async Task<IActionResult> GetChatStats()
        {
            try
            {
                // Проверка авторизации
                var (isValid, userInfo) = await GetUserInfoFromTokenAsync();
                if (!isValid || userInfo == null || userInfo.Role != "Admin")
                    return Unauthorized(new { message = "Требуются права администратора" });

                var totalChats = await _context.Chats.CountAsync();
                var activeChats = await _context.Chats
                    .Where(c => c.IsActive && !c.IsClosed)
                    .CountAsync();
                var unreadMessages = await _context.ChatMessages
                    .Where(m => !m.IsRead && !m.IsFromSupport)
                    .CountAsync();
                var closedChats = await _context.Chats
                    .Where(c => c.IsClosed)
                    .CountAsync();

                var stats = new ChatStatsResponse
                {
                    TotalChats = totalChats,
                    ActiveChats = activeChats,
                    UnreadMessages = unreadMessages,
                    ClosedChats = closedChats
                };

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики чатов");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/chats/health (проверка здоровья сервиса)
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "ChatService",
                timestamp = DateTime.UtcNow,
                dependencies = new
                {
                    authService = "http://localhost:5001"
                }
            });
        }
    }
}