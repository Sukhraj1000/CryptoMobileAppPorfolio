using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace CryptoApp.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("balance")]
        public decimal Balance { get; set; } = 0;
    }
}