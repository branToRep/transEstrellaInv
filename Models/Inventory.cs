using System;

namespace transEstrellaInv.Models
{
    public class Inventory
    {
        public string PartNumber { get; set; }
        public string PartType { get; set; }
        public int TotalQuantity { get; set; }
        public decimal AveragePrice { get; set; }     
        public decimal LatestPrice { get; set; }        
        public string PrimaryLocation { get; set; }     
        public string AllLocations { get; set; }        
        public bool MultipleLocations { get; set; }    
        //public int BatchCount { get; set; }// How many separate purchases
        public DateTime? EarliestPurchase { get; set; }
        public DateTime? LatestPurchase { get; set; }
    }

    public class InventoryViewModel
    {
        public int BatchId { get; set; }
        public string PartNumber { get; set; }
        public string PartType { get; set; }
        public int OrderedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int AvailableQuantity => ReceivedQuantity; // Currently available
        public int PendingQuantity => OrderedQuantity - ReceivedQuantity;
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string RackPosition { get; set; }
        public string Seller { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string Status => GetStatus();
        
        private string GetStatus()
        {
            if (ReceivedQuantity == 0) return "🚚 Ordered";
            if (ReceivedQuantity < OrderedQuantity) return "📦 Partial";
            return "✅ In Stock";
        }
        
        public string StatusClass => ReceivedQuantity == 0 ? "warning" : 
                                    (ReceivedQuantity < OrderedQuantity ? "info" : "success");
    }

    public class PurchaseViewModel
    {
        public int BatchId { get; set; }
        public string PartNumber { get; set; }
        public string PartType { get; set; }
        public int OrderedQuantity { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Seller { get; set; }
        public string OrderedBy { get; set; }
        public string BoughtBy { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public int ReceivedQuantity { get; set; }
        public int PendingQuantity => OrderedQuantity - ReceivedQuantity;
    }

    public class TransactionViewModel
    {
        public int TransactionId { get; set; }
        public int BatchId { get; set; }
        public string PartNumber { get; set; }
        public string PartType { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        
        // Personas involucradas
        public string? OrderedBy { get; set; }      // Quién ordenó
        public string? BoughtBy { get; set; }        // Quién compró
        public string? ReceivedBy { get; set; }      // Quién recibió (inbound)
        public string? TruckNumber { get; set; }     // Número de camión (outbound)
        public string? RepairedBy { get; set; }      // Quién reparó (outbound)
        public string? Seller { get; set; }           // Vendedor
        
        // Evidencia
        public string? EvidenceComments { get; set; }
        public List<TransactionPhotoViewModel> Photos { get; set; } = new();
        public int PhotoCount => Photos.Count;
        
        public string TypeDisplay => Type == TransactionType.Incoming ? "📥 Entrada" : "📤 Salida";
        public string TypeClass => Type == TransactionType.Incoming ? "success" : "danger";
    }
}