using System.Net.Http.Json;
using System.Text.Json;
using ChatService.Models;

namespace ChatService.Services
{
    public interface IAuthValidationService
    {
        Task<bool> ValidateTokenAsync(string token);
        Task<UserInfo?> GetUserInfoAsync(string token);
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

        public async Task<UserInfo?> GetUserInfoAsync(string token)
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
                            var userInfo = new UserInfo();

                            if (dataElement.TryGetProperty("UserId", out var userIdElement) &&
                                int.TryParse(userIdElement.GetString(), out var userId))
                            {
                                userInfo.Id = userId;
                            }

                            if (dataElement.TryGetProperty("Email", out var emailElement))
                            {
                                userInfo.Email = emailElement.GetString() ?? string.Empty;
                            }

                            if (dataElement.TryGetProperty("Role", out var roleElement))
                            {
                                userInfo.Role = roleElement.GetString() ?? "User";
                            }

                            // Получаем имя пользователя из Auth Service
                            var userNameResponse = await _httpClient.GetAsync($"api/auth/user/{userInfo.Id}");
                            if (userNameResponse.IsSuccessStatusCode)
                            {
                                var userNameString = await userNameResponse.Content.ReadAsStringAsync();
                                using var userNameDoc = JsonDocument.Parse(userNameString);
                                var userNameRoot = userNameDoc.RootElement;

                                if (userNameRoot.TryGetProperty("data", out var userDataElement))
                                {
                                    if (userDataElement.TryGetProperty("Username", out var usernameElement))
                                    {
                                        userInfo.UserName = usernameElement.GetString() ?? string.Empty;
                                    }
                                    else if (userDataElement.TryGetProperty("UserName", out var usernameElement2))
                                    {
                                        userInfo.UserName = usernameElement2.GetString() ?? string.Empty;
                                    }
                                }
                            }

                            return userInfo;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о пользователе");
                return null;
            }
        }
    }
}