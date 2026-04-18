using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenLeafTeaAPI.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Grade { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0.01", "100000.00")]
        public decimal PricePerKg { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        // Category relationship (nullable for backward compat with existing data)
        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(50)]
        public string? Badge { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
