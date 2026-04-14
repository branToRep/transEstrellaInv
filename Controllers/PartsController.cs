using Microsoft.AspNetCore.Mvc;
using transEstrellaInv.Data;
using transEstrellaInv.Models;
using Microsoft.EntityFrameworkCore;


namespace transEstrellaInv.Controllers
{
    public class PartsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IExchangeRateService _exchangeService;
        private readonly ILogger<PartsController> _logger;
        
        public PartsController(
            AppDbContext context,
            IExchangeRateService exchangeService,
            ILogger<PartsController> logger)
        {
            _context = context;
            _exchangeService = exchangeService;
            _logger = logger;
        }
        
        // GET: Parts/Index - Show all part definitions with their inventory batches
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var partDefinitions = await _context.PartDefinitions
                .Include(pd => pd.InventoryItems)
                .OrderBy(pd => pd.PartNumber)
                .ToListAsync();
            
            return View(partDefinitions);
        }
        
        // GET: Parts/CreateDefinition - Create a new part definition (template)
        [HttpGet]
        public IActionResult CreateDefinition()
        {
            return View(new PartDefinitionViewModel());
        }
        
        // POST: Parts/CreateDefinition
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDefinition(PartDefinitionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            // Check if part number already exists
            var existing = await _context.PartDefinitions
                .FirstOrDefaultAsync(p => p.PartNumber == model.PartNumber);
            
            if (existing != null)
            {
                ModelState.AddModelError("PartNumber", "Part number already exists");
                return View(model);
            }
            
            var partDefinition = new PartDefinition
            {
                PartNumber = model.PartNumber,
                PartType = model.PartType
            };
            
            _context.PartDefinitions.Add(partDefinition);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Part definition {model.PartNumber} created successfully!";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> CreateBatch(int? partDefinitionId = null)
        {
            var model = new PartBatchViewModel();
            
            if (partDefinitionId.HasValue)
            {
                var definition = await _context.PartDefinitions
                    .FirstOrDefaultAsync(p => p.Id == partDefinitionId.Value);
                
                if (definition != null)
                {
                    model.PartDefinitionId = definition.Id;
                    model.PartNumber = definition.PartNumber;
                }
            }
            
            // Load existing part definitions for dropdown
            ViewBag.PartDefinitions = await _context.PartDefinitions
                .OrderBy(p => p.PartNumber)
                .ToListAsync();
            
            Console.WriteLine($"CreateBatch GET: Found {((List<PartDefinition>)ViewBag.PartDefinitions).Count} definitions");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(PartBatchViewModel model)
        {
            Console.WriteLine("========== CREATE BATCH POST START ==========");
            Console.WriteLine($"Timestamp: {DateTime.Now}");
            Console.WriteLine($"PartDefinitionId: {model.PartDefinitionId}");
            Console.WriteLine($"OrderedQuantity: {model.OrderedQuantity}");
            Console.WriteLine($"Price: {model.Price}");
            Console.WriteLine($"Currency: {model.Currency}");
            Console.WriteLine($"Seller: {model.Seller}");
            Console.WriteLine($"PurchaseDate: {model.PurchaseDate}");
            Console.WriteLine($"PartOrderedBy: {model.PartOrderedBy}");
            Console.WriteLine($"PartBoughtBy: {model.PartBoughtBy}");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            var now = DateTime.UtcNow;
            
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                
                Console.WriteLine($"Validation errors: {errors}");
                TempData["Error"] = $"Validation failed: {errors}";
                return RedirectToAction("Test", "Test");
            }
            
            try
            {
                // Get the part definition
                Console.WriteLine($"Looking for PartDefinition with ID: {model.PartDefinitionId}");
                var definition = await _context.PartDefinitions
                    .FirstOrDefaultAsync(p => p.Id == model.PartDefinitionId);
                
                if (definition == null)
                {
                    Console.WriteLine("ERROR: PartDefinition not found!");
                    TempData["Error"] = "Selected part not found";
                    return RedirectToAction("Test", "Test");
                }
                
                Console.WriteLine($"Found PartDefinition: {definition.PartNumber} - {definition.PartType}");
                

                var purchaseDate = model.PurchaseDate.HasValue
                ? DateTime.SpecifyKind(model.PurchaseDate.Value, DateTimeKind.Utc)
                : now;
                
                
                // Calculate prices
                Console.WriteLine($"Calculating prices for {model.Price} {model.Currency}");
                var (priceUSD, priceMXN) = await CalculatePrices(model.Price, model.Currency);
                Console.WriteLine($"PriceUSD: {priceUSD}, PriceMXN: {priceMXN}");
                
                // Check for existing batch with same price (merge logic)
                Console.WriteLine("Checking for existing batch with same price...");
                var existingBatch = await _context.PartInventories
                    .FirstOrDefaultAsync(b => 
                        b.PartDefinitionId == definition.Id && 
                        b.Price == model.Price && 
                        b.CurrencyCode == model.Currency &&
                        b.IsActive);
                
                if (existingBatch != null)
                {
                    Console.WriteLine($"Found existing batch ID: {existingBatch.Id}");
                    Console.WriteLine($"Current OrderedQuantity: {existingBatch.OrderedQuantity}");
                    Console.WriteLine($"Adding {model.OrderedQuantity} units");
                    
                    // MERGE: Add to existing batch
                    existingBatch.OrderedQuantity += model.OrderedQuantity;
                    existingBatch.Seller = existingBatch.Seller + ", " + model.Seller;
                    
                    _context.PartInventories.Update(existingBatch);
                    
                    Console.WriteLine($"New OrderedQuantity: {existingBatch.OrderedQuantity}");
                    
                    TempData["SuccessMessage"] = $"✅ Added {model.OrderedQuantity} units to existing batch " +
                        $"(Total ordered: {existingBatch.OrderedQuantity})";
                }
                else
                {
                    Console.WriteLine("No existing batch found, creating new one");
                    
                    // CREATE NEW BATCH
                    var newBatch = new PartInventory
                    {
                        PartDefinitionId = definition.Id,
                        OrderedQuantity = model.OrderedQuantity,
                        ReceivedQuantity = 0,
                        Price = model.Price,
                        PriceUSD = priceUSD,
                        PriceMXN = priceMXN,
                        CurrencyCode = model.Currency,
                        Seller = model.Seller,
                        ExchangeRateDate = now,
                        PurchaseDate = purchaseDate,
                        PartOrderedBy = model.PartOrderedBy,
                        PartBoughtBy = model.PartBoughtBy,
                        ReceivedDate = null,
                        DeactivatedAt = null,
                        IsActive = true
                    };
                    
                    _context.PartInventories.Add(newBatch);
                    Console.WriteLine("New batch created in memory");
                    
                    TempData["SuccessMessage"] = $"✅ New batch created for {definition.PartNumber} " +
                        $"with {model.OrderedQuantity} units ordered at {model.Price} {model.Currency}";
                }
                
                Console.WriteLine("Calling SaveChangesAsync...");
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges result: {saveResult} rows affected");
                
                Console.WriteLine("========== CREATE BATCH POST END ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch");
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = $"❌ Error creating batch: {ex.Message}";
            }
            
            return RedirectToAction("Test", "Test");
        }

        private async Task<(decimal PriceUSD, decimal PriceMXN)> CalculatePrices(decimal price, string currency)
        {
            try
            {
                if (currency == "USD")
                {
                    var mxnPrice = await _exchangeService.ConvertUsdToMxnAsync(price);
                    return (price, mxnPrice);
                }
                else // MXN
                {
                    var usdPrice = await _exchangeService.ConvertMxnToUsdAsync(price);
                    return (usdPrice, price);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Currency conversion failed, using fallback rate");
                
                // Fallback to approximate rate if service fails
                const decimal fallbackRate = 17.0m;
                if (currency == "USD")
                {
                    return (price, price * fallbackRate);
                }
                else
                {
                    return (price / fallbackRate, price);
                }
            }
        }
        
        // GET: Parts/Details/5 - Show part definition with all its batches
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var definition = await _context.PartDefinitions
                .Include(pd => pd.InventoryItems)
                .FirstOrDefaultAsync(pd => pd.Id == id);
            
            if (definition == null)
                return NotFound();
            
            return View(definition);
        }
        
        // GET: Parts/BatchDetails/5 - Show details of a specific inventory batch
        [HttpGet]
        public async Task<IActionResult> BatchDetails(int id)
        {
            var batch = await _context.PartInventories
                .Include(pi => pi.PartDefinition)
                .Include(pi => pi.Transactions)
                    .ThenInclude(t => t.Photos)
                .FirstOrDefaultAsync(pi => pi.Id == id);
            
            if (batch == null)
                return NotFound();
            
            return View(batch);
        }
        
        // GET: Parts/EditBatch/5 - Edit inventory batch (price, rack position, etc.)
        [HttpGet]
        public async Task<IActionResult> EditBatch(int id)
        {
            var batch = await _context.PartInventories
                .Include(pi => pi.PartDefinition)
                .FirstOrDefaultAsync(pi => pi.Id == id);
            
            if (batch == null)
                return NotFound();
            
            var model = new EditBatchViewModel
            {
                Id = batch.Id,
                PartDefinitionId = batch.PartDefinitionId,
                PartNumber = batch.PartDefinition.PartNumber,
                Quantity = batch.Quantity,
                Price = batch.Price,
                Currency = batch.CurrencyCode,
                RackPosition = batch.RackPosition
            };
            
            return View(model);
        }
        
        // POST: Parts/EditBatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBatch(EditBatchViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            var batch = await _context.PartInventories
                .FirstOrDefaultAsync(pi => pi.Id == model.Id);
            
            if (batch == null)
                return NotFound();
            
            // Update price if changed
            if (batch.Price != model.Price || batch.CurrencyCode != model.Currency)
            {
                try
                {
                    if (model.Currency == "USD")
                    {
                        batch.PriceUSD = model.Price;
                        batch.PriceMXN = await _exchangeService.ConvertUsdToMxnAsync(model.Price);
                    }
                    else
                    {
                        batch.PriceMXN = model.Price;
                        batch.PriceUSD = await _exchangeService.ConvertMxnToUsdAsync(model.Price);
                    }
                    batch.ExchangeRateDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Currency conversion failed during edit");
                    // Keep old values but warn user
                    TempData["Warning"] = "Price saved but exchange rate unavailable";
                }
                
                batch.Price = model.Price;
                batch.CurrencyCode = model.Currency;
            }
            
            // Update other fields
            batch.Quantity = model.Quantity;
            batch.RackPosition = model.RackPosition;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Batch updated successfully";
            return RedirectToAction(nameof(BatchDetails), new { id = batch.Id });
        }
        
        // GET: Parts/CheckPartNumber
        [HttpGet]
        public async Task<IActionResult> CheckPartNumber(string partNumber)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                return Json(new { exists = false });
            
            var exists = await _context.PartDefinitions
                .AnyAsync(p => p.PartNumber == partNumber);
            
            return Json(new { exists });
        }
        
        [HttpGet("api/parts/definitions")]
        public async Task<IActionResult> GetDefinitions()
        {
            var definitions = await _context.PartDefinitions
                .Select(d => new { d.Id, d.PartNumber, d.PartType })
                .OrderBy(d => d.PartNumber)
                .ToListAsync();
            
            return Ok(definitions);
        }
        
        // Keep the exchange rate test endpoint
        [HttpGet("test/exchange-rate")]
        public async Task<IActionResult> TestExchangeRate()
        {
            try
            {
                var usdToMxn = await _exchangeService.GetUsdToMxnRateAsync();
                var conversion = await _exchangeService.ConvertCurrencyAsync(100, "USD");
                
                return Ok(new
                {
                    success = true,
                    rate = usdToMxn,
                    converted = conversion,
                    timestamp = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveBatch(int batchId, int quantity, string rackPosition)
        {
            var batch = await _context.PartInventories.FindAsync(batchId);
            
            // Update received quantity
            batch.ReceivedQuantity += quantity;
            batch.RackPosition = rackPosition;
            batch.ReceivedDate = DateTime.UtcNow;
            
            // Create transaction record
            var transaction = new Transaction
            {
                PartInventoryId = batch.Id,
                Type = TransactionType.Incoming,
                Quantity = quantity,
                ReceivedDate = DateTime.UtcNow,
                PartReceivedBy = User.Identity.Name
            };
            
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            
            return Ok();
        }
            }
}
