using System;
using Microsoft.Extensions.Caching.Memory;

namespace transEstrellaInv.Models
{

    public interface IExchangeRateService
    {
        Task<decimal> GetUsdToMxnRateAsync();
        Task<decimal> ConvertUsdToMxnAsync(decimal usdAmount);
        Task<decimal> ConvertMxnToUsdAsync(decimal mxnAmount);
        Task<(decimal usd, decimal mxn)> ConvertCurrencyAsync(decimal amount, string fromCurrency);
    }

    public class BanxicoExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BanxicoExchangeRateService> _logger;

        public async Task<(decimal usd, decimal mxn)> ConvertCurrencyAsync(decimal amount, string fromCurrency)
        {
            var rate = await GetUsdToMxnRateAsync();     
            if (fromCurrency == "USD")
            {
                var usd = amount;
                var mxn = amount * rate;
                return (usd, mxn);
            }
            else if (fromCurrency == "MXN")
            {
                var mxn = amount;
                var usd = amount / rate;
                return (usd, mxn);
            }
            else
            {
                throw new ArgumentException("Currency must be USD or MXN");  
            }
        }
        
        public BanxicoExchangeRateService(
            HttpClient httpClient, 
            IMemoryCache cache,
            ILogger<BanxicoExchangeRateService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<decimal> GetUsdToMxnRateAsync()
        {
            // Cache for 6 hours
            return await _cache.GetOrCreateAsync("USD_MXN_RATE", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                
                try
                {
                    var token = "77d0d2f66c445d82c1450f814b9a1cbe32b4b2294067ddfcfdbd5d1907dad57b";
                    var url = $"https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno?token={token}";
                    
                    var response = await _httpClient.GetFromJsonAsync<BanxicoResponse>(url);
                    var rate = response?.bmx?.series?[0]?.datos?[0]?.dato;
                    
                    if (rate == null || rate <= 0)
                        throw new Exception("Invalid rate from Banxico");
                        
                    return rate.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch Banxico rate");
                    // Fallback to last known rate or default
                    return 20.0m; 
                }
            });
        }
        
        public async Task<decimal> ConvertUsdToMxnAsync(decimal usdAmount)
        {
            var rate = await GetUsdToMxnRateAsync();
            return usdAmount * rate;
        }
        
        public async Task<decimal> ConvertMxnToUsdAsync(decimal mxnAmount)
        {
            var rate = await GetUsdToMxnRateAsync();
            return mxnAmount / rate;
        }
    }

    // Response for Banxico API
    public class BanxicoResponse
    {
        public Bmx? bmx { get; set; }
    }

    public class Bmx
    {
        public Series[]? series { get; set; }
    }

    public class Series
    {
        public Datum[]?  datos { get; set; }
    }

    public class Datum
    {
        public decimal? dato { get; set; }
        public string? fecha { get; set; }
    }
}