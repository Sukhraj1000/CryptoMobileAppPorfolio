using SQLite;

namespace CryptoApp.Models;

public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string CryptoName { get; set; } = string.Empty; 
    public decimal Amount { get; set; }
    public decimal BuyPrice { get; set; }
    public string Currency { get; set; } = string.Empty; 
    public DateTime Date { get; set; } = DateTime.UtcNow; 
}