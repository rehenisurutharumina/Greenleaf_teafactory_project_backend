// ============================================================
// DTOs/QuoteRequestDto.cs — DTOs for Quote Requests
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    // ----------------------------------------------------------
    // QuoteRequestDto — Response DTO
    // ----------------------------------------------------------
    public class QuoteRequestDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QuantityKg { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AdminNotes { get; set; }
        public decimal? QuotedAmount { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    // ----------------------------------------------------------
    // UpdateQuoteStatusDto — Admin updates quote status
    // ----------------------------------------------------------
    public class UpdateQuoteStatusDto
    {
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        public decimal? QuotedAmount { get; set; }
    }

    // ----------------------------------------------------------
    // CreateQuoteRequestDto — Request DTO for submitting quotes
    // ----------------------------------------------------------
    public class CreateQuoteRequestDto
    {
        [Required(ErrorMessage = "Your name is required.")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
        [MaxLength(100, ErrorMessage = "Name is too long (max 100 characters).")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a product.")]
        [MinLength(2, ErrorMessage = "Product name must be at least 2 characters.")]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100000, ErrorMessage = "Quantity must be between 1 and 100,000 kg.")]
        public int QuantityKg { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}
