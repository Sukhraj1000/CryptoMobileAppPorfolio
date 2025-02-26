using Supabase.Postgrest.Models;
using System;
using SQLite;

namespace CryptoApp.Models
{
    [Supabase.Postgrest.Attributes.Table("transactions")] 
    public class Transaction : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("cryptoname")]
        public string CryptoName { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("amount")]
        public decimal Amount { get; set; }

        [Supabase.Postgrest.Attributes.Column("buyprice")]
        public decimal BuyPrice { get; set; }

        [Supabase.Postgrest.Attributes.Column("currency")]
        public string Currency { get; set; } = "USD";

        [Supabase.Postgrest.Attributes.Column("date")] 
        public DateTime Date { get; set; } 
    }
}