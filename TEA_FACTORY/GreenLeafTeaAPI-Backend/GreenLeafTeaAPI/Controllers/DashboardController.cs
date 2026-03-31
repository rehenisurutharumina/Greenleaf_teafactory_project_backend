using GreenLeafTeaAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------
        // GET /api/dashboard/admin — Admin dashboard stats
        // -------------------------------------------------------
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync(p => p.IsAvailable);
            var totalOrders = await _context.Orders.CountAsync();

            var lowStockProducts = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityKg <= i.ReorderLevelKg && i.Product.IsAvailable)
                .Select(i => new
                {
                    ProductName = i.Product.Name,
                    i.QuantityKg,
                    i.ReorderLevelKg
                })
                .ToListAsync();

            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    CustomerName = o.Customer.FullName,
                    o.TotalAmount,
                    o.Status,
                    o.OrderDate
                })
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                totalProducts,
                totalOrders,
                lowStockCount = lowStockProducts.Count,
                lowStockProducts,
                recentOrders
            });
        }

        // -------------------------------------------------------
        // GET /api/dashboard/staff — Staff dashboard stats
        // -------------------------------------------------------
        [HttpGet("staff")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetStaffDashboard()
        {
            var staffId = GetCurrentUserId();
            if (staffId == null) return Unauthorized();

            var assignedTasks = await _context.StaffTasks
                .Where(t => t.StaffId == staffId.Value)
                .CountAsync();

            var pendingTasks = await _context.StaffTasks
                .Where(t => t.StaffId == staffId.Value && t.Status == "Pending")
                .CountAsync();

            var inProgressTasks = await _context.StaffTasks
                .Where(t => t.StaffId == staffId.Value && t.Status == "InProgress")
                .CountAsync();

            var completedTasks = await _context.StaffTasks
                .Where(t => t.StaffId == staffId.Value && t.Status == "Completed")
                .CountAsync();

            var recentTasks = await _context.StaffTasks
                .Include(t => t.Order)
                .Where(t => t.StaffId == staffId.Value)
                .OrderByDescending(t => t.AssignedAt)
                .Take(10)
                .Select(t => new
                {
                    t.Id,
                    t.TaskType,
                    t.Status,
                    OrderId = t.OrderId,
                    t.Notes,
                    t.AssignedAt,
                    t.CompletedAt
                })
                .ToListAsync();

            return Ok(new
            {
                assignedTasks,
                pendingTasks,
                inProgressTasks,
                completedTasks,
                recentTasks
            });
        }

        // -------------------------------------------------------
        // GET /api/dashboard/customer — Customer dashboard stats
        // -------------------------------------------------------
        [HttpGet("customer")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerDashboard()
        {
            var customerId = GetCurrentUserId();
            if (customerId == null) return Unauthorized();

            var totalOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId.Value)
                .CountAsync();

            var activeOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId.Value
                    && o.Status != "Delivered" && o.Status != "Cancelled")
                .CountAsync();

            var cartItems = await _context.CartItems
                .Where(c => c.CustomerId == customerId.Value)
                .CountAsync();

            var recentOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId.Value)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.OrderDate
                })
                .ToListAsync();

            return Ok(new
            {
                totalOrders,
                activeOrders,
                cartItems,
                recentOrders
            });
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
