using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.Models
{
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? SenderName { get; set; }

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Subject { get; set; }

        [Required]
        [MinLength(10)]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
