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
                Console.WriteLine(" Supabase Initialised");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to initialise Supabase: {ex.Message}");
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

        public async Task<List<Transaction>> GetAllHoldingsAsync()
        {
            try
            {
                var response = await _client.From<Transaction>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                return new List<Transaction>();
            }
        }

        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            try
            {
                var response = await _client.From<Transaction>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                return new List<Transaction>();
            }
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            transaction.Date = DateTime.UtcNow; 
            await _client.From<Transaction>().Insert(transaction);
        }



        public async Task DeleteTransactionAsync(Transaction transaction)
        {
            try
            {
                await _client
                    .From<Transaction>()
                    .Filter(t => t.Id, Constants.Operator.Equals, transaction.Id) 
                    .Delete();

                Console.WriteLine($"Transaction deleted: {transaction.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR deleting transaction: {ex.Message}");
            }
        }
    }
}
