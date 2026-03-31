using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// POST /api/orders — Place a new order from cart items
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerId == userId.Value)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "Your cart is empty." });

            var order = new Order
            {
                CustomerId = userId.Value,
                ShippingAddress = dto.ShippingAddress?.Trim(),
                PaymentMethod = dto.PaymentMethod ?? "Cash on Delivery",
                Status = "Pending",
                PaymentStatus = "Pending",
                OrderDate = DateTime.UtcNow
            };

            decimal total = 0;
            foreach (var cartItem in cartItems)
            {
                var subtotal = cartItem.QuantityKg * cartItem.Product.PricePerKg;
                order.Items.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    QuantityKg = cartItem.QuantityKg,
                    UnitPrice = cartItem.Product.PricePerKg,
                    Subtotal = subtotal
                });
                total += subtotal;
            }

            order.TotalAmount = total;

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order placed successfully!",
                orderId = order.Id,
                totalAmount = order.TotalAmount
            });
        }

        /// <summary>
        /// GET /api/orders — Get orders for current customer
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.CustomerId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentStatus,
                    o.PaymentMethod,
                    o.ShippingAddress,
                    o.OrderDate,
                    Items = o.Items.Select(i => new
                    {
                        i.Id,
                        ProductName = i.Product.Name,
                        i.QuantityKg,
                        i.UnitPrice,
                        i.Subtotal
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// GET /api/orders/all — Admin: get all orders
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    CustomerName = o.Customer.FullName,
                    CustomerEmail = o.Customer.Email,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentStatus,
                    o.PaymentMethod,
                    o.ShippingAddress,
                    o.OrderDate,
                    o.UpdatedAt,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// PUT /api/orders/{id}/status — Admin/Staff: update order status
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(new { message = "Order not found." });

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (dto.PaymentStatus != null)
                order.PaymentStatus = dto.PaymentStatus;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Order #{id} updated to '{dto.Status}'." });
        }

        /// <summary>
        /// GET /api/orders/{id} — Get single order details
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Order not found." });

            // Customers can only see their own orders
            if (role == "Customer" && order.CustomerId != userId)
                return Forbid();

            return Ok(new
            {
                order.Id,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.PaymentMethod,
                order.ShippingAddress,
                order.OrderDate,
                order.UpdatedAt,
                Items = order.Items.Select(i => new
                {
                    ProductName = i.Product.Name,
                    i.QuantityKg,
                    i.UnitPrice,
                    i.Subtotal
                })
            });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    // ---- DTOs ----
    public class PlaceOrderDto
    {
        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
    }
}
