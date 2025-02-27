using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Interfaces
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<User> GetUserByIdAsync(Guid userId);
        Task<List<Deposit>> GetUserDepositsAsync(Guid userId);
        Task UpdateUserBalanceAsync(Guid userId, decimal newBalance);
        Task AddDepositAsync(Deposit deposit);
        Task<List<Transaction>> GetAllHoldingsAsync();
        Task AddTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int transactionId);
    

        // Initialization
        Task TestSupabaseConnection();
    }
}