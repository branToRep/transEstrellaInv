using Microsoft.EntityFrameworkCore;
using transEstrellaInv.Models;

namespace transEstrellaInv.Data
{
    public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options)
                : base(options)
            {
            }
            
            //public DbSet<Part> Parts { get; set; } //OBSOLETE
            public DbSet<Transaction> Transactions { get; set; }
            public DbSet<TransactionPhoto> TransactionPhotos { get; set; }
            public DbSet<PartDefinition> PartDefinitions { get; set; }
            public DbSet<PartInventory> PartInventories { get; set; }
            
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                
                // PartDefinition
                modelBuilder.Entity<PartDefinition>(entity =>
                {
                    entity.HasIndex(p => p.PartNumber).IsUnique();
                    entity.Property(p => p.PartNumber).IsRequired().HasMaxLength(50);
                    entity.Property(p => p.PartType).HasMaxLength(30);
                });
                
                // PartInventory
                modelBuilder.Entity<PartInventory>(entity =>
                {
                    entity.HasOne(i => i.PartDefinition)
                        .WithMany(d => d.InventoryItems)
                        .HasForeignKey(i => i.PartDefinitionId)
                        .OnDelete(DeleteBehavior.Restrict);
                    
                    entity.Property(p => p.Price).HasPrecision(18, 2);
                    entity.Property(p => p.PriceMXN).HasPrecision(18, 2);
                    entity.Property(p => p.PriceUSD).HasPrecision(18, 2);
                    entity.Property(p => p.RackPosition).HasMaxLength(100);
                    entity.Property(p => p.Seller).HasMaxLength(200);
                    entity.Property(e => e.PartOrderedBy).HasMaxLength(100);                    
                    entity.Property(e => e.PartBoughtBy).HasMaxLength(100);
                    entity.Property(e => e.Seller).HasMaxLength(100);
                    //entity.Ignore(e => e.OrderDate);

                    entity.HasIndex(i => i.PartDefinitionId);
                    entity.HasIndex(i => i.RackPosition);
                });
                
                // Transaction
                modelBuilder.Entity<Transaction>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    
                    entity.HasOne(e => e.PartInventory)
                        .WithMany(i => i.Transactions)
                        .HasForeignKey(e => e.PartInventoryId)
                        .OnDelete(DeleteBehavior.Restrict);
                    entity.Property(e => e.TransactionDate).IsRequired();
                    entity.Property(e => e.EvidenceComments).HasMaxLength(4000);
                    entity.Property(e => e.PartReceivedBy).HasMaxLength(100);
                    entity.Property(e => e.TruckNumber).HasMaxLength(50);
                    entity.Property(e => e.RepairedBy).HasMaxLength(100);
    
                    entity.HasIndex(e => e.TransactionDate);
                    entity.HasIndex(e => e.Type);
                });
                
                // TransactionPhoto
                modelBuilder.Entity<TransactionPhoto>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    
                    entity.HasOne(e => e.Transaction)
                        .WithMany(t => t.Photos)
                        .HasForeignKey(e => e.TransactionId)
                        .OnDelete(DeleteBehavior.Cascade);
                    
                    entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
                    entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
                    entity.Property(e => e.FileType).HasMaxLength(50).IsRequired();
                    entity.Property(e => e.Description).HasMaxLength(500);
                    entity.Property(e => e.UploadedBy).HasMaxLength(100).IsRequired();
                    entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
                    entity.Property(e => e.MediumSizePath).HasMaxLength(500);
                });
            }            
        }
}
