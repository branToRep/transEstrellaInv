using System;
using System.ComponentModel.DataAnnotations;

namespace transEstrellaInv.Models
{
    [Obsolete("Use PartDefinition and PartInventory instead")]
    public class Part
    {
        public int Id {get ; set;}

        [Required]
        [StringLength(50)]
        public string PartNumber {get; set;}

        [StringLength(30)]
        public string? PartType {get; set;}

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity {get; set;}

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price {get; set;}

        public decimal PriceMXN {get; set;}

        public decimal PriceUSD {get; set;}

        public string Currency { get; set;}

        private DateTime? _exchangeRateDate;
        public DateTime? ExchangeRateDate
        {
            get => _exchangeRateDate;
            set => _exchangeRateDate = value.HasValue 
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) 
                : null;
        }

        [StringLength(100)]
        public string? RackPosition {get; set;}

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}