using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Services
{
    public class CryptoService
    {
        private readonly HttpClient _httpClient;
        private readonly Subject<CryptoAsset> _priceUpdates;
        private const string API_URL = "https://api.coingecko.com/api/v3/simple/price?ids={0}&vs_currencies=usd";
        private readonly List<string> _watchlistSymbols = new() { "bitcoin", "ethereum", "solana" };

        public IObservable<CryptoAsset> PriceUpdates => _priceUpdates; 

        public CryptoService()
        {
            _httpClient = new HttpClient();
            _priceUpdates = new Subject<CryptoAsset>();
        }

        public async Task FetchLivePrices()
        {
            try
            {
                string ids = string.Join(",", _watchlistSymbols);
                string url = string.Format(API_URL, ids);

                Console.WriteLine($"Fetching latest prices from: {url}");
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var priceData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(json);

                if (priceData != null)
                {
                    foreach (var symbol in _watchlistSymbols)
                    {
                        if (priceData.TryGetValue(symbol, out var price))
                        {
                            var asset = new CryptoAsset
                            {
                                Symbol = symbol.ToUpper(),
                                Name = char.ToUpper(symbol[0]) + symbol.Substring(1),
                                Price = price["usd"]
                            };

                            Console.WriteLine($"{asset.Name}: ${asset.Price}");
                            _priceUpdates.OnNext(asset); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
            }
        }

        public decimal GetLatestPrice(string symbol)
        {
            decimal latestPrice = 0;
            _priceUpdates.Subscribe(asset =>
            {
                if (asset.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                {
                    latestPrice = asset.Price;
                }
            });

            return latestPrice;
        }
    }
}
