// ============================================================
// Models/QuoteRequest.cs — Quote Request entity
// ============================================================
// This represents a row in the QuoteRequests table.
// Submitted when a visitor fills out the "Get a Quick Quote"
// form on the frontend homepage.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.Models
{
    public class QuoteRequest
    {
        // Auto-generated Primary Key
        [Key]
        public int Id { get; set; }

        // The name of the person requesting the quote
        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        // Which product they are interested in
        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        // How many kilograms they want
        [Required]
        [Range(1, 100000)] // Between 1kg and 100,000kg
        public int QuantityKg { get; set; }

        // Optional email for us to reply to
        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        // Optional phone number
        [MaxLength(20)]
        public string? Phone { get; set; }

        // Status of the quote: Pending / Reviewed / Replied
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Admin notes (filled in later by staff)
        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        // When the form was submitted
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
