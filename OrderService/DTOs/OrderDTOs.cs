using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Номер заказа обязателен")]
        public int OrderNumber { get; set; }

        [Required(ErrorMessage = "Адрес доставки обязателен")]
        [StringLength(500, ErrorMessage = "Адрес не должен превышать 500 символов")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Дата доставки обязательна")]
        public DateTime DeliveryDate { get; set; }

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [StringLength(20, ErrorMessage = "Телефон не должен превышать 20 символов")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Общая сумма обязательна")]
        [Range(0.01, 1000000, ErrorMessage = "Сумма должна быть от 0.01 до 1 000 000")]
        public decimal TotalAmount { get; set; }

        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        [Required(ErrorMessage = "ID товара обязателен")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, 1000, ErrorMessage = "Количество должно быть от 1 до 1000")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 100000, ErrorMessage = "Цена должна быть от 0.01 до 100 000")]
        public decimal Price { get; set; }
    }

    public class OrderResponse
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public string Phone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemResponse> Items { get; set; } = new();
    }

    public class OrderItemResponse
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Статус обязателен")]
        [StringLength(50, ErrorMessage = "Статус не должен превышать 50 символов")]
        public string Status { get; set; } = string.Empty;
    }

    public class OrderListResponse
    {
        public List<OrderResponse> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}