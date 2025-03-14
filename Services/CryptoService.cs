using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Services
{
    public class CryptoService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.coingecko.com/api/v3/simple/price?ids={0}&vs_currencies=usd";
        private readonly Dictionary<string, string> _cryptoIdMap = new()
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "SOL", "solana" }
        };

        public CryptoService()
        {
            _httpClient = new HttpClient();
        }

        /// Fetches live prices for the watchlist cryptos.
        public async Task<Dictionary<string, decimal>> FetchLivePrices()
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

                return parsedPrices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch live prices: {ex.Message}");
                return new Dictionary<string, decimal>();
            }
        }





        /// Fetches the latest price for a specific cryptocurrency.
        public async Task<decimal> GetLatestPrice(string symbol)
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
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var priceData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(json);

                if (priceData != null && priceData.TryGetValue(cryptoId, out var price))
                {
                    return price["usd"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch price for {symbol}: {ex.Message}");
            }

            return 0;
        }
    }
}
