using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace CryptoApp.Models
{
    [Table("transactions")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "USD";

        [Column("user_id")]
        public Guid UserId { get; set; }
    }
}