using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Services
{
    /// <summary>
    /// Service responsible for fetching and managing cryptocurrency price data from external APIs.
    /// </summary>
    public class CryptoService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.coingecko.com/api/v3/simple/price?ids={0}&vs_currencies=usd";
        private const string HISTORY_API_URL = "https://api.coingecko.com/api/v3/coins/{0}/market_chart?vs_currency=usd&days={1}";
        private readonly Dictionary<string, string> _cryptoIdMap = new()
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "SOL", "solana" }
        };

        // Cache configuration for price history
        private Dictionary<string, CryptoPriceHistory> _priceHistoryCache = new();
        private Dictionary<string, DateTime> _priceHistoryLastFetched = new();
        private const int CACHE_EXPIRY_MINUTES = 30;

        public CryptoService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Fetches live prices for cryptocurrencies in the watchlist.
        /// </summary>
        /// <returns>A tuple containing success status and dictionary of crypto symbols to their prices.</returns>
        public async Task<(bool success, Dictionary<string, decimal> prices)> FetchLivePrices()
        {
            try
            {
                using HttpClient client = new();
                string url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,solana&vs_currencies=usd";
                var response = await client.GetStringAsync(url);

                Console.WriteLine($"[DEBUG] API Response: {response}");

                var prices = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(response);
                if (prices == null || prices.Count == 0)
                {
                    throw new Exception("Empty price response from API.");
                }

                var parsedPrices = prices.ToDictionary(
                    p => p.Key.ToLower(),
                    p => p.Value["usd"]
                );

                Console.WriteLine("[DEBUG] Parsed live prices successfully.");
                foreach (var price in parsedPrices)
                {
                    Console.WriteLine($"[DEBUG] {price.Key}: ${price.Value}");
                }

                return (true, parsedPrices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch live prices: {ex.Message}");
                return (false, new Dictionary<string, decimal>()); 
            }
        }

        /// <summary>
        /// Retrieves the current price for a specific cryptocurrency symbol.
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol (e.g., "BTC", "ETH").</param>
        /// <returns>The current price in USD, or 0 if the price cannot be fetched.</returns>
        public async Task<decimal> GetCurrentPrice(string symbol)
        {
            int maxRetries = 2;
            int currentRetry = 0;
            
            while (currentRetry <= maxRetries)
            {
                try
                {
                    if (!_cryptoIdMap.ContainsKey(symbol))
                    {
                        Console.WriteLine($"[ERROR] Unknown symbol: {symbol}");
                        return 0;
                    }

                    string cryptoId = _cryptoIdMap[symbol];
                    string url = $"https://api.coingecko.com/api/v3/simple/price?ids={cryptoId}&vs_currencies=usd";
                    
                    Console.WriteLine($"[DEBUG] Fetching price for {symbol} (API ID: {cryptoId})");
                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        currentRetry++;
                        if (currentRetry <= maxRetries)
                        {
                            int delayMs = 1000 * (int)Math.Pow(2, currentRetry);
                            Console.WriteLine($"[WARN] Rate limited for {symbol}, retrying in {delayMs}ms... (Attempt {currentRetry}/{maxRetries})");
                            await Task.Delay(delayMs);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] Failed to fetch price for {symbol}: Rate limited and max retries exceeded");
                            return 0;
                        }
                    }
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[ERROR] Failed to fetch price for {symbol}: {response.StatusCode}");
                        return 0;
                    }

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    var priceData = JsonSerializer.Deserialize<JsonNode>(jsonContent);
                    
                    if (priceData != null && priceData[_cryptoIdMap[symbol]] != null)
                    {
                        decimal price = priceData[_cryptoIdMap[symbol]]["usd"].GetValue<decimal>();
                        Console.WriteLine($"[DEBUG] Successfully fetched price for {symbol}: ${price:N2}");
                        return price;
                    }
                    
                    Console.WriteLine($"[ERROR] Invalid price data format for {symbol}");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Exception fetching price for {symbol}: {ex.Message}");
                    currentRetry++;
                    
                    if (currentRetry <= maxRetries)
                    {
                        await Task.Delay(1000 * currentRetry);
                        continue;
                    }
                    
                    return 0;
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Fetches historical price data for a cryptocurrency over a specified number of days.
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol.</param>
        /// <param name="days">The number of days of historical data to fetch.</param>
        /// <returns>A CryptoPriceHistory object containing the price data.</returns>
        public async Task<CryptoPriceHistory> GetPriceHistory(string symbol, int days)
        {
            try
            {
                string cacheKey = $"{symbol}_{days}";
                if (_priceHistoryCache.TryGetValue(cacheKey, out var cachedData) && 
                    _priceHistoryLastFetched.TryGetValue(cacheKey, out var lastFetched))
                {
                    if ((DateTime.UtcNow - lastFetched).TotalMinutes < CACHE_EXPIRY_MINUTES)
                    {
                        Console.WriteLine($"[DEBUG] Using cached price history for {symbol}");
                        return cachedData;
                    }
                }

                if (!_cryptoIdMap.ContainsKey(symbol))
                {
                    Console.WriteLine($"[ERROR] Unknown symbol: {symbol}");
                    return new CryptoPriceHistory { Symbol = symbol };
                }

                string cryptoId = _cryptoIdMap[symbol];
                string url = string.Format(HISTORY_API_URL, cryptoId, days);
                
                Console.WriteLine($"[DEBUG] Fetching price history for {symbol} from {url}");
                
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string jsonContent = await response.Content.ReadAsStringAsync();
                var historyData = JsonSerializer.Deserialize<JsonNode>(jsonContent);
                
                var priceHistory = new CryptoPriceHistory
                {
                    Symbol = symbol,
                    LastUpdated = DateTime.UtcNow
                };

                if (historyData != null && historyData["prices"] != null)
                {
                    var pricePoints = new List<PricePoint>();
                    decimal highestPrice = decimal.MinValue;
                    decimal lowestPrice = decimal.MaxValue;

                    foreach (var point in historyData["prices"].AsArray())
                    {
                        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(point[0].GetValue<long>()).UtcDateTime;
                        var price = point[1].GetValue<decimal>();
                        
                        pricePoints.Add(new PricePoint { Timestamp = timestamp, Price = price });
                        
                        highestPrice = Math.Max(highestPrice, price);
                        lowestPrice = Math.Min(lowestPrice, price);
                    }

                    priceHistory.PricePoints = pricePoints;
                    priceHistory.HighestPrice = highestPrice;
                    priceHistory.LowestPrice = lowestPrice;
                    
                    _priceHistoryCache[cacheKey] = priceHistory;
                    _priceHistoryLastFetched[cacheKey] = DateTime.UtcNow;
                    
                    Console.WriteLine($"[DEBUG] Successfully fetched price history for {symbol}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Invalid price history data format for {symbol}");
                }

                return priceHistory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch price history for {symbol}: {ex.Message}");
                return new CryptoPriceHistory { Symbol = symbol };
            }
        }
    }
}
