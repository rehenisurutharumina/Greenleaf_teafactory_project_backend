using System.ComponentModel.DataAnnotations;

namespace GreenLeafTeaAPI.DTOs
{
    public class UpdateStockDto
    {
        [Required]
        [Range(typeof(decimal), "0", "1000000")]
        public decimal QuantityKg { get; set; }

        [Range(typeof(decimal), "0", "1000000")]
        public decimal? ReorderLevelKg { get; set; }
    }
}
