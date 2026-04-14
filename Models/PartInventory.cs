using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transEstrellaInv.Models
{
    public class PartInventory
    {
        public int Id { get; set; }
        
        [Required]
        public int PartDefinitionId { get; set; } //FK to PartDefinition
        
        [Required]
        public int Quantity { get; set; }

        public int OrderedQuantity { get; set; }  // Total ordered 
        public int ReceivedQuantity { get; set; } // Total received 
        [NotMapped]
        public int AvailableQuantity => ReceivedQuantity;  // Simple version
        
        [Required]
        public decimal Price { get; set; } //Original price 
        
        public decimal PriceMXN { get; set; }
        
        public decimal PriceUSD { get; set; }
        
        public string CurrencyCode { get; set; } = null!;
        
        public DateTime? ExchangeRateDate { get; set; }

        [StringLength(50)]
        public string? PartOrderedBy {get; set;}


        public DateTime? ReceivedDate { get; set; }

        [StringLength(200)]
        public string? Seller { get; set; }
        
        [StringLength(100)]
        public string? RackPosition { get; set; }

        public string PartBoughtBy {get; set;}  

        public DateTime? PurchaseDate { get; set; }

        //Active or inactive (for discontinued parts)
        public bool IsActive { get; set; } = true;  
        public DateTime? DeactivatedAt { get; set; }
        
        //Navigation 
        public virtual PartDefinition PartDefinition { get; set; } =null!;
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

        public class PartCreateViewModel
    {
        [Required]
        public string PartType { get; set; }

        public int PartDefinitionId { get; set; }

        public bool IsExistingStock { get; set; }
        
        [Required]
        public string PartNumber { get; set; }

        [StringLength(50)]
        public string? PartOrderedBy {get; set;}

        public string PartBoughtBy {get; set;}   

        public DateTime? PurchaseDate { get; set; }

        [StringLength(200)]
        public string? Seller { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        public string Currency { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        public int OrderedQuantity { get; set; }  // Total ordered 
        public int ReceivedQuantity { get; set; } // Total received 
        
        public string? RackPosition { get; set; }
        
        public DateTime? ReceivedDate { get; set; }
    }

    public class PartBatchViewModel
    {
        //For existing part selection
        public int? PartDefinitionId { get; set; }
        
        public string? PartNumber { get; set; }

        public string? PartType { get; set; }

        [StringLength(50)]
        public string? PartOrderedBy {get; set;}

        public DateTime? PurchaseDate { get; set; }

        [Required]
        [StringLength(50)]
        public string PartBoughtBy {get; set;}   

        public bool IsNewDefinition { get; set; }
        
        [StringLength(50)]
        public string? NewPartNumber { get; set; }
        
        [StringLength(30)]
        public string? NewPartType { get; set; }
        
        // Batch fields
        [Required]
        public int OrderedQuantity { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public string Currency { get; set; } = "MXN";
        
        [StringLength(100)]
        public string? RackPosition { get; set; }

        public string? Seller { get; set; }

    }

    public class EditBatchViewModel
    {
        public int Id { get; set; }
        public int PartDefinitionId { get; set; }
        
        public string PartNumber { get; set; }
        
        public string PartType { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        public string Currency { get; set; }
        
        public string? RackPosition { get; set; }
    }
}