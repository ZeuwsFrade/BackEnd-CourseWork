using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IAuthValidationService _authValidationService;
        private readonly IProductValidationService _productValidationService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            OrderDbContext context,
            IAuthValidationService authValidationService,
            IProductValidationService productValidationService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _authValidationService = authValidationService;
            _productValidationService = productValidationService;
            _logger = logger;
        }

        // Вспомогательный метод для проверки авторизации
        private async Task<(bool isValid, int? userId, string? email, string role)> CheckAuthorizationAsync()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Отсутствует заголовок Authorization");
                    return (false, null, null, "User");
                }

                var token = authHeader.Replace("Bearer ", "");

                var isValid = await _authValidationService.ValidateTokenAsync(token);
                if (!isValid)
                {
                    return (false, null, null, "User");
                }

                var (userId, email, role) = await _authValidationService.GetUserInfoAsync(token);
                return (true, userId, email, role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке авторизации");
                return (false, null, null, "User");
            }
        }

        // POST: api/orders (создание заказа)
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userId, userEmail, role) = await CheckAuthorizationAsync();
                if (!isValid)
                    return Unauthorized(new { message = "Необходима авторизация" });

                // Валидация модели
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Проверка даты доставки
                if (request.DeliveryDate.Date < DateTime.Today)
                    return BadRequest(new { message = "Дата доставки не может быть в прошлом" });

                // Проверка наличия товаров
                foreach (var item in request.Items)
                {
                    var productExists = await _productValidationService.ValidateProductExistsAsync(item.ProductId);
                    if (!productExists)
                    {
                        return BadRequest(new { message = $"Товар с ID {item.ProductId} не найден" });
                    }

                    // Можно дополнительно проверить цену
                    var productPrice = await _productValidationService.GetProductPriceAsync(item.ProductId);
                    if (productPrice.HasValue && productPrice.Value != item.Price)
                    {
                        _logger.LogWarning($"Цена товара {item.ProductId} изменилась. Ожидалось: {productPrice}, получено: {item.Price}");
                    }
                }

                // Проверка уникальности номера заказа
                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber);

                if (existingOrder != null)
                {
                    return BadRequest(new { message = $"Заказ с номером {request.OrderNumber} уже существует" });
                }

                // Создание заказа
                var order = new Models.Order
                {
                    OrderNumber = request.OrderNumber,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryDate = request.DeliveryDate,
                    Phone = request.Phone,
                    TotalAmount = request.TotalAmount,
                    UserId = userId,
                    UserEmail = userEmail,
                    CreatedAt = DateTime.UtcNow,
                    Status = "New",
                    IsActive = true
                };

                // Добавление позиций заказа
                foreach (var item in request.Items)
                {
                    var orderItem = new Models.OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };

                    order.Items.Add(orderItem);
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Формирование ответа
                var orderResponse = new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryDate = order.DeliveryDate,
                    Phone = order.Phone,
                    TotalAmount = order.TotalAmount,
                    UserId = order.UserId,
                    UserEmail = order.UserEmail,
                    CreatedAt = order.CreatedAt,
                    Status = order.Status,
                    Items = order.Items.Select(i => new OrderItemResponse
                    {
                        Id = i.Id,
                        OrderId = i.OrderId,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };

                return Ok(new
                {
                    success = true,
                    message = "Заказ успешно создан",
                    data = orderResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера",
                    error = ex.Message
                });
            }
        }

        // GET: api/orders (получение списка заказов)
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userId, _, role) = await CheckAuthorizationAsync();
                if (!isValid)
                    return Unauthorized(new { message = "Необходима авторизация" });

                IQueryable<Models.Order> query = _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.IsActive);

                // Для обычных пользователей показываем только их заказы
                if (role != "Admin" && userId.HasValue)
                {
                    query = query.Where(o => o.UserId == userId.Value);
                }

                // Фильтрация
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (startDate.HasValue)
                    query = query.Where(o => o.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.CreatedAt <= endDate.Value);

                // Пагинация
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        DeliveryAddress = o.DeliveryAddress,
                        DeliveryDate = o.DeliveryDate,
                        Phone = o.Phone,
                        TotalAmount = o.TotalAmount,
                        UserId = o.UserId,
                        UserEmail = o.UserEmail,
                        CreatedAt = o.CreatedAt,
                        Status = o.Status,
                        Items = o.Items.Select(i => new OrderItemResponse
                        {
                            Id = i.Id,
                            OrderId = i.OrderId,
                            ProductId = i.ProductId,
                            ProductName = i.ProductName,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new OrderListResponse
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка заказов");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/orders/{id} (получение заказа по ID)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userId, _, role) = await CheckAuthorizationAsync();
                if (!isValid)
                    return Unauthorized(new { message = "Необходима авторизация" });

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                // Проверка прав доступа
                if (role != "Admin" && order.UserId != userId)
                    return Forbid();

                var orderResponse = new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryDate = order.DeliveryDate,
                    Phone = order.Phone,
                    TotalAmount = order.TotalAmount,
                    UserId = order.UserId,
                    UserEmail = order.UserEmail,
                    CreatedAt = order.CreatedAt,
                    Status = order.Status,
                    Items = order.Items.Select(i => new OrderItemResponse
                    {
                        Id = i.Id,
                        OrderId = i.OrderId,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };

                return Ok(orderResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении заказа {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // PATCH: api/orders/{id}/status (обновление статуса заказа)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                // Проверка авторизации - только админ может менять статус
                var (isValid, _, _, role) = await CheckAuthorizationAsync();
                if (!isValid || role != "Admin")
                    return Unauthorized(new { message = "Требуются права администратора" });

                var order = await _context.Orders.FindAsync(id);
                if (order == null || !order.IsActive)
                    return NotFound(new { message = "Заказ не найден" });

                order.Status = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Статус заказа обновлен на '{request.Status}'"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении статуса заказа {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // DELETE: api/orders/{id} (мягкое удаление заказа)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                // Проверка авторизации
                var (isValid, userId, _, role) = await CheckAuthorizationAsync();
                if (!isValid)
                    return Unauthorized(new { message = "Необходима авторизация" });

                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                // Проверка прав доступа
                if (role != "Admin" && order.UserId != userId)
                    return Forbid();

                // Мягкое удаление
                order.IsActive = false;
                order.UpdatedAt = DateTime.UtcNow;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Заказ успешно удален"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении заказа {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера"
                });
            }
        }

        // GET: api/orders/statuses (получение доступных статусов)
        [HttpGet("statuses")]
        public IActionResult GetOrderStatuses()
        {
            var statuses = new[]
            {
                "New",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            return Ok(statuses);
        }

        // GET: api/orders/health (проверка здоровья сервиса)
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "OrderService",
                timestamp = DateTime.UtcNow,
                dependencies = new
                {
                    authService = "http://localhost:5001",
                    productService = "http://localhost:5149"
                }
            });
        }
    }
}