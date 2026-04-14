using Microsoft.AspNetCore.Mvc;
using transEstrellaInv.Models; 

namespace transEstrellaInv.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ILogger<ExchangeController> _logger;

        public ExchangeController(
            IExchangeRateService exchangeRateService,
            ILogger<ExchangeController> logger)
        {
            _exchangeRateService = exchangeRateService;
            _logger = logger;
        }

        [HttpGet("convert")]
        public async Task<IActionResult> Convert(
            [FromQuery] decimal amount, 
            [FromQuery] string from = "USD")
        {
            try
            {
                _logger.LogInformation($"Converting {amount} {from}");
                
                var (usd, mxn) = await _exchangeRateService.ConvertCurrencyAsync(amount, from);
                
                // Get the current rate for information
                var rate = await _exchangeRateService.GetUsdToMxnRateAsync();
                
                return Ok(new
                {
                    success = true,
                    amount = amount,
                    from = from,
                    results = new
                    {
                        usd = Math.Round(usd, 2),
                        mxn = Math.Round(mxn, 2)
                    },
                    rate = Math.Round(rate, 4),
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    message = from == "USD" 
                        ? $"{amount} USD = {mxn:F2} MXN"
                        : $"{amount} MXN = {usd:F2} USD"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting currency");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error converting currency",
                    error = ex.Message
                });
            }
        }

        [HttpGet("rate")]
        public async Task<IActionResult> GetRate()
        {
            try
            {
                var rate = await _exchangeRateService.GetUsdToMxnRateAsync();
                
                return Ok(new
                {
                    success = true,
                    rate = Math.Round(rate, 4),
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    message = $"1 USD = {rate:F2} MXN"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting exchange rate"
                });
            }
        }

        [HttpGet("convert-both")]
        public async Task<IActionResult> ConvertBoth([FromQuery] decimal usdAmount, [FromQuery] decimal mxnAmount)
        {
            try
            {
                var rate = await _exchangeRateService.GetUsdToMxnRateAsync();
                
                var results = new
                {
                    usdToMxn = usdAmount > 0 
                        ? await _exchangeRateService.ConvertUsdToMxnAsync(usdAmount) 
                        : 0,
                    mxnToUsd = mxnAmount > 0 
                        ? await _exchangeRateService.ConvertMxnToUsdAsync(mxnAmount) 
                        : 0,
                    rate = Math.Round(rate, 4)
                };
                
                return Ok(new
                {
                    success = true,
                    results,
                    message = $"1 USD = {rate:F2} MXN"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error converting currency"
                });
            }
        }
        
    }
}