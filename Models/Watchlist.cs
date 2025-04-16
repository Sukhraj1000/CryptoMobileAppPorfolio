using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CryptoApp.Models
{
    [Table("watchlist")]
    public class Watchlist : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("crypto_symbol")]
        public string CryptoSymbol { get; set; } = string.Empty;

        [Column("last_price")]
        public decimal LastPrice { get; set; } = 0;

        [Column("price")] 
        public decimal Price { get; set; } = 0;

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}