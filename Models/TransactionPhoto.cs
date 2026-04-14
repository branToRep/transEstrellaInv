using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transEstrellaInv.Models
{
    public class TransactionPhoto
    {
        public int Id { get; set; }
        
        public int TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }
        
        [MaxLength(500)]
        public string FileName { get; set; }
        
        [MaxLength(50)]
        public string FileType { get; set; }
        
        public long FileSize { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public string? UploadedBy { get; set; }
        
        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }
        
        [MaxLength(500)]
        public string? MediumSizePath { get; set; }
        
        //Helper property for upload
        [NotMapped]
        public IFormFile? PhotoFile { get; set; }
    }

    public class TransactionWithEvidenceViewModel
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType Type { get; set; }
        
        // Part information from PartInventory -> PartDefinition
        public string PartType { get; set; }
        public string PartNumber { get; set; }
        public int BatchId { get; set; } // The PartInventory ID
        
        public int Quantity { get; set; }
        
        // Evidence fields
        public string? EvidenceComments { get; set; }
        public List<TransactionPhotoViewModel> Photos { get; set; } = new List<TransactionPhotoViewModel>();
        
        // For new evidence submission
        [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters")]
        public string? NewEvidenceComments { get; set; }
        
        [DataType(DataType.Upload)]
        [Display(Name = "Add Photos")]
        public List<IFormFile> NewPhotoFiles { get; set; } = new List<IFormFile>();
        
        [Display(Name = "Photo Descriptions (comma-separated)")]
        public string? PhotoDescriptions { get; set; }
    }

    public class TransactionPhotoViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
        public string FileSizeDisplay { get; set; }
    }

    public interface IPhotoUploadService
    {
        Task<string> UploadTransactionPhotoAsync(IFormFile file, int transactionId, string description = null);
        Task DeletePhotoAsync(string filePath);
        string GetPhotoUrl(string filePath);
    }

    public class PhotoUploadService : IPhotoUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhotoUploadService> _logger;
        
        public PhotoUploadService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<PhotoUploadService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }
        
        public async Task<string> UploadTransactionPhotoAsync(IFormFile file, int transactionId, string description = null)
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");
                
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
                
                // Validate file size (max 10MB)
                var maxFileSize = 10 * 1024 * 1024; 
                if (file.Length > maxFileSize)
                    throw new InvalidOperationException("File size exceeds 10MB limit");
                
                // Create directory structure
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "transactions", transactionId.ToString());
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // Return relative path
                var relativePath = filePath.Replace(_environment.WebRootPath, "").Replace("\\", "/");
                return relativePath; 
            }
            
            public async Task DeletePhotoAsync(string filePath)
            {
                if (string.IsNullOrEmpty(filePath))
                    return;
                
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                }
            }
            
            public string GetPhotoUrl(string filePath)
            {
                if (string.IsNullOrEmpty(filePath))
                    return null;
                
                return filePath.TrimStart('/');
            }
               
        public string GetThumbnailUrl(string thumbnailPath)
        {
            if (string.IsNullOrEmpty(thumbnailPath))
                return GetPhotoUrl("/images/default-thumbnail.jpg");
            
            return thumbnailPath.TrimStart('/');
        }
    }
}