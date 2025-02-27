using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Interfaces
{
    public interface IDatabaseService
    {
        // User Balance Methods
        Task UpdateUserBalanceAsync(Guid userId, decimal newBalance);

        // Holdings (Transactions)
        Task<List<Transaction>> GetAllHoldingsAsync();
        Task<List<Transaction>> GetUserTransactionsAsync(Guid userId);
        Task AddTransactionAsync(Transaction transaction);
        Task UpdateTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int transactionId);

        // Deposits
        Task<List<Deposit>> GetUserDepositsAsync(Guid userId);
        Task AddDepositAsync(Deposit deposit);
    }
}