using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using transEstrellaInv.Data;
using transEstrellaInv.Models;
using Microsoft.EntityFrameworkCore;

namespace transEstrellaInv.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPhotoUploadService _photoUploadService;
        private readonly ILogger<TransactionsController> _logger;
        
        public TransactionsController(
            AppDbContext context,
            IPhotoUploadService photoUploadService,
            ILogger<TransactionsController> logger)
        {
            _context = context;
            _photoUploadService = photoUploadService;
            _logger = logger;
        }
        
        // GET: Transactions/Index
        public async Task<IActionResult> Index()
        {
            var transactions = await _context.Transactions
                .Include(t => t.PartInventory)
                    .ThenInclude(pi => pi.PartDefinition)
                .Include(t => t.Photos)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            
            return View(transactions);
        }
        
        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.PartInventory)
                    .ThenInclude(pi => pi.PartDefinition)
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (transaction == null)
            {
                return NotFound();
            }
            
            return View(transaction);
        }
        
        // GET: Transactions/AddEvidence/5
        public async Task<IActionResult> AddEvidence(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.PartInventory)
                    .ThenInclude(pi => pi.PartDefinition)  // Now we can access part info
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (transaction == null)
            {
                return NotFound();
            }
            
            var viewModel = new TransactionWithEvidenceViewModel
            {
                Id = transaction.Id,
                TransactionDate = transaction.TransactionDate,
                Type = transaction.Type,
                // Navigate through PartInventory to PartDefinition
                PartType = transaction.PartInventory?.PartDefinition?.PartType ?? "Unknown",
                PartNumber = transaction.PartInventory?.PartDefinition?.PartNumber ?? "Unknown",
                BatchId = transaction.PartInventoryId, // Include the batch ID for reference
                Quantity = transaction.Quantity,
                EvidenceComments = transaction.EvidenceComments,
                Photos = transaction.Photos.Select(p => new TransactionPhotoViewModel
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    FilePath = p.FilePath,
                    ThumbnailPath = p.ThumbnailPath,
                    Description = p.Description,
                    UploadedAt = p.UploadedAt,
                    UploadedBy = p.UploadedBy,
                    FileSizeDisplay = FormatFileSize(p.FileSize)
                }).ToList()
            };
            
            return View(viewModel);
        }
        
        // POST: Transactions/AddEvidence/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvidence(int id, TransactionWithEvidenceViewModel model)
        {
            var response = new ApiResponse();
            
            try
            {
                if (id != model.Id)
                {
                    response.Success = false;
                    response.Message = "Invalid transaction ID";
                    return BadRequest(response);
                }
                
                var transaction = await _context.Transactions
                    .Include(t => t.Photos)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (transaction == null)
                {
                    response.Success = false;
                    response.Message = "Transaction not found";
                    return NotFound(response);
                }
                
                var currentUser = User.Identity?.Name ?? "Anonymous";
                
                // Update evidence comments
                if (!string.IsNullOrEmpty(model.NewEvidenceComments))
                {
                    transaction.EvidenceComments = string.IsNullOrEmpty(transaction.EvidenceComments)
                        ? model.NewEvidenceComments
                        : $"{transaction.EvidenceComments}\n\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {model.NewEvidenceComments}";
                    
                    transaction.EvidenceSubmittedBy = currentUser;
                }
                
                var uploadedFiles = new List<UploadedFileInfo>();
                
                // Upload photos
                if (model.NewPhotoFiles != null && model.NewPhotoFiles.Count > 0)
                {
                    var descriptions = model.PhotoDescriptions?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    
                    for (int i = 0; i < model.NewPhotoFiles.Count; i++)
                    {
                        var photoFile = model.NewPhotoFiles[i];
                        var description = descriptions?.Length > i ? descriptions[i].Trim() : null;
                        
                        try
                        {
                            var filePath = await _photoUploadService.UploadTransactionPhotoAsync(
                                photoFile, 
                                transaction.Id, 
                                description);
                            
                            var photo = new TransactionPhoto
                            {
                                TransactionId = transaction.Id,
                                FilePath = filePath,
                                FileName = photoFile.FileName,
                                FileType = photoFile.ContentType,
                                FileSize = photoFile.Length,
                                Description = description,
                                UploadedBy = currentUser,
                                UploadedAt = DateTime.UtcNow
                            };
                            
                            transaction.Photos.Add(photo);
                            
                            uploadedFiles.Add(new UploadedFileInfo
                            {
                                Name = photoFile.FileName,
                                Size = photoFile.Length,
                                Type = photoFile.ContentType,
                                Path = filePath
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading {FileName}", photoFile.FileName);
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                
                response.Success = true;
                response.Message = $"Evidence submitted successfully! {uploadedFiles.Count} file(s) uploaded.";
                response.Data = new
                {
                    TransactionId = transaction.Id,
                    FilesUploaded = uploadedFiles,
                    UploadPath = $"/uploads/transactions/{transaction.Id}/",
                    EvidenceSubmittedAt = transaction.TransactionDate
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting evidence for transaction {TransactionId}", id);
                
                response.Success = false;
                response.Message = "An error occurred while submitting evidence: " + ex.Message;
                return StatusCode(500, response);
            }
        }
        
        // GET: Transactions/ByBatch/5 - Show all transactions for a specific batch
        public async Task<IActionResult> ByBatch(int id)
        {
            var batch = await _context.PartInventories
                .Include(pi => pi.PartDefinition)
                .Include(pi => pi.Transactions)
                    .ThenInclude(t => t.Photos)
                .FirstOrDefaultAsync(pi => pi.Id == id);
            
            if (batch == null)
                return NotFound();
            
            ViewBag.Batch = batch;
            
            var transactions = batch.Transactions
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
            
            return View(transactions);
        }
        
        // GET: Transactions/ByPart/5 - Show all transactions for a part definition
        public async Task<IActionResult> ByPart(int id)
        {
            var part = await _context.PartDefinitions
                .Include(pd => pd.InventoryItems)
                    .ThenInclude(pi => pi.Transactions)
                        .ThenInclude(t => t.Photos)
                .FirstOrDefaultAsync(pd => pd.Id == id);
            
            if (part == null)
                return NotFound();
            
            var allTransactions = part.InventoryItems
                .SelectMany(pi => pi.Transactions)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
            
            ViewBag.Part = part;
            
            return View(allTransactions);
        }
        
        // GET: Transactions/DeletePhoto/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _context.TransactionPhotos
                .Include(p => p.Transaction)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (photo == null)
            {
                return NotFound();
            }
            
            var transactionId = photo.TransactionId;
            
            try
            {
                // Delete physical file
                await _photoUploadService.DeletePhotoAsync(photo.FilePath);
                
                // Delete database record
                _context.TransactionPhotos.Remove(photo);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Photo deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo");
                TempData["ErrorMessage"] = "Error deleting photo.";
            }
            
            return RedirectToAction(nameof(AddEvidence), new { id = transactionId });
        }
        
        // Helper classes for JSON response
        public class ApiResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public object Data { get; set; }
        }

        public class UploadedFileInfo
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public string Type { get; set; }
            public string Path { get; set; }
        }
        
        // Helper method
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
