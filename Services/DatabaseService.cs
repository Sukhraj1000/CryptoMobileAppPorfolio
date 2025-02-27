using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using Supabase;
using Supabase.Interfaces;
using Supabase.Postgrest;

namespace CryptoApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly Supabase.Client _client;

        public DatabaseService()
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ??
                              "https://jfukhwbyszlclqfhecmn.supabase.co";

            var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY") ??
                              "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImpmdWtod2J5c3psY2xxZmhlY21uIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzkyMTc3NDgsImV4cCI6MjA1NDc5Mzc0OH0.KikzTEL99_ngl6mzCpep76wFzcrEDi_aqRfed_SgApg";

            _client = new Supabase.Client(supabaseUrl, supabaseKey);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _client.InitializeAsync();
                Console.WriteLine("Supabase Initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to initialize Supabase: {ex.Message}");
            }
        }
        public async Task TestSupabaseConnection()
        {
            try
            {
                var transactions = await _client.From<Transaction>().Get();
                Console.WriteLine($"Transactions Fetched: {transactions.Models.Count}");

                foreach (var tx in transactions.Models)
                {
                    Console.WriteLine($"{tx.Id} | {tx.CryptoName} | {tx.Symbol} | {tx.Amount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            var userIdString = userId.ToString(); 

            var userResponse = await _client
                .From<User>()
                .Filter("userid", Constants.Operator.Equals, userIdString) 
                .Single();

            return userResponse;
        }

        public async Task<List<Deposit>> GetUserDepositsAsync(Guid userId)
        {
            var response = await _client.From<Deposit>()
                .Filter("userid", Constants.Operator.Equals, userId)
                .Get();

            return response.Models;
        }

        public async Task UpdateUserBalanceAsync(Guid userId, decimal newBalance)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    Console.WriteLine("User not found. Cannot update balance.");
                    return;
                }

                user.Balance = newBalance;

                var response = await _client.From<User>().Update(user);
                if (response.Models.Count > 0)
                {
                    Console.WriteLine($"Balance updated successfully: {newBalance}");
                }
                else
                {
                    Console.WriteLine("Failed to update user balance.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR updating balance: {ex.Message}");
            }
        }

        public async Task AddDepositAsync(Deposit deposit)
        {
            await _client.From<Deposit>().Insert(deposit);
        }

        public async Task<List<Transaction>> GetAllHoldingsAsync()
        {
            var response = await _client.From<Transaction>().Get();
            return response.Models;
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await _client.From<Transaction>().Insert(transaction);
        }

        public async Task DeleteTransactionAsync(int transactionId)
        {
            await _client.From<Transaction>()
                .Filter("id", Constants.Operator.Equals, transactionId)
                .Delete();
        }
    }
}
