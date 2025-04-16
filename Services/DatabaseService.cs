using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using Supabase;
using Supabase.Postgrest;

namespace CryptoApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly Supabase.Client _client;
        private readonly CryptoService _cryptoService;

        public DatabaseService(Supabase.Client client, CryptoService cryptoService)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        //  Get User's Crypto Balance for a Specific Coin
        public async Task<decimal> GetCryptoBalanceAsync(Guid userId, string cryptoSymbol)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Fetching crypto balance for {userId} - {cryptoSymbol}");

                var response = await _client
                    .From<Wallet>()
                    .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                    .Filter("crypto_symbol", Constants.Operator.Equals, cryptoSymbol)
                    .Single();

                return response?.Balance ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch crypto balance: {ex.Message}");
                return 0;
            }
        }

        // Update (or Create) Crypto Balance
        public async Task UpdateCryptoBalanceAsync(Guid userId, string cryptoSymbol, decimal amountChange)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Updating crypto balance for {userId} - {cryptoSymbol} by {amountChange}");

                var wallet = await _client
                    .From<Wallet>()
                    .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                    .Filter("crypto_symbol", Constants.Operator.Equals, cryptoSymbol)
                    .Single();

                if (wallet == null)
                {
                    var newWallet = new Wallet
                    {
                        UserId = userId,
                        CryptoSymbol = cryptoSymbol,
                        Balance = amountChange
                    };
                    await _client.From<Wallet>().Insert(newWallet);
                    Console.WriteLine("[DEBUG] Wallet entry created successfully.");
                }
                else
                {
                    wallet.Balance += amountChange;
                    await _client.From<Wallet>().Update(wallet);
                    Console.WriteLine($"[DEBUG] Updated {cryptoSymbol} balance to: {wallet.Balance}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update crypto balance: {ex.Message}");
            }
        }

        // Store a Transaction
        public async Task AddTransactionAsync(Transaction transaction)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Adding transaction: {transaction.TransactionType} {transaction.Amount} {transaction.CryptoSymbol}");
                await _client.From<Transaction>().Insert(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add transaction: {ex.Message}");
            }
        }

        // Fetch All User Holdings (Transactions)
        public async Task<List<Transaction>> GetAllHoldingsAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] Fetching all user holdings...");

                var response = await _client
                    .From<Transaction>()
                    .Filter("transaction_type", Constants.Operator.Equals, "BUY") 
                    .Get();

                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch holdings: {ex.Message}");
                return new List<Transaction>();
            }
        }
        // Update transaction holdings for users
        public async Task UpdateUserCryptoHoldingsAsync(Guid userId, string cryptoSymbol, decimal newBalance)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Updating {cryptoSymbol} balance for user {userId} to {newBalance}");

                var wallet = await _client
                    .From<Wallet>()
                    .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                    .Filter("crypto_symbol", Constants.Operator.Equals, cryptoSymbol)
                    .Single();

                if (wallet == null)
                {
                    // If no wallet exists, create a new one
                    var newWallet = new Wallet
                    {
                        UserId = userId,
                        CryptoSymbol = cryptoSymbol,
                        Balance = newBalance
                    };
                    await _client.From<Wallet>().Insert(newWallet);
                    Console.WriteLine("[DEBUG] New wallet created.");
                }
                else
                {
                    wallet.Balance = newBalance;
                    await _client.From<Wallet>().Update(wallet);
                    Console.WriteLine($"[DEBUG] {cryptoSymbol} balance updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update crypto holdings: {ex.Message}");
            }
        }

        // Fetch Transactions for a User
        public async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Fetching transactions for {userId}");

                var response = await _client
                    .From<Transaction>()
                    .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                    .Get();
                foreach (var transaction in response.Models)
                {
                    transaction.PercentageChange = transaction.PercentageChange;
                }

                Console.WriteLine($"[DEBUG] Fetched {response.Models.Count} transactions.");
                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }




        // Get Deposits for a User
        public async Task<List<Deposit>> GetUserDepositsAsync(Guid userId)
        {
            try
            {
                var response = await _client
                    .From<Deposit>()
                    .Filter("userid", Constants.Operator.Equals, userId.ToString())
                    .Order("deposit_date", Constants.Ordering.Descending)
                    .Limit(6)
                    .Get();

                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch user deposits: {ex.Message}");
                return new List<Deposit>();
            }
        }

        // Add a Deposit
        public async Task AddDepositAsync(Deposit deposit)
        {
            try
            {
                await _client.From<Deposit>().Insert(deposit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add deposit: {ex.Message}");
            }
        }

        // Get User's Fiat (Cash) Balance
        public async Task<decimal> GetUserBalanceAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Fetching balance for User ID: {userId}");

                var userResponse = await _client
                    .From<User>()
                    .Filter("id", Constants.Operator.Equals, userId.ToString())
                    .Single();

                return userResponse?.Balance ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch user balance: {ex.Message}");
                return 0;
            }
        }

        // Update User's Fiat (Cash) Balance
        public async Task UpdateUserBalanceAsync(Guid userId, decimal newBalance)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Updating user balance: {newBalance} for User ID: {userId}");

                var userResponse = await _client
                    .From<User>()
                    .Match(new Dictionary<string, string> { { "id", userId.ToString() } })
                    .Single();

                if (userResponse == null)
                {
                    Console.WriteLine($"[ERROR] User with ID {userId} not found.");
                    return;
                }

                userResponse.Balance = newBalance;

                await _client
                    .From<User>()
                    .Match(new Dictionary<string, string> { { "id", userId.ToString() } })
                    .Update(userResponse);

                Console.WriteLine("[DEBUG] Successfully updated balance.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update balance: {ex.Message}");
            }
        }

        // Fetch User's Crypto Balances
        public async Task<List<Wallet>> GetUserCryptoBalancesAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Fetching all crypto balances for {userId}");

                var response = await _client
                    .From<Wallet>()
                    .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                    .Get();

                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch user crypto balances: {ex.Message}");
                return new List<Wallet>();
            }
        }

        // Update Portfolio Percentage Change
        public async Task UpdatePortfolioPercentageChange(Guid userId, List<Transaction> transactions)
{
    try
    {
        Console.WriteLine($"[DEBUG] Updating portfolio percentage changes for {userId}");

        foreach (var transaction in transactions)
        {
            if (string.IsNullOrEmpty(transaction.TransactionType)) 
            {
                Console.WriteLine($"[ERROR] Skipping update for transaction {transaction.Id} due to empty transaction type.");
                continue; 
            }

            await _client.From<Transaction>()
                .Match(new Dictionary<string, string> { { "id", transaction.Id.ToString() } })
                .Update(transaction);

        }

        Console.WriteLine("[DEBUG] Successfully updated percentage changes in the database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to update portfolio percentage changes: {ex.Message}");
    }
}
        // Update the portfolio value on UI
        public async Task UpdateTransactionFinalValues(Guid userId, List<Transaction> transactions)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Updating final total values and percentage changes for {userId}");

                foreach (var transaction in transactions)
                {
                    Console.WriteLine($"[DEBUG] Updating Transaction ID: {transaction.Id} | Final Value: {transaction.FinalTotalValue} | % Change: {transaction.PercentageChange}");

                    var updateResult = await _client.Rpc("update_transaction_final_values", new
                    {
                        transaction_id = transaction.Id,
                        new_final_value = transaction.FinalTotalValue,
                        new_percentage_change = transaction.PercentageChange
                    });

                    Console.WriteLine($"[DEBUG] Update result: {updateResult}");
                }

                Console.WriteLine("[DEBUG] Successfully updated transaction values in the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update transaction values: {ex.Message}");
            }
        }
        
        // Fetches new watchlist values
        public async Task UpdateWatchlistPrices(Dictionary<string, decimal> latestPrices)
        {
            try
            {
                Console.WriteLine("[DEBUG] Updating watchlist prices in database...");

                var watchlistEntries = await GetWatchlistAsync(); // Get existing watchlist from DB
                if (watchlistEntries == null || watchlistEntries.Count == 0)
                {
                    Console.WriteLine("[ERROR] No watchlist entries found.");
                    return;
                }

                foreach (var entry in watchlistEntries)
                {
                    if (latestPrices.TryGetValue(entry.CryptoSymbol.ToLower(), out var latestPrice))
                    {
                        Console.WriteLine($"[DEBUG] Updating {entry.CryptoSymbol} price: {latestPrice}");

                        entry.LastPrice = latestPrice;
                        entry.LastUpdated = DateTime.UtcNow;

                        await UpdateWatchlistEntry(entry);
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] No latest price found for {entry.CryptoSymbol}, skipping update.");
                    }
                }

                Console.WriteLine("[DEBUG] Watchlist prices updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update watchlist prices: {ex.Message}");
            }
        }
        public async Task UpdateWatchlistEntry(Watchlist entry)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Saving updated entry: {entry.CryptoSymbol} - {entry.LastPrice}");

                await _client
                    .From<Watchlist>()
                    .Match(new Dictionary<string, string> { { "crypto_symbol", entry.CryptoSymbol } }) 
                    .Update(entry);

                Console.WriteLine($"[DEBUG] Successfully updated {entry.CryptoSymbol} in watchlist.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update watchlist entry: {ex.Message}");
            }
        }

        
        public async Task<List<Watchlist>> GetWatchlistAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] Fetching stored watchlist prices...");

                var response = await _client
                    .From<Watchlist>()
                    .Filter("crypto_symbol", Constants.Operator.In, new List<string> { "BTC", "ETH", "SOL" }) 
                    .Get();

                Console.WriteLine("[DEBUG] Watchlist retrieved successfully.");

                return response.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch watchlist: {ex.Message}");
                return new List<Watchlist>();
            }
        }



    }
}
