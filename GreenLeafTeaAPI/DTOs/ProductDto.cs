using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Grade { get; set; }
        public decimal PricePerKg { get; set; }
        public bool IsAvailable { get; set; }
        public string? Badge { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Grade { get; set; }

        [Required]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10,000.")]
        public decimal PricePerKg { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(50)]
        public string? Badge { get; set; }

        public int? CategoryId { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }
    }
}
