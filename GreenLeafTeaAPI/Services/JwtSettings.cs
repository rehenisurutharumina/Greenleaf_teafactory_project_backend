namespace GreenLeafTeaAPI.Services
{
    /// <summary>
    /// Strongly-typed JWT configuration.
    /// Registered as singleton in Program.cs and injected into TokenService.
    /// </summary>
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "GreenLeafTeaAPI";
        public string Audience { get; set; } = "GreenLeafTeaFrontend";
        public int ExpiryHours { get; set; } = 24;
    }
}
