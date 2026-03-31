using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs;
using GreenLeafTeaAPI.DTOs.Auth;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/users — Get all users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    Role = u.Role.Name,
                    u.IsActive,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// POST /api/users — Create user (admin can set role)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return BadRequest(new { message = "Email already exists." });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
            if (role == null)
                return BadRequest(new { message = $"Role '{dto.Role}' not found." });

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = HashPassword(dto.Password),
                Phone = dto.Phone?.Trim(),
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created.", userId = user.Id });
        }

        /// <summary>
        /// PUT /api/users/{id}/toggle — Toggle user active status
        /// </summary>
        [HttpPut("{id:int}/toggle")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {(user.IsActive ? "activated" : "deactivated")}.", isActive = user.IsActive });
        }

        /// <summary>
        /// DELETE /api/users/{id} — Delete user
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted." });
        }

        private static string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
    }

    public class AdminCreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "Customer";
    }
}
