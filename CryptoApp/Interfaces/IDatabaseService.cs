using CryptoApp.Models;

namespace CryptoApp.Interfaces; 

public interface IDatabaseService
{
    Task<int> AddTransactionAsync(Transaction transaction);
    Task<List<Transaction>> GetTransactionsAsync();
    Task<int> UpdateTransactionAsync(Transaction transaction);
    Task<int> DeleteTransactionAsync(Transaction transaction);
}