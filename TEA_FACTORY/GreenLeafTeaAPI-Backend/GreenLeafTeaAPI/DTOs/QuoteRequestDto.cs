// ============================================================
// DTOs/QuoteRequestDto.cs — DTOs for Quote Requests
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    // ----------------------------------------------------------
    // QuoteRequestDto — Response DTO
    // Sent back to the client after saving a quote request.
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
        public DateTime SubmittedAt { get; set; }
    }

    // ----------------------------------------------------------
    // CreateQuoteRequestDto — Request DTO
    // This is the shape of data the frontend POSTS when
    // the visitor submits the "Get a Quick Quote" form.
    //
    // Validation rules here are checked automatically by
    // ASP.NET Core before your controller code even runs.
    // If validation fails, it returns a 400 Bad Request.
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

        // Email is optional but must be valid format if provided
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}
