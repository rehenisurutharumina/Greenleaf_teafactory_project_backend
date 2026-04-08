using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(typeof(decimal), "0.1", "100000")]
        public decimal QuantityKg { get; set; } = 1;
    }

    public class UpdateCartDto
    {
        [Required]
        [Range(typeof(decimal), "0.1", "100000")]
        public decimal QuantityKg { get; set; }
    }
}
