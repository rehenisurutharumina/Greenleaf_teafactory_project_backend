using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuoteRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuoteRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<QuoteRequestDto>> SubmitQuote([FromBody] CreateQuoteRequestDto dto)
        {
            var customerName = dto.CustomerName.Trim();
            var productName = dto.ProductName.Trim();

            if (string.IsNullOrWhiteSpace(customerName))
            {
                ModelState.AddModelError(nameof(dto.CustomerName), "Your name is required.");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                ModelState.AddModelError(nameof(dto.ProductName), "Please select a product.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var productExists = await _context.Products
                .AsNoTracking()
                .AnyAsync(product => product.IsAvailable && product.Name.ToLower() == productName.ToLower());

            if (!productExists)
            {
                ModelState.AddModelError(nameof(dto.ProductName), "Selected product is not available.");
                return ValidationProblem(ModelState);
            }

            var quoteRequest = new QuoteRequest
            {
                CustomerName = customerName,
                ProductName = productName,
                QuantityKg = dto.QuantityKg,
                Email = Normalize(dto.Email),
                Phone = Normalize(dto.Phone),
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.QuoteRequests.Add(quoteRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuote), new { id = quoteRequest.Id }, ToDto(quoteRequest));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuoteRequestDto>>> GetAllQuotes()
        {
            var quotes = await _context.QuoteRequests
                .AsNoTracking()
                .OrderByDescending(quote => quote.SubmittedAt)
                .Select(quote => ToDto(quote))
                .ToListAsync();

            return Ok(quotes);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<QuoteRequestDto>> GetQuote(int id)
        {
            var quote = await _context.QuoteRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            if (quote == null)
            {
                return NotFound(new { message = $"Quote request #{id} not found." });
            }

            return Ok(ToDto(quote));
        }

        private static QuoteRequestDto ToDto(QuoteRequest quote)
        {
            return new QuoteRequestDto
            {
                Id = quote.Id,
                CustomerName = quote.CustomerName,
                ProductName = quote.ProductName,
                QuantityKg = quote.QuantityKg,
                Email = quote.Email,
                Phone = quote.Phone,
                Status = quote.Status,
                SubmittedAt = quote.SubmittedAt
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
