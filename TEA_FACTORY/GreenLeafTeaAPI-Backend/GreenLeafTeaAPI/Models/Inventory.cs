using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenLeafTeaAPI.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal QuantityKg { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ReorderLevelKg { get; set; } = 50.0m;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
