using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class ContactMessageDto
    {
        public int Id { get; set; }
        public string? SenderName { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class CreateContactMessageDto
    {
        [MaxLength(100)]
        public string? SenderName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(150)]
        public string SenderEmail { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Message cannot be empty.")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters.")]
        [MaxLength(2000, ErrorMessage = "Message is too long (max 2000 characters).")]
        public string Message { get; set; } = string.Empty;
    }
}
