using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CryptoApp.Models
{
    [Table("wallets")]
    public class Wallet : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } 

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("crypto_symbol")]
        public string CryptoSymbol { get; set; } = string.Empty;

        [Column("balance")]
        public decimal Balance { get; set; } = 0;
    }
}