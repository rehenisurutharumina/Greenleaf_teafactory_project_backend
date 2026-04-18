using System.Security.Cryptography;
using System.Text;

namespace GreenLeafTeaAPI.Services
{
    /// <summary>
    /// Centralized password hashing and verification.
    /// Uses HMACSHA512 with a random salt per password.
    /// Stored format: base64(salt):base64(hash)
    /// </summary>
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string storedHash)
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
    }
}
