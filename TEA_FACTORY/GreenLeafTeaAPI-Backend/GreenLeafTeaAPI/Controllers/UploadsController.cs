using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenLeafTeaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            return BadRequest(new { message = "Invalid file type. Only jpg, jpeg, png, gif, webp are allowed." });
        }

        // Limit size to 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "File too large. Maximum size is 5MB." });
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + ext;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return the URL relative to the domain
        var fileUrl = $"/uploads/{uniqueFileName}";
        
        // Return full url
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
        var fullUrl = $"{baseUrl}{fileUrl}";

        return Ok(new { url = fullUrl });
    }
}
