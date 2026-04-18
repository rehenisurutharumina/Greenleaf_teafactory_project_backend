using GreenLeafTeaAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/inventory — List all inventory records
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.Inventories
                .Include(i => i.Product)
                .AsNoTracking()
                .OrderBy(i => i.Product.Name)
                .Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.QuantityKg,
                    i.ReorderLevelKg,
                    IsLow = i.QuantityKg <= i.ReorderLevelKg,
                    i.LastUpdated
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// PUT /api/inventory/{id} — Update stock quantity (staff: qty only, admin: qty + reorder)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null) return NotFound(new { message = "Inventory record not found." });

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            inventory.QuantityKg = dto.QuantityKg;

            // Only Admin can change reorder levels
            if (dto.ReorderLevelKg.HasValue && role == "Admin")
                inventory.ReorderLevelKg = dto.ReorderLevelKg.Value;

            inventory.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock updated." });
        }
    }

    public class UpdateStockDto
    {
        public decimal QuantityKg { get; set; }
        public decimal? ReorderLevelKg { get; set; }
    }
}
