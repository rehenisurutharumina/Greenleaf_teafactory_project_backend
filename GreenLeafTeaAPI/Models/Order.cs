using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenLeafTeaAPI.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; } = null!;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Status: Pending, Confirmed, Processing, Packed, Shipped, Delivered, Cancelled
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
