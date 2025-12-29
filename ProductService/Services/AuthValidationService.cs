namespace ProductService.Services
    {
        public interface IAuthValidationService
        {
            Task<bool> ValidateTokenAsync(string token);
            Task<string> GetUserRoleAsync(string token);
        }

        public class AuthValidationService : IAuthValidationService
        {
            private readonly HttpClient _httpClient;
            private readonly ILogger<AuthValidationService> _logger;

            public AuthValidationService(HttpClient httpClient, ILogger<AuthValidationService> logger)
            {
                _httpClient = httpClient;
                _logger = logger;

                // Базовый адрес Auth Service
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
                        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<ValidateTokenData>>();
                        return wrapper?.Success == true && wrapper.Data?.IsValid == true;
                    }

                    _logger.LogWarning($"Ошибка валидации токена: {response.StatusCode}");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при вызове Auth Service для валидации токена");
                    return false;
                }
            }

            public async Task<string> GetUserRoleAsync(string token)
            {
                try
                {
                    var request = new { Token = token };
                    var response = await _httpClient.PostAsJsonAsync("api/auth/validate-token", request);

                    if (response.IsSuccessStatusCode)
                    {
                        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<ValidateTokenData>>();
                        if (wrapper?.Success == true)
                        {
                            return wrapper.Data?.Role ?? "User";
                        }
                    }

                    _logger.LogWarning($"Не удалось получить роль пользователя: {response.StatusCode}");
                    return "User";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при вызове Auth Service для получения роли");
                    return "User";
                }
            }

            // Классы для десериализации ответа
            private class ApiResponseWrapper<T>
            {
                public bool Success { get; set; }
                public T? Data { get; set; }
                public string? Message { get; set; }
            }

            private class ValidateTokenData
            {
                public bool IsValid { get; set; }
                public string UserId { get; set; } = string.Empty;
                public string Role { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;
            }
        }
    }