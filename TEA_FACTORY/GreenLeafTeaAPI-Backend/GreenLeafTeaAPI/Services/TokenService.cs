using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GreenLeafTeaAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace GreenLeafTeaAPI.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates a JWT token containing user ID, email, and role claims.
        /// Token is valid for 24 hours.
        /// </summary>
        public string GenerateToken(User user)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured.");
            var issuer = _config["Jwt:Issuer"] ?? "GreenLeafTeaAPI";
            var audience = _config["Jwt:Audience"] ?? "GreenLeafTeaFrontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
