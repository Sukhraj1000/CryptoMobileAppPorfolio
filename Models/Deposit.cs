using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace CryptoApp.Models
{
    [Table("deposits")]
    public class Deposit : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("deposit_date")]
        public DateTime DepositDate { get; set; }

        [Column("userid")]
        public Guid UserId { get; set; }
    }
}