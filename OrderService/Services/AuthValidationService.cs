using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Services
{
    public interface IAuthValidationService
    {
        Task<bool> ValidateTokenAsync(string token);
        Task<(int? userId, string? email, string role)> GetUserInfoAsync(string token);
    }

    public class AuthValidationService : IAuthValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthValidationService> _logger;

        public AuthValidationService(HttpClient httpClient, ILogger<AuthValidationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri("http://localhost:5001/");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var request = new { Token = token };
                var response = await _httpClient.PostAsJsonAsync("api/auth/validate-token", request);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataElement))
                        {
                            if (dataElement.TryGetProperty("IsValid", out var isValidElement))
                            {
                                return isValidElement.GetBoolean();
                            }
                        }
                    }
                }

                _logger.LogWarning($"Auth Service вернул ошибку: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при вызове Auth Service");
                return false;
            }
        }

        public async Task<(int? userId, string? email, string role)> GetUserInfoAsync(string token)
        {
            try
            {
                var request = new { Token = token };
                var response = await _httpClient.PostAsJsonAsync("api/auth/validate-token", request);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean())
                    {
                        if (root.TryGetProperty("data", out var dataElement))
                        {
                            int? userId = null;
                            string? email = null;
                            string role = "User";

                            if (dataElement.TryGetProperty("UserId", out var userIdElement) &&
                                int.TryParse(userIdElement.GetString(), out var parsedUserId))
                            {
                                userId = parsedUserId;
                            }

                            if (dataElement.TryGetProperty("Email", out var emailElement))
                            {
                                email = emailElement.GetString();
                            }

                            if (dataElement.TryGetProperty("Role", out var roleElement))
                            {
                                role = roleElement.GetString() ?? "User";
                            }

                            return (userId, email, role);
                        }
                    }
                }

                return (null, null, "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о пользователе");
                return (null, null, "User");
            }
        }
    }
}