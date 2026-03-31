using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/products — Public: list available products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.Name)
                .Select(p => ToDto(p))
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// GET /api/products/all — Admin: list ALL products (including unavailable)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => ToDto(p))
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// GET /api/products/{id} — Public: get single product
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return Ok(ToDto(product));
        }

        /// <summary>
        /// POST /api/products — Admin: create product
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
        {
            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(dto.Name), "Product name is required.");
                return ValidationProblem(ModelState);
            }

            var exists = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(dto.Name), "A product with this name already exists.");
                return ValidationProblem(ModelState);
            }

            var product = new Product
            {
                Name = name,
                Description = Normalize(dto.Description),
                Grade = Normalize(dto.Grade),
                PricePerKg = dto.PricePerKg,
                IsAvailable = dto.IsAvailable,
                Badge = Normalize(dto.Badge),
                CategoryId = dto.CategoryId,
                ImageUrl = Normalize(dto.ImageUrl),
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Also create an inventory record
            _context.Inventories.Add(new Inventory
            {
                ProductId = product.Id,
                QuantityKg = 0,
                ReorderLevelKg = 50,
                LastUpdated = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product));
        }

        /// <summary>
        /// PUT /api/products/{id} — Admin: update product
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (!string.IsNullOrWhiteSpace(dto.Name))
                product.Name = dto.Name.Trim();
            if (dto.Description != null)
                product.Description = Normalize(dto.Description);
            if (dto.Grade != null)
                product.Grade = Normalize(dto.Grade);
            if (dto.PricePerKg > 0)
                product.PricePerKg = dto.PricePerKg;
            if (dto.Badge != null)
                product.Badge = Normalize(dto.Badge);
            if (dto.CategoryId.HasValue)
                product.CategoryId = dto.CategoryId;
            if (dto.ImageUrl != null)
                product.ImageUrl = Normalize(dto.ImageUrl);

            product.IsAvailable = dto.IsAvailable;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Product updated." });
        }

        /// <summary>
        /// DELETE /api/products/{id} — Admin: soft-delete product
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            product.IsAvailable = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ProductDto ToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Grade = product.Grade,
                PricePerKg = product.PricePerKg,
                IsAvailable = product.IsAvailable,
                Badge = product.Badge,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                ImageUrl = product.ImageUrl,
                CreatedAt = product.CreatedAt
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
