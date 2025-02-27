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

        public DatabaseService(Supabase.Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // Get all user transactions (Portfolio Holdings)
        public async Task<List<Transaction>> GetAllHoldingsAsync()
        {
            try
            {
                var response = await _client.From<Transaction>().Get();
                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch holdings: {ex.Message}");
                return new List<Transaction>();
            }
        }

        // Get transactions for a specific user
        public async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId)
        {
            try
            {
                var response = await _client
                    .From<Transaction>()
                    .Filter("user_id", Constants.Operator.Equals, userId)
                    .Get();

                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch user transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }

        // Add a new transaction
        public async Task AddTransactionAsync(Transaction transaction)
        {
            try
            {
                await _client.From<Transaction>().Insert(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add transaction: {ex.Message}");
            }
        }

        // Update an existing transaction
        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            try
            {
                await _client.From<Transaction>().Update(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update transaction: {ex.Message}");
            }
        }

        // Delete a transaction
        public async Task DeleteTransactionAsync(int transactionId)
        {
            try
            {
                await _client
                    .From<Transaction>()
                    .Filter("id", Constants.Operator.Equals, transactionId)
                    .Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete transaction: {ex.Message}");
            }
        }

        // ✅ Get deposits for a specific user
        public async Task<List<Deposit>> GetUserDepositsAsync(Guid userId)
        {
            try
            {
                var response = await _client
                    .From<Deposit>()
                    .Filter("user_id", Constants.Operator.Equals, userId) 
                    .Get();

                return response.Models.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch user deposits: {ex.Message}");
                return new List<Deposit>();
            }
        }

        // Add a new deposit
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
        public async Task UpdateUserBalanceAsync(Guid userId, decimal newBalance)
        {
            try
            {
                // 🔍 Fetch the existing user first
                var userResponse = await _client
                    .From<User>()
                    .Match(new Dictionary<string, string> { { "id", userId.ToString() } })
                    .Single();

                if (userResponse == null)
                {
                    Console.WriteLine($"[ERROR] User with ID {userId} not found.");
                    return;
                }

                // Update balance while keeping other fields intact
                userResponse.Balance = newBalance;

                // Perform update
                var response = await _client
                    .From<User>()
                    .Match(new Dictionary<string, string> { { "id", userId.ToString() } })
                    .Update(userResponse, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

                Console.WriteLine($"[DEBUG] Balance updated successfully for user {userResponse.Username}: {response.Models.Count} rows affected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update balance: {ex.Message}");
            }
        }










    }
}
