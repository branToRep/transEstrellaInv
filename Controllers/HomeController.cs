using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using transEstrellaInv.Models;
using transEstrellaInv.Data;
using Microsoft.EntityFrameworkCore;

namespace transEstrellaInv.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}


[Route("test")]
public class TestController : Controller
{
    private readonly AppDbContext _context;
    private readonly IExchangeRateService _exchangeService;
    private readonly ILogger<TestController> _logger;
    
    public TestController(AppDbContext context, IExchangeRateService exchangeService, ILogger<TestController> logger)
    {
        _context = context;
        _exchangeService = exchangeService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> Test()
    {
        // Get all PartDefinitions with their inventory items
        var partDefinitions = await _context.PartDefinitions
            .Include(pd => pd.InventoryItems)
            .OrderBy(pd => pd.PartNumber)
            .ToListAsync();
        
        // Also get inventory items with their definitions for the form dropdown
        ViewBag.InventoryItems = await _context.PartInventories
            .Include(pi => pi.PartDefinition)
            .Where(pi => pi.IsActive)
            .OrderBy(pi => pi.PartDefinition.PartNumber)
            .ThenBy(pi => pi.Id)
            .Select(pi => new  // ← This creates anonymous type
            {
                pi.Id,
                PartDefinition = new {  // Create anonymous for PartDefinition too
                    pi.PartDefinition.PartNumber,
                    pi.PartDefinition.PartType
                },
                pi.OrderedQuantity,
                pi.ReceivedQuantity,
                pi.Price,
                pi.CurrencyCode,
                Status = pi.ReceivedQuantity == 0 ? "🚚 Ordered" : 
                        pi.ReceivedQuantity < pi.OrderedQuantity ? "📦 Partial" : "✅ In Stock"
            })
            .ToListAsync();
        ViewBag.PartDefinitions = await _context.PartDefinitions
            .OrderBy(p => p.PartNumber)
            .ToListAsync();

        try
        {
            var rate = await _exchangeService.GetUsdToMxnRateAsync();
            ViewBag.CurrentRate = rate;
            Console.WriteLine($"Current exchange rate loaded: {rate}");
        }
        catch (Exception ex)
        {
            ViewBag.CurrentRate = 17.20m; // Default fallback rate
            Console.WriteLine($"Failed to get exchange rate, using fallback: {ex.Message}");
        }
            
            return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Test(PartCreateViewModel model)
    {
        Console.WriteLine("=== POST TEST METHOD CALLED ===");
        Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");
        Console.WriteLine($"PartNumber: {model.PartNumber}");
        Console.WriteLine($"Price: {model.Price}");
        Console.WriteLine($"Currency: {model.Currency}");
        
        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState invalid - returning to form");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Error: {error.ErrorMessage}");
            }
            
            var partDefinitions = await _context.PartDefinitions
                .Include(pd => pd.InventoryItems)
                .OrderBy(pd => pd.PartNumber)
                .ToListAsync();
            ViewBag.PartDefinitions = partDefinitions;
            
            return View(model);
        }
        
        try
        {
            Console.WriteLine("Creating new part definition and inventory...");
            
            var existingDefinition = await _context.PartDefinitions
                .FirstOrDefaultAsync(pd => pd.PartNumber == model.PartNumber);
            
            if (existingDefinition == null)
            {
                existingDefinition = new PartDefinition
                {
                    PartNumber = model.PartNumber,
                    PartType = model.PartType
                };
                
                _context.PartDefinitions.Add(existingDefinition);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Created new PartDefinition with ID: {existingDefinition.Id}");
            }
            
            // Calculate prices in both currencies
            decimal priceMXN, priceUSD;
            if (model.Currency == "USD")
            {
                priceUSD = model.Price;
                priceMXN = model.Price * 17.0m; // Use your actual exchange rate service
            }
            else // MXN
            {
                priceMXN = model.Price;
                priceUSD = model.Price / 17.0m;
            }
            
            // Create the PartInventory (batch)
            var inventory = new PartInventory
            {
                PartDefinitionId = existingDefinition.Id,
                OrderedQuantity = model.Quantity,
                ReceivedQuantity = 0,
                Price = model.Price,
                PriceMXN = priceMXN,
                PriceUSD = priceUSD,
                CurrencyCode = model.Currency,
                ExchangeRateDate = DateTime.UtcNow,
                PurchaseDate = model.ReceivedDate ?? DateTime.UtcNow,
                Seller = model.Seller,
                PartOrderedBy = model.PartOrderedBy,
                PartBoughtBy = model.PartBoughtBy,
                RackPosition = null,
                ReceivedDate = model.IsExistingStock ? (model.ReceivedDate ?? DateTime.UtcNow) : (DateTime?)null, 
                IsActive = true
            };
            
            _context.PartInventories.Add(inventory);
            Console.WriteLine("PartInventory added to context");
            
            var saveResult = await _context.SaveChangesAsync();
            Console.WriteLine($"SaveChanges result: {saveResult} rows affected");
            
            if (saveResult > 0)
            {
                Console.WriteLine($"Inventory saved with ID: {inventory.Id}");
                
                // ONLY create transaction for existing stock
                if (model.IsExistingStock)
                {
                    var transaction = new Transaction
                    {
                        PartInventoryId = inventory.Id,
                        Type = TransactionType.Incoming,
                        Quantity = model.Quantity,
                        ReceivedDate = model.ReceivedDate ?? DateTime.UtcNow,
                        PartReceivedBy = "System",
                        TransactionDate = DateTime.UtcNow,
                        EvidenceComments = "Initial stock entry",
                        EvidenceSubmittedBy = "System"
                    };
                    
                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created initial transaction with ID: {transaction.Id}");
                    
                    TempData["SuccessMessage"] = $"✅ Part created with {model.Quantity} units in stock at {model.RackPosition}";
                }
                else
                {
                    TempData["SuccessMessage"] = $"✅ Purchase order created for {model.Quantity} units. Use Inbound tab when they arrive.";
                }
                
                return RedirectToAction("Test");
            }
            else
            {
                Console.WriteLine("No rows were saved");
                ModelState.AddModelError("", "Failed to save part to database");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            ModelState.AddModelError("", $"Error saving: {ex.Message}");
        }
        
        // Reload definitions for dropdown
        var definitions = await _context.PartDefinitions
            .Include(pd => pd.InventoryItems)
            .OrderBy(pd => pd.PartNumber)
            .ToListAsync();
        ViewBag.PartDefinitions = definitions;
        
        return View(model);
}
    
    
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTestTransactions()
    {
        var transactions = await _context.Transactions
            .Include(t => t.PartInventory)
                .ThenInclude(pi => pi.PartDefinition)
            .OrderByDescending(t => t.Id)
            .Take(10)
            .Select(t => new
            {
                t.Id,
                t.Type,
                PartName = t.PartInventory.PartDefinition.PartType,
                PartNumber = t.PartInventory.PartDefinition.PartNumber,
                BatchId = t.PartInventoryId,
                t.Quantity,
                t.TransactionDate
            })
            .ToListAsync();
            
        return Ok(new
        {
            success = true,
            count = transactions.Count,
            transactions
        });
    }
    
    [HttpPost("CreateTransaction")]
    public async Task<IActionResult> CreateTransaction(Transaction transaction, string? rackPosition, List<IFormFile> PhotoFiles)
    {
        Console.WriteLine("=== CREATE TRANSACTION POST CALLED ===");
        
        ModelState.Remove("PartInventory");
        
        // Validate based on transaction type
        if (transaction.Type == TransactionType.Incoming)
        {
            ModelState.Remove("TruckNumber");
            ModelState.Remove("RepairedBy");
            transaction.TruckNumber = null;
            transaction.RepairedBy = null;
        }
        else 
        {
            ModelState.Remove("ReceivedDate");
            ModelState.Remove("PartReceivedBy");
            
            transaction.ReceivedDate = DateTime.UtcNow;
            transaction.PartReceivedBy = "N/A - Outgoing";
            
            if (string.IsNullOrWhiteSpace(transaction.TruckNumber))
            {
                ModelState.AddModelError("TruckNumber", "Se necesita número de camión");
            }
        }
        
        if (!ModelState.IsValid)
        {
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any())
                {
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Error in {key}: {error.ErrorMessage}");
                    }
                }
            }
            
            ViewBag.InventoryItems = await _context.PartInventories
                .Include(pi => pi.PartDefinition)
                .OrderBy(pi => pi.PartDefinition.PartNumber)
                .Select(pi => new  // ← This creates anonymous type
                {
                    pi.Id,
                    PartDefinition = new {  // Create anonymous for PartDefinition too
                        pi.PartDefinition.PartNumber,
                        pi.PartDefinition.PartType
                    },
                    pi.OrderedQuantity,
                    pi.ReceivedQuantity,
                    pi.Price,
                    pi.CurrencyCode,
                    Status = pi.ReceivedQuantity == 0 ? "🚚 Ordered" : 
                            pi.ReceivedQuantity < pi.OrderedQuantity ? "📦 Partial" : "✅ In Stock"
                })
                .ToListAsync();
                
                
            return View("Test", new PartCreateViewModel());
        }
        
        try
        {
            var inventory = await _context.PartInventories
                .Include(pi => pi.PartDefinition)
                .FirstOrDefaultAsync(pi => pi.Id == transaction.PartInventoryId);
                
            if (inventory == null)
            {
                ModelState.AddModelError("", "Selected inventory batch not found");
                ViewBag.InventoryItems = await _context.PartInventories
                    .Include(pi => pi.PartDefinition)
                    .OrderBy(pi => pi.PartDefinition.PartNumber)
                    .Select(pi => new  // ← This creates anonymous type
                    {
                        pi.Id,
                        PartDefinition = new {  // Create anonymous for PartDefinition too
                            pi.PartDefinition.PartNumber,
                            pi.PartDefinition.PartType
                        },
                        pi.OrderedQuantity,
                        pi.ReceivedQuantity,
                        pi.Price,
                        pi.CurrencyCode,
                        Status = pi.ReceivedQuantity == 0 ? "🚚 Ordered" : 
                                pi.ReceivedQuantity < pi.OrderedQuantity ? "📦 Partial" : "✅ In Stock"
                    })                    
                    .ToListAsync();
                return View("Test", new PartCreateViewModel());
            }
            
            // Update inventory quantity
            if (transaction.Type == TransactionType.Incoming)
            {
                inventory.ReceivedQuantity += transaction.Quantity;
                if (string.IsNullOrWhiteSpace(rackPosition))
                {
                    ModelState.AddModelError("rackPosition", "Rack position is required for incoming transactions");
                }
                else
                {
                    inventory.RackPosition = rackPosition;
                }

                if (inventory.ReceivedQuantity > inventory.OrderedQuantity)
                {
                    ModelState.AddModelError("", $"Cannot receive more than ordered quantity ({inventory.OrderedQuantity})");
                }
            }
            else
            {
                if (inventory.ReceivedQuantity < transaction.Quantity)
                {
                    ModelState.AddModelError("", $"Insufficient stock. Available: {inventory.Quantity}");
                    ViewBag.InventoryItems = await _context.PartInventories
                        .Include(pi => pi.PartDefinition)
                        .OrderBy(pi => pi.PartDefinition.PartNumber)
                        .Select(pi => new  // ← This creates anonymous type
                        {
                            pi.Id,
                            PartDefinition = new {  // Create anonymous for PartDefinition too
                                pi.PartDefinition.PartNumber,
                                pi.PartDefinition.PartType
                            },
                            pi.OrderedQuantity,
                            pi.ReceivedQuantity,
                            pi.Price,
                            pi.CurrencyCode,
                            Status = pi.ReceivedQuantity == 0 ? "🚚 Ordered" : 
                                    pi.ReceivedQuantity < pi.OrderedQuantity ? "📦 Partial" : "✅ In Stock"
                        })       
                        .ToListAsync();
                    return View("Test", new PartCreateViewModel());
                }
                inventory.ReceivedQuantity -= transaction.Quantity;
            }
            
            // Set transaction dates
            transaction.TransactionDate = DateTime.UtcNow;
            
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            
            // Handle photos
            if (PhotoFiles != null && PhotoFiles.Any())
            {
                Console.WriteLine($"Processing {PhotoFiles.Count} photo(s)");
                
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/transactions");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                foreach (var file in PhotoFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        
                        var photo = new TransactionPhoto
                        {
                            TransactionId = transaction.Id,
                            FileName = file.FileName,
                            FilePath = $"/uploads/transactions/{uniqueFileName}",
                            FileSize = file.Length,
                            FileType = file.ContentType,
                            UploadedAt = DateTime.UtcNow,
                            UploadedBy = User.Identity?.Name ?? "System"
                        };
                        
                        _context.TransactionPhotos.Add(photo);
                    }
                }
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"Saved {PhotoFiles.Count} photo(s) to database");
            }
            
            TempData["SuccessMessage"] = $"Transaction created! New stock: {inventory.Quantity} for batch {inventory.Id}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex.Message}");
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }
        
        return RedirectToAction("Test");
    }
    
    [HttpGet("check-transactions")]
    public async Task<IActionResult> CheckTransactions()
    {
        var transactions = await _context.Transactions
            .Include(t => t.PartInventory)
                .ThenInclude(pi => pi.PartDefinition)
            .OrderByDescending(t => t.Id)
            .Take(10)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.Quantity,
                t.TransactionDate,
                InventoryId = t.PartInventoryId,
                PartNumber = t.PartInventory.PartDefinition.PartNumber,
                PartType = t.PartInventory.PartDefinition.PartType,
                Ordered = t.PartInventory.OrderedQuantity,
                Received = t.PartInventory.ReceivedQuantity,
                Pending = t.PartInventory.OrderedQuantity - t.PartInventory.ReceivedQuantity
                
            })
            .ToListAsync();
        
        return Ok(transactions);
    }
    
    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory()
    {
        var inventory = await _context.PartInventories
            .Include(pi => pi.PartDefinition)
            .OrderBy(pi => pi.PartDefinition.PartNumber)
            .ThenBy(pi => pi.Id)
            .ToListAsync();
        
        return View(inventory);
    }
    
    [HttpGet("inventory/{id}")]
    public async Task<IActionResult> InventoryDetails(int id)
    {
        var inventory = await _context.PartInventories
            .Include(pi => pi.PartDefinition)
            .Include(pi => pi.Transactions)
                .ThenInclude(t => t.Photos)
            .FirstOrDefaultAsync(pi => pi.Id == id);
        
        if (inventory == null)
            return NotFound();
        
        return View(inventory);
    }
    
    [HttpGet("transaction-photos/{transactionId}")]
    public async Task<IActionResult> GetTransactionPhotos(int transactionId)
    {
        var photos = await _context.TransactionPhotos
            .Where(p => p.TransactionId == transactionId)
            .Select(p => new
            {
                p.Id,
                p.FileName,
                p.FilePath,
                p.FileSize,
                p.UploadedAt
            })
            .ToListAsync();
        
        return Ok(photos);
    }

    [HttpGet("debug/parts")]
    public async Task<IActionResult> DebugParts()
    {
        var definitions = await _context.PartDefinitions
            .Select(d => new { d.Id, d.PartNumber, d.PartType })
            .ToListAsync();
        
        var inventories = await _context.PartInventories
            .Include(i => i.PartDefinition)
            .Select(i => new { 
                i.Id, 
                i.PartDefinitionId,
                PartNumber = i.PartDefinition.PartNumber,
                i.Quantity,
                i.Price
            })
            .ToListAsync();
        
        return Ok(new
        {
            PartDefinitions = definitions,
            PartInventories = inventories,
            DefinitionCount = definitions.Count,
            InventoryCount = inventories.Count
        });
    }

    [HttpGet("create-batch")]
    public async Task<IActionResult> CreateBatch()
    {
        ViewBag.PartDefinitions = await _context.PartDefinitions
            .OrderBy(p => p.PartNumber)
            .ToListAsync();
        
        return View();
    }

    [HttpGet("inventory-view")]
    public async Task<IActionResult> InventoryView()
    {
        var inventory = await _context.PartInventories
            .Include(pi => pi.PartDefinition)
            .Where(pi => pi.IsActive)
            .OrderBy(pi => pi.PartDefinition.PartNumber)
            .ThenBy(pi => pi.Id)
            .Select(pi => new InventoryViewModel
            {
                BatchId = pi.Id,
                PartNumber = pi.PartDefinition.PartNumber,
                PartType = pi.PartDefinition.PartType ?? "N/A",
                OrderedQuantity = pi.OrderedQuantity,
                ReceivedQuantity = pi.ReceivedQuantity,
                Price = pi.Price,
                Currency = pi.CurrencyCode,
                RackPosition = pi.RackPosition ?? "Sin asignar",
                Seller = pi.Seller ?? "N/A",
                PurchaseDate = pi.PurchaseDate,
                ReceivedDate = pi.ReceivedDate
            })
            .ToListAsync();
        
        return View(inventory);
    }

    [HttpGet("transactions-view")]
    public async Task<IActionResult> TransactionsView()
    {
        // Get all purchases (batches)
        var purchases = await _context.PartInventories
            .Include(pi => pi.PartDefinition)
            .Where(pi => pi.IsActive)
            .OrderByDescending(pi => pi.PurchaseDate)
            .Select(pi => new PurchaseViewModel
            {
                BatchId = pi.Id,
                PartNumber = pi.PartDefinition.PartNumber,
                PartType = pi.PartDefinition.PartType ?? "N/A",
                OrderedQuantity = pi.OrderedQuantity,
                ReceivedQuantity = pi.ReceivedQuantity,
                Price = pi.Price,
                Currency = pi.CurrencyCode,
                Seller = pi.Seller ?? "N/A",
                OrderedBy = pi.PartOrderedBy ?? "N/A",
                BoughtBy = pi.PartBoughtBy ?? "N/A",
                PurchaseDate = pi.PurchaseDate
            })
            .ToListAsync();
        
        // Get all transactions with photos
        var transactions = await _context.Transactions
            .Include(t => t.PartInventory)
                .ThenInclude(pi => pi.PartDefinition)
            .Include(t => t.Photos)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionViewModel
            {
                TransactionId = t.Id,
                BatchId = t.PartInventoryId,
                PartNumber = t.PartInventory.PartDefinition.PartNumber,
                PartType = t.PartInventory.PartDefinition.PartType ?? "N/A",
                Type = t.Type,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                OrderedBy = t.PartInventory.PartOrderedBy,
                BoughtBy = t.PartInventory.PartBoughtBy,
                ReceivedBy = t.PartReceivedBy,
                TruckNumber = t.TruckNumber,
                RepairedBy = t.RepairedBy,
                Seller = t.PartInventory.Seller,
                EvidenceComments = t.EvidenceComments,
                Photos = t.Photos.Select(p => new TransactionPhotoViewModel
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    FilePath = p.FilePath,
                    ThumbnailPath = p.ThumbnailPath,
                    FileSize = p.FileSize,
                    UploadedAt = p.UploadedAt,
                    UploadedBy = p.UploadedBy
                }).ToList()
            })
            .ToListAsync();
        
        // Separate by type
        var incoming = transactions.Where(t => t.Type == TransactionType.Incoming).ToList();
        var outgoing = transactions.Where(t => t.Type == TransactionType.Outgoing).ToList();
        
        ViewBag.Purchases = purchases;
        ViewBag.Incoming = incoming;
        ViewBag.Outgoing = outgoing;
        
        return View();
    }

    //MUST CREATE ADMIN PAGE TO MANUALLY DEACTIVATE OBSOLETE PARTS
    [HttpPost ("deactivate-batch")]
    public async Task<IActionResult> DeactivateBatch(int id, string reason)
    {
        var batch = await _context.PartInventories
            .FirstOrDefaultAsync(b => b.Id == id);
        
        if (batch == null)
            return NotFound();

        if (batch.Quantity > 0)
        {
            TempData["Error"] = "Cannot deactivate batch with remaining stock";
            return RedirectToAction("BatchDetails", new { id });
        }
        
        batch.IsActive = false;
        batch.DeactivatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Batch deactivated successfully";
        return RedirectToAction("Index");
    }


    
}


