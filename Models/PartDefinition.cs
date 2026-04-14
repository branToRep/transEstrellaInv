using System;
using System.ComponentModel.DataAnnotations;

namespace transEstrellaInv.Models
{
    public class PartDefinition
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PartNumber { get; set; } 
        
        [StringLength(30)]
        public string? PartType { get; set; } //Description
        
        //Navigation
        public virtual ICollection<PartInventory> InventoryItems { get; set; } = new List<PartInventory>();
    }

    public class PartDefinitionViewModel
    {
        [Required]
        [StringLength(50)]
        public string PartNumber { get; set; }
        
        [StringLength(30)]
        public string? PartType { get; set; }
    }
}