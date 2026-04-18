using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs.Auth;
using GreenLeafTeaAPI.Models;
using GreenLeafTeaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        // Creates a new customer account (public registration)
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

            // Public registration always creates a Customer account.
            // Staff and Admin accounts are created via the admin panel only.
            var roleName = "Customer";

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
                PasswordHash = PasswordHelper.Hash(dto.Password),
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

            if (user == null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
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
                return Unauthorized(new { message = "Invalid token." });

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
                return Unauthorized(new { message = "Invalid token." });

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
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (!PasswordHelper.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(dto.CurrentPassword), "Current password is incorrect.");
                return ValidationProblem(ModelState);
            }

            user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
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
