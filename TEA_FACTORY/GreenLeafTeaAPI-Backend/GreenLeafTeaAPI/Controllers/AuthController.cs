using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs.Auth;
using GreenLeafTeaAPI.Models;
using GreenLeafTeaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // -------------------------------------------------------
        // POST /api/auth/register
        // Creates a new customer/staff account
        // -------------------------------------------------------
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();

            // Check if email already exists
            var exists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (exists)
            {
                ModelState.AddModelError(nameof(dto.Email), "An account with this email already exists.");
                return ValidationProblem(ModelState);
            }

            var requestedRole = dto.Role?.Trim();
            var roleName = string.IsNullOrWhiteSpace(requestedRole) ? "Customer" : requestedRole;

            // Public registration is limited to Customer or Staff accounts.
            if (!roleName.Equals("Customer", StringComparison.OrdinalIgnoreCase) &&
                !roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(dto.Role), "Role must be either Customer or Staff.");
                return ValidationProblem(ModelState);
            }

            var targetRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (targetRole == null)
            {
                return StatusCode(500, new { message = "System roles not initialized." });
            }

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = HashPassword(dto.Password),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim(),
                RoleId = targetRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Load the role for token generation
            user.Role = targetRole;

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = ToUserInfo(user)
            });
        }

        // -------------------------------------------------------
        // POST /api/auth/login
        // Authenticates a user and returns a JWT token
        // -------------------------------------------------------
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Your account has been deactivated. Contact admin." });
            }

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = ToUserInfo(user)
            });
        }

        // -------------------------------------------------------
        // GET /api/auth/me
        // Returns the current authenticated user's profile
        // -------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(ToUserInfo(user));
        }

        // -------------------------------------------------------
        // PUT /api/auth/profile
        // Updates the current user's profile
        // -------------------------------------------------------
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (dto.Phone != null)
                user.Phone = dto.Phone.Trim();

            if (dto.Address != null)
                user.Address = dto.Address.Trim();

            await _context.SaveChangesAsync();

            return Ok(ToUserInfo(user));
        }

        // -------------------------------------------------------
        // PUT /api/auth/change-password
        // Changes the current user's password
        // -------------------------------------------------------
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(dto.CurrentPassword), "Current password is incorrect.");
                return ValidationProblem(ModelState);
            }

            user.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }

        // -------------------------------------------------------
        // Helper: Hash password using HMACSHA512
        // -------------------------------------------------------
        private static string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            // Store salt:hash as base64
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return computedHash.SequenceEqual(hash);
        }

        // -------------------------------------------------------
        // Helper: Get current user ID from JWT claims
        // -------------------------------------------------------
        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out var id) ? id : null;
        }

        private static UserInfoDto ToUserInfo(User user)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role.Name
            };
        }
    }
}
