using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using transEstrellaInv.Data;
using transEstrellaInv.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public class IncomingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<IncomingController> _logger;
    
    public IncomingController(AppDbContext context, ILogger<IncomingController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // GET: Incoming/Create
    public async Task<IActionResult> Create()
    {
        // Load existing part definitions for dropdown (Option B)
        ViewBag.ExistingParts = await _context.PartDefinitions
            .OrderBy(p => p.PartNumber)
            .ToListAsync();
        
        return View();
    }
    
    // POST: Incoming/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IncomingTransactionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ExistingParts = await _context.PartDefinitions
                .OrderBy(p => p.PartNumber)
                .ToListAsync();
            return View(model);
        }
        
        try
        {
            // STEP 1: Handle PartDefinition (the "template")
            PartDefinition partDefinition = null;
            
            if (model.UseExistingPart && model.SelectedPartDefinitionId.HasValue)
            {
                // OPTION B: Using an existing part definition
                partDefinition = await _context.PartDefinitions
                    .FirstOrDefaultAsync(p => p.Id == model.SelectedPartDefinitionId.Value);
                
                if (partDefinition == null)
                {
                    ModelState.AddModelError("", "Selected part not found");
                    ViewBag.ExistingParts = await _context.PartDefinitions
                        .OrderBy(p => p.PartNumber)
                        .ToListAsync();
                    return View(model);
                }
                
                _logger.LogInformation($"Using existing PartDefinition ID {partDefinition.Id}: {partDefinition.PartNumber}");
            }
            else
            {
                // OPTION A: Creating a new part definition
                // Check if part number already exists to avoid duplicates
                var existing = await _context.PartDefinitions
                    .FirstOrDefaultAsync(p => p.PartNumber == model.PartNumber);
                
                if (existing != null)
                {
                    ModelState.AddModelError("PartNumber", 
                        $"Part number '{model.PartNumber}' already exists. Please use the existing part option.");
                    
                    ViewBag.ExistingParts = await _context.PartDefinitions
                        .OrderBy(p => p.PartNumber)
                        .ToListAsync();
                    return View(model);
                }
                
                partDefinition = new PartDefinition
                {
                    PartNumber = model.PartNumber,
                    PartType = model.PartType
                };
                
                _context.PartDefinitions.Add(partDefinition);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created new PartDefinition ID {partDefinition.Id}: {partDefinition.PartNumber}");
            }
            
            // STEP 2: Calculate prices in both currencies
            decimal priceMXN, priceUSD;
            if (model.Currency == "USD")
            {
                priceUSD = model.Price;
                priceMXN = model.Price * 17.0m; // Use your exchange rate service here
            }
            else // MXN
            {
                priceMXN = model.Price;
                priceUSD = model.Price / 17.0m;
            }
            
            var partInventory = new PartInventory
            {
                PartDefinitionId = partDefinition.Id,
                Quantity = model.Quantity,
                Price = model.Price,
                PriceMXN = priceMXN,
                PriceUSD = priceUSD,
                CurrencyCode = model.Currency ?? "MXN",
                ExchangeRateDate = DateTime.UtcNow,
                PurchaseDate = model.ReceivedDate ?? DateTime.UtcNow,
                RackPosition = model.RackPosition
            };
            
            _context.PartInventories.Add(partInventory);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created PartInventory ID {partInventory.Id} for PartDefinition {partDefinition.Id}");
            
            var transaction = new Transaction
            {
                PartInventoryId = partInventory.Id,  
                Type = TransactionType.Incoming,
                Quantity = model.Quantity,
                ReceivedDate = model.ReceivedDate ?? DateTime.UtcNow,
                PartReceivedBy = model.PartReceivedBy,
                TruckNumber = model.TruckNumber,
                TransactionDate = DateTime.UtcNow,
                EvidenceComments = $"Incoming transaction for batch {partInventory.Id}",
                EvidenceSubmittedBy = "System"
            };
            
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created Transaction ID {transaction.Id} for Inventory ID {partInventory.Id}");
            
            TempData["SuccessMessage"] = $"Transaction created successfully! Batch ID: {partInventory.Id}";
            return RedirectToAction("Details", "Transactions", new { id = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating incoming transaction");
            ModelState.AddModelError("", $"Error creating transaction: {ex.Message}");
            
            ViewBag.ExistingParts = await _context.PartDefinitions
                .OrderBy(p => p.PartNumber)
                .ToListAsync();
            return View(model);
        }
    }
    
    // GET: Incoming/CheckPartNumber
    [HttpGet]
    public async Task<IActionResult> CheckPartNumber(string partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
            return Json(new { exists = false });
        
        var exists = await _context.PartDefinitions
            .AnyAsync(p => p.PartNumber == partNumber);
        
        return Json(new { exists });
    }
    
    // GET: Incoming/GetPartDetails/{id}
    [HttpGet]
    public async Task<IActionResult> GetPartDetails(int id)
    {
        var part = await _context.PartDefinitions
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (part == null)
            return NotFound();
        
        return Json(new
        {
            id = part.Id,
            partNumber = part.PartNumber,
            partType = part.PartType
        });
    }
}

public class IncomingTransactionViewModel
{
    // Option A: Create new part (requires PartNumber and PartType)
    [Required]
    public string? PartNumber { get; set; }
    
    [Required]
    public string? PartType { get; set; }
    
    // Option B: Use existing part
    [Display]
    public bool UseExistingPart { get; set; }
    
    [Display]
    public int? SelectedPartDefinitionId { get; set; }
    
    // Common fields for both options
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    
    [Required]
    public string Currency { get; set; } = "MXN";
    
    
    [Required]
    public string PartReceivedBy { get; set; }
    
    [Required]
    public string RackPosition { get; set; }
    
    public DateTime? ReceivedDate { get; set; }
    
    public string? TruckNumber { get; set; }
}
