using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class CategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }
    }
}
