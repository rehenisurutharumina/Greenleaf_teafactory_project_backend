using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StaffTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffTasksController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/stafftasks — Staff: get my tasks / Admin: get all tasks
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetTasks()
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.StaffTasks
                .Include(t => t.Staff)
                .Include(t => t.Order)
                .AsNoTracking();

            if (role == "Staff")
                query = query.Where(t => t.StaffId == userId!.Value);

            var tasks = await query
                .OrderByDescending(t => t.AssignedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TaskType,
                    t.Status,
                    t.Notes,
                    StaffName = t.Staff.FullName,
                    OrderId = t.OrderId,
                    t.AssignedAt,
                    t.CompletedAt
                })
                .ToListAsync();

            return Ok(tasks);
        }

        /// <summary>
        /// POST /api/stafftasks — Admin: create/assign a task
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            var staff = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == dto.StaffId && u.Role.Name == "Staff");

            if (staff == null)
                return BadRequest(new { message = "Staff member not found." });

            var task = new StaffTask
            {
                StaffId = dto.StaffId,
                OrderId = dto.OrderId,
                TaskType = dto.TaskType,
                Status = "Pending",
                Notes = dto.Notes?.Trim(),
                AssignedAt = DateTime.UtcNow
            };

            _context.StaffTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Task assigned.", taskId = task.Id });
        }

        /// <summary>
        /// PUT /api/stafftasks/{id}/status — Staff/Admin: update task status
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusDto dto)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var task = await _context.StaffTasks.FindAsync(id);
            if (task == null) return NotFound(new { message = "Task not found." });

            // Staff can only update their own tasks
            if (role == "Staff" && task.StaffId != userId)
                return Forbid();

            task.Status = dto.Status;
            if (dto.Status == "Completed")
                task.CompletedAt = DateTime.UtcNow;
            if (dto.Notes != null)
                task.Notes = dto.Notes.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Task updated." });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public class CreateTaskDto
    {
        public int StaffId { get; set; }
        public int? OrderId { get; set; }
        public string TaskType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
