using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class PlaceOrderDto
    {
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PaymentStatus { get; set; }
    }
}
