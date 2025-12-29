using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Services
{
    public interface IProductValidationService
    {
        Task<bool> ValidateProductExistsAsync(int productId);
        Task<decimal?> GetProductPriceAsync(int productId);
        Task<string?> GetProductNameAsync(int productId);
    }

    public class ProductValidationService : IProductValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductValidationService> _logger;

        public ProductValidationService(HttpClient httpClient, ILogger<ProductValidationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri("http://localhost:5149/"); // ProductService порт
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<bool> ValidateProductExistsAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // Проверяем, что товар активен и существует
                    return !responseString.Contains("\"message\":\"Товар не найден\"");
                }

                _logger.LogWarning($"Product Service вернул ошибку: {response.StatusCode} для товара {productId}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке товара {productId}");
                return false;
            }
        }

        public async Task<decimal?> GetProductPriceAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("price", out var priceElement) &&
                        priceElement.TryGetDecimal(out var price))
                    {
                        return price;
                    }

                    // Альтернативный путь поиска цены
                    if (root.TryGetProperty("Price", out var priceElement2) &&
                        priceElement2.TryGetDecimal(out var price2))
                    {
                        return price2;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении цены товара {productId}");
                return null;
            }
        }

        public async Task<string?> GetProductNameAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("name", out var nameElement))
                    {
                        return nameElement.GetString();
                    }

                    if (root.TryGetProperty("Name", out var nameElement2))
                    {
                        return nameElement2.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении названия товара {productId}");
                return null;
            }
        }
    }
}