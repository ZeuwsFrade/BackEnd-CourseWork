using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 200 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 1000000, ErrorMessage = "Цена должна быть от 0.01 до 1 000 000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Категория обязательна")]
        [StringLength(100, ErrorMessage = "Категория не должна превышать 100 символов")]
        public string Category { get; set; } = string.Empty;

        [Url(ErrorMessage = "Некорректный URL изображения")]
        public string? ImageUrl { get; set; }

        public bool InStock { get; set; } = true;
    }

    public class UpdateProductRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Url]
        public string? ImageUrl { get; set; }

        public bool InStock { get; set; }
    }

    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool InStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductListResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}