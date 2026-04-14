using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transEstrellaInv.Models
{
    public enum TransactionType
    {
        Incoming,
        Outgoing,
    }

    public class Transaction
    {
        public int Id {get; set;}

        //For definition table
        [Required]
        public int PartInventoryId { get; set; }  // Link to specific batch, not PartDefinition
        public virtual PartInventory PartInventory { get; set; } = null!;

        [Required]
        public TransactionType Type {get; set;}

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity {get; set;}     

        private DateTime _transactionDate;
        public DateTime TransactionDate 
        { 
            get => _transactionDate;
            set => _transactionDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        

        private DateTime? _receivedDate;
        public DateTime? ReceivedDate
        {
            get => _receivedDate;
            set => _receivedDate = value.HasValue 
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) 
                : null;
        }        

        [Required]
        [StringLength(50)]
        public string? PartReceivedBy {get; set;}        

        [StringLength(50)]
        public string? EvidenceSubmittedBy {get; set;}

        [StringLength(4000)]
        public string? EvidenceComments {get; set;} 
        
        [StringLength(50)]
        public string? TruckNumber {get; set;}
        [NotMapped]
        public bool IsTruckNumberRequired => Type == TransactionType.Outgoing;
         
        [StringLength(50)]
        public string? RepairedBy {get; set;}  

        //Media
        public virtual ICollection<TransactionPhoto> Photos { get; set; } = new List<TransactionPhoto>();           

        //Helper properties 
        [NotMapped]
        public List<IFormFile> PhotoFiles { get; set; } = new List<IFormFile>();
    }

}