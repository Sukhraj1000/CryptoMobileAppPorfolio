using CryptoApp.Interfaces;
using CryptoApp.Models;
using SQLite;

namespace CryptoApp.Services; 

public class DatabaseService : IDatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<Transaction>().Wait();
    }

    public Task<int> AddTransactionAsync(Transaction transaction) => _database.InsertAsync(transaction);
    public Task<List<Transaction>> GetTransactionsAsync() => _database.Table<Transaction>().ToListAsync();
    public Task<int> UpdateTransactionAsync(Transaction transaction) => _database.UpdateAsync(transaction);
    public Task<int> DeleteTransactionAsync(Transaction transaction) => _database.DeleteAsync(transaction);
}