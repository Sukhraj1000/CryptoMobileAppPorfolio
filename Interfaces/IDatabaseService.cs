using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoApp.Models;

namespace CryptoApp.Interfaces
{
    public interface IDatabaseService
    {
        // Cash Balance
        Task UpdateUserBalanceAsync(Guid userId, decimal newBalance);
        Task<decimal> GetUserBalanceAsync(Guid userId);

        // Crypto Balance (Wallets)
        Task<decimal> GetCryptoBalanceAsync(Guid userId, string cryptoSymbol);
        Task<List<Wallet>> GetUserCryptoBalancesAsync(Guid userId);
        Task UpdateCryptoBalanceAsync(Guid userId, string cryptoSymbol, decimal amountChange); 

        // Crypto Holdings (Stored in Users Table)
        Task UpdateUserCryptoHoldingsAsync(Guid userId, string cryptoSymbol, decimal amountChange); 

        // Transactions (Buying/Selling)
        Task AddTransactionAsync(Transaction transaction);
        Task<List<Transaction>> GetUserTransactionsAsync(Guid userId);
        Task<List<Transaction>> GetAllHoldingsAsync();

        // Percentage Change Tracking
        Task UpdatePortfolioPercentageChange(Guid userId, List<Transaction> transactions);
        Task UpdateTransactionFinalValues(Guid userId, List<Transaction> transactions);

        // Deposits 
        Task<List<Deposit>> GetUserDepositsAsync(Guid userId);
        Task AddDepositAsync(Deposit deposit);
    }
}