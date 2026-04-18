using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        public int StaffId { get; set; }

        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Task type is required.")]
        [MaxLength(50)]
        public string TaskType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
