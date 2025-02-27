using Supabase.Postgrest.Models;
using System;

namespace CryptoApp.Models
{
    [Supabase.Postgrest.Attributes.Table("deposits")]
    public class Deposit : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("userid")]
        public Guid UserId { get; set; }

        [Supabase.Postgrest.Attributes.Column("amount")]
        public decimal Amount { get; set; }

        [Supabase.Postgrest.Attributes.Column("date")]
        public DateTime Date { get; set; }
    }
}