using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/cart — Get current user's cart items
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerId == userId.Value)
                .OrderByDescending(c => c.AddedAt)
                .Select(c => new
                {
                    c.Id,
                    c.ProductId,
                    ProductName = c.Product.Name,
                    ProductBadge = c.Product.Badge,
                    PricePerKg = c.Product.PricePerKg,
                    c.QuantityKg,
                    Subtotal = c.QuantityKg * c.Product.PricePerKg,
                    c.AddedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// POST /api/cart — Add item to cart
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.IsAvailable);

            if (product == null)
                return NotFound(new { message = "Product not found or unavailable." });

            // Check if product already in cart
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == userId.Value && c.ProductId == dto.ProductId);

            if (existing != null)
            {
                existing.QuantityKg += dto.QuantityKg;
                existing.AddedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CustomerId = userId.Value,
                    ProductId = dto.ProductId,
                    QuantityKg = dto.QuantityKg,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Item added to cart." });
        }

        /// <summary>
        /// PUT /api/cart/{id} — Update cart item quantity
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == userId.Value);

            if (item == null) return NotFound(new { message = "Cart item not found." });

            item.QuantityKg = dto.QuantityKg;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart updated." });
        }

        /// <summary>
        /// DELETE /api/cart/{id} — Remove item from cart
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == userId.Value);

            if (item == null) return NotFound(new { message = "Cart item not found." });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart." });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
