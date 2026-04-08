using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GreenLeafTeaAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace GreenLeafTeaAPI.Services
{
    public class TokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Generates a JWT token containing user ID, email, full name, and role claims.
        /// Expiry is configurable via JwtSettings.ExpiryHours (default: 24 hours).
        /// </summary>
        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
