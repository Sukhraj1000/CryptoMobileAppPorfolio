using CryptoApp.Models;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<Transaction>> GetAllHoldingsAsync();
    Task<List<Transaction>> GetTransactionsAsync();
    Task AddTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(Transaction transaction);
    Task TestSupabaseConnection();
}