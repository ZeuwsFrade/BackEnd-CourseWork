using Microsoft.AspNetCore.Mvc;
using AuthService.Services;
using AuthService.DTOs;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation($"Регистрация нового пользователя: {request.Email}");

                var result = await _authService.RegisterAsync(request);

                _logger.LogInformation($"Пользователь {request.Email} успешно зарегистрирован");

                return Ok(new
                {
                    success = true,
                    message = "Регистрация успешно завершена",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Ошибка регистрации: {ex.Message}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при регистрации пользователя {request.Email}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation($"Попытка входа: {request.Email}");

                var result = await _authService.LoginAsync(request);

                _logger.LogInformation($"Успешный вход: {request.Email}");

                return Ok(new
                {
                    success = true,
                    message = "Вход выполнен успешно",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Неудачная попытка входа: {request.Email} - {ex.Message}");
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при входе пользователя {request.Email}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                var result = await _authService.ValidateTokenAsync(request.Token);

                return Ok(new
                {
                    success = result.IsValid,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка валидации токена");
                return Ok(new
                {
                    success = false,
                    message = "Токен недействителен"
                });
            }
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _authService.GetUserByIdAsync(id);
                return Ok(new
                {
                    success = true,
                    data = user
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения пользователя с ID {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        [HttpGet("user/email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                var user = await _authService.GetUserByEmailAsync(email);
                return Ok(new
                {
                    success = true,
                    data = user
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения пользователя с email {email}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // Health check endpoint для мониторинга
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "AuthService",
                timestamp = DateTime.UtcNow
            });
        }
    }
}