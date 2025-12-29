using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly IAuthValidationService _authValidationService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ProductDbContext context,
            IAuthValidationService authValidationService,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _authValidationService = authValidationService;
            _logger = logger;
        }

        private async Task<(bool isValid, string role)> CheckAuthorizationAsync()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return (false, "User");

            var isValid = await _authValidationService.ValidateTokenAsync(token);
            var role = await _authValidationService.GetUserRoleAsync(token);

            return (isValid, role);
        }

        // GET: api/products (публичный доступ)
        [HttpGet]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] string? category = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Products.Where(p => p.IsActive);

                // Фильтрация
                if (!string.IsNullOrEmpty(category))
                    query = query.Where(p => p.Category == category);

                if (inStock.HasValue)
                    query = query.Where(p => p.InStock == inStock.Value);

                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(search) ||
                        p.Description != null && p.Description.ToLower().Contains(search));
                }

                // Пагинация
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        InStock = p.InStock,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ProductListResponse
                {
                    Products = products,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка товаров");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // GET: api/products/{id} (публичный доступ)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return NotFound(new { message = "Товар не найден" });

                var response = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    InStock = product.InStock,
                    CreatedAt = product.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении товара {id}");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // POST: api/products (только для администраторов)
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                // Проверка авторизации
                var (isValid, role) = await CheckAuthorizationAsync();
                if (!isValid || role != "Admin")
                    return Unauthorized(new { message = $"Требуются права администратора. Текущая роль: {role} + {isValid}" });

                // Валидация модели
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Category = request.Category,
                    ImageUrl = request.ImageUrl,
                    InStock = request.InStock,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Товар успешно создан",
                    productId = product.Id,
                    product = new ProductResponse
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Category = product.Category,
                        ImageUrl = product.ImageUrl,
                        InStock = product.InStock,
                        CreatedAt = product.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании товара");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // PUT: api/products/{id} (только для администраторов)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                // Проверка авторизации
                var (isValid, role) = await CheckAuthorizationAsync();
                if (!isValid || role != "Admin")
                    return Unauthorized(new { message = "Требуются права администратора" });

                var product = await _context.Products.FindAsync(id);
                if (product == null || !product.IsActive)
                    return NotFound(new { message = "Товар не найден" });

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Category = request.Category;
                product.ImageUrl = request.ImageUrl;
                product.InStock = request.InStock;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Товар успешно обновлен",
                    product = new ProductResponse
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Category = product.Category,
                        ImageUrl = product.ImageUrl,
                        InStock = product.InStock,
                        CreatedAt = product.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении товара {id}");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // DELETE: api/products/{id} (только для администраторов)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                // Проверка авторизации
                var (isValid, role) = await CheckAuthorizationAsync();
                if (!isValid || role != "Admin")
                    return Unauthorized(new { message = "Требуются права администратора" });

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { message = "Товар не найден" });

                // Мягкое удаление (изменяем флаг IsActive)
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Товар успешно удален" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении товара {id}");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // GET: api/products/categories (публичный доступ)
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении категорий");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // GET: api/products/health (публичный доступ)
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "ProductService",
                timestamp = DateTime.UtcNow
            });
        }
    }
}