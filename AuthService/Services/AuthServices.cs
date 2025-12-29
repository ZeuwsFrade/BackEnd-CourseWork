using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<ValidateTokenResponse> ValidateTokenAsync(string token);
        Task<UserResponse> GetUserByIdAsync(int userId);
        Task<UserResponse> GetUserByEmailAsync(string email);
    }

    public class AuthServices : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthServices(AuthDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Проверка существования пользователя
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new ArgumentException("Пользователь с таким email уже существует");

            if (await _context.Users.AnyAsync(u => u.UserName == request.Username))
                throw new ArgumentException("Пользователь с таким именем уже существует");

            // Создание пользователя
            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                Role = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Генерация токена
            var token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Username = user.UserName,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddHours(5)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверный email или пароль");

            // Генерация токена
            var token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Username = user.UserName,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddHours(5)
            };
        }

        public async Task<ValidateTokenResponse> ValidateTokenAsync(string token)
        {
            var principal = _jwtService.ValidateToken(token);

            if (principal == null)
                return new ValidateTokenResponse { IsValid = false };

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return new ValidateTokenResponse { IsValid = false };

            // Проверяем существование пользователя в БД
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return new ValidateTokenResponse { IsValid = false };

            return new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userId.ToString(),
                Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User",
                Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? ""
            };
        }

        public async Task<UserResponse> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.IsActive)
                throw new KeyNotFoundException("Пользователь не найден");

            return new UserResponse
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserResponse> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
                throw new KeyNotFoundException("Пользователь не найден");

            return new UserResponse
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }
    }
}