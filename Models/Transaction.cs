using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using Newtonsoft.Json;

namespace CryptoApp.Models
{
    [Table("transactions")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("crypto_symbol")]
        public string CryptoSymbol { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; } = 0;

        [Column("price_at_transaction")]
        public decimal PriceAtTransaction { get; set; } = 0;

        [Column("transaction_type")]
        public string TransactionType { get; set; } = string.Empty;

        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Column("total_value")]
        public decimal TotalValue { get; set; } = 0;

        [Column("percentage_change")]
        public decimal? PercentageChange { get; set; } = 0;

        [Column("final_total_value")]
        public decimal? FinalTotalValue { get; set; } = 0;

    }
}