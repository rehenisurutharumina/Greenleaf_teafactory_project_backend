// ============================================================
// Models/QuoteRequest.cs — Quote Request entity
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenLeafTeaAPI.Models
{
    public class QuoteRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, 100000)]
        public int QuantityKg { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        /// <summary>
        /// The amount quoted by the admin for this request
        /// </summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal? QuotedAmount { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
