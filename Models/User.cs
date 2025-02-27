using Supabase.Postgrest.Models;
using System;

namespace CryptoApp.Models
{
    [Supabase.Postgrest.Attributes.Table("users")]
    public class User : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("userid", false)]
        public Guid Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("username")]
        public string Username { get; set; } = string.Empty;
        

        [Supabase.Postgrest.Attributes.Column("balance")]
        public decimal Balance { get; set; }
    }
}