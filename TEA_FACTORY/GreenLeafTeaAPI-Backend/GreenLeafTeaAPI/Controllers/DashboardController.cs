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
        // GET /api/dashboard/admin — Admin dashboard stats (enhanced)
        // -------------------------------------------------------
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync(p => p.IsAvailable);
            var totalOrders = await _context.Orders.CountAsync();
            var totalCategories = await _context.Categories.CountAsync(c => c.IsActive);

            // Revenue
            var totalRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .SumAsync(o => o.TotalAmount);

            // Pending quotes
            var pendingQuotes = await _context.QuoteRequests
                .CountAsync(q => q.Status == "Pending");

            // Unread messages
            var unreadMessages = await _context.ContactMessages
                .CountAsync(m => !m.IsRead);

            // Low stock
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

            // Recent orders
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
                    o.PaymentStatus,
                    o.OrderDate
                })
                .ToListAsync();

            // Order status breakdown
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            var shippedOrders = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered");

            return Ok(new
            {
                totalUsers,
                totalProducts,
                totalOrders,
                totalCategories,
                totalRevenue,
                pendingQuotes,
                unreadMessages,
                lowStockCount = lowStockProducts.Count,
                lowStockProducts,
                recentOrders,
                orderBreakdown = new
                {
                    pending = pendingOrders,
                    processing = processingOrders,
                    shipped = shippedOrders,
                    delivered = deliveredOrders
                }
            });
        }

        // -------------------------------------------------------
        // GET /api/dashboard/analytics — Admin analytics data
        // -------------------------------------------------------
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnalytics()
        {
            var now = DateTime.UtcNow;

            // ── Monthly Sales (last 12 months) ──
            var twelveMonthsAgo = now.AddMonths(-11);
            var startDate = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var rawOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.PaymentStatus == "Paid")
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var monthlySales = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var month = startDate.AddMonths(i);
                    var monthEnd = month.AddMonths(1);
                    var revenue = rawOrders
                        .Where(o => o.OrderDate >= month && o.OrderDate < monthEnd)
                        .Sum(o => o.TotalAmount);
                    return new
                    {
                        month = month.ToString("MMM yyyy"),
                        revenue
                    };
                })
                .ToList();

            // ── Order Trends (last 12 months — count per month) ──
            var allOrdersRaw = await _context.Orders
                .Where(o => o.OrderDate >= startDate)
                .Select(o => new { o.OrderDate })
                .ToListAsync();

            var orderTrends = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var month = startDate.AddMonths(i);
                    var monthEnd = month.AddMonths(1);
                    var count = allOrdersRaw.Count(o => o.OrderDate >= month && o.OrderDate < monthEnd);
                    return new
                    {
                        month = month.ToString("MMM yyyy"),
                        orders = count
                    };
                })
                .ToList();

            // ── Top Selling Products (by total quantity sold) ──
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.PaymentStatus == "Paid" || oi.Order.Status != "Cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new
                {
                    productName = g.Key.Name,
                    totalQuantityKg = g.Sum(oi => oi.QuantityKg),
                    totalRevenue = g.Sum(oi => oi.Subtotal),
                    orderCount = g.Select(oi => oi.OrderId).Distinct().Count()
                })
                .OrderByDescending(x => x.totalRevenue)
                .Take(10)
                .ToListAsync();

            // ── Low Stock Overview (all inventory with levels) ──
            var stockOverview = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product.IsAvailable)
                .Select(i => new
                {
                    productName = i.Product.Name,
                    quantityKg = i.QuantityKg,
                    reorderLevelKg = i.ReorderLevelKg,
                    isLow = i.QuantityKg <= i.ReorderLevelKg,
                    percentOfReorder = i.ReorderLevelKg > 0
                        ? Math.Round((double)(i.QuantityKg / i.ReorderLevelKg) * 100, 1)
                        : 100.0
                })
                .OrderBy(x => x.percentOfReorder)
                .ToListAsync();

            // ── Quote Trends (last 6 months) ──
            var sixMonthsAgo = new DateTime(now.AddMonths(-5).Year, now.AddMonths(-5).Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var quotesRaw = await _context.QuoteRequests
                .Where(q => q.SubmittedAt >= sixMonthsAgo)
                .Select(q => new { q.SubmittedAt, q.Status })
                .ToListAsync();

            var quoteTrends = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var month = sixMonthsAgo.AddMonths(i);
                    var monthEnd = month.AddMonths(1);
                    var monthQuotes = quotesRaw.Where(q => q.SubmittedAt >= month && q.SubmittedAt < monthEnd).ToList();
                    return new
                    {
                        month = month.ToString("MMM yyyy"),
                        total = monthQuotes.Count,
                        pending = monthQuotes.Count(q => q.Status == "Pending"),
                        replied = monthQuotes.Count(q => q.Status == "Replied")
                    };
                })
                .ToList();

            // ── Revenue KPI ──
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var thisMonthRevenue = rawOrders
                .Where(o => o.OrderDate >= thisMonthStart)
                .Sum(o => o.TotalAmount);

            var lastMonthRevenue = rawOrders
                .Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < thisMonthStart)
                .Sum(o => o.TotalAmount);

            var revenueGrowth = lastMonthRevenue > 0
                ? Math.Round((double)((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1)
                : (thisMonthRevenue > 0 ? 100.0 : 0.0);

            var thisMonthOrders = allOrdersRaw.Count(o => o.OrderDate >= thisMonthStart);
            var lastMonthOrders = allOrdersRaw.Count(o => o.OrderDate >= lastMonthStart && o.OrderDate < thisMonthStart);
            var orderGrowth = lastMonthOrders > 0
                ? Math.Round((double)(thisMonthOrders - lastMonthOrders) / lastMonthOrders * 100, 1)
                : (thisMonthOrders > 0 ? 100.0 : 0.0);

            return Ok(new
            {
                monthlySales,
                orderTrends,
                topProducts,
                stockOverview,
                quoteTrends,
                kpi = new
                {
                    thisMonthRevenue,
                    lastMonthRevenue,
                    revenueGrowth,
                    thisMonthOrders,
                    lastMonthOrders,
                    orderGrowth
                }
            });
        }

        // -------------------------------------------------------
        // GET /api/dashboard/notifications — Admin notifications
        // -------------------------------------------------------
        [HttpGet("notifications")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNotifications()
        {
            var now = DateTime.UtcNow;
            var notifications = new List<object>();

            // ── Low Stock Alerts ──
            var lowStockItems = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityKg <= i.ReorderLevelKg && i.Product.IsAvailable)
                .Select(i => new
                {
                    i.Product.Name,
                    i.QuantityKg,
                    i.ReorderLevelKg,
                    i.LastUpdated
                })
                .ToListAsync();

            foreach (var item in lowStockItems)
            {
                var pct = item.ReorderLevelKg > 0 ? Math.Round((double)(item.QuantityKg / item.ReorderLevelKg) * 100, 0) : 0;
                notifications.Add(new
                {
                    type = "low_stock",
                    severity = pct < 30 ? "critical" : "warning",
                    title = $"Low Stock: {item.Name}",
                    message = $"{item.QuantityKg:F1} kg remaining (reorder at {item.ReorderLevelKg:F1} kg)",
                    timestamp = item.LastUpdated,
                    icon = "alert-triangle"
                });
            }

            // ── New Orders (last 48 hours) ──
            var recentCutoff = now.AddHours(-48);
            var newOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate >= recentCutoff)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    o.Id,
                    CustomerName = o.Customer.FullName,
                    o.TotalAmount,
                    o.Status,
                    o.OrderDate
                })
                .ToListAsync();

            foreach (var order in newOrders)
            {
                notifications.Add(new
                {
                    type = "new_order",
                    severity = "info",
                    title = $"New Order #{order.Id}",
                    message = $"{order.CustomerName} placed an order worth ${order.TotalAmount:F2}",
                    timestamp = order.OrderDate,
                    icon = "shopping-bag",
                    status = order.Status
                });
            }

            // ── New Quote Requests (last 48 hours) ──
            var newQuotes = await _context.QuoteRequests
                .Where(q => q.SubmittedAt >= recentCutoff && q.Status == "Pending")
                .OrderByDescending(q => q.SubmittedAt)
                .Take(10)
                .Select(q => new
                {
                    q.Id,
                    q.CustomerName,
                    q.ProductName,
                    q.QuantityKg,
                    q.SubmittedAt
                })
                .ToListAsync();

            foreach (var quote in newQuotes)
            {
                notifications.Add(new
                {
                    type = "new_quote",
                    severity = "info",
                    title = $"Quote Request #{quote.Id}",
                    message = $"{quote.CustomerName} requested {quote.QuantityKg} kg of {quote.ProductName}",
                    timestamp = quote.SubmittedAt,
                    icon = "file-text"
                });
            }

            // ── New / Unread Contact Messages ──
            var unreadMessages = await _context.ContactMessages
                .Where(m => !m.IsRead)
                .OrderByDescending(m => m.ReceivedAt)
                .Take(10)
                .Select(m => new
                {
                    m.Id,
                    m.SenderName,
                    m.SenderEmail,
                    m.Subject,
                    m.ReceivedAt
                })
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                notifications.Add(new
                {
                    type = "new_message",
                    severity = "info",
                    title = $"Message from {msg.SenderName ?? msg.SenderEmail}",
                    message = msg.Subject ?? "No subject",
                    timestamp = msg.ReceivedAt,
                    icon = "mail"
                });
            }

            // Sort all notifications by timestamp descending
            var sorted = notifications
                .OrderByDescending(n => ((dynamic)n).timestamp)
                .ToList();

            // Summary counts
            var summary = new
            {
                lowStock = lowStockItems.Count,
                newOrders = newOrders.Count,
                newQuotes = newQuotes.Count,
                unreadMessages = unreadMessages.Count,
                total = sorted.Count
            };

            return Ok(new
            {
                notifications = sorted,
                summary
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

            // Inventory summary for staff quick-view
            var lowStockItems = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityKg <= i.ReorderLevelKg && i.Product.IsAvailable)
                .Select(i => new
                {
                    productName = i.Product.Name,
                    quantityKg = i.QuantityKg,
                    reorderLevelKg = i.ReorderLevelKg
                })
                .ToListAsync();

            var totalProducts = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product.IsAvailable)
                .CountAsync();

            // Completion rate
            var completionRate = assignedTasks > 0
                ? Math.Round((double)completedTasks / assignedTasks * 100, 1)
                : 0.0;

            return Ok(new
            {
                assignedTasks,
                pendingTasks,
                inProgressTasks,
                completedTasks,
                completionRate,
                recentTasks,
                inventorySummary = new
                {
                    totalProducts,
                    lowStockCount = lowStockItems.Count,
                    lowStockItems
                }
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
                    o.PaymentStatus,
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
