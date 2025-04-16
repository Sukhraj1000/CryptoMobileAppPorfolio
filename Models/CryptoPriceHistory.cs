using System;

namespace CryptoApp.Models
{
    public class CryptoPriceHistory
    {
        public string Symbol { get; set; }
        public List<PricePoint> PricePoints { get; set; } = new List<PricePoint>();
        public decimal HighestPrice { get; set; }
        public decimal LowestPrice { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class PricePoint
    {
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }
} 