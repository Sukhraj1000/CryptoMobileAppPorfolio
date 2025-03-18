using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using CryptoApp.Services;
using Microsoft.Maui.Dispatching;

namespace CryptoApp.ViewModels
{
    public class HomeViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly CryptoService _cryptoService;

        private ObservableCollection<Watchlist> _watchlistItems = new();
        public ObservableCollection<Watchlist> WatchlistItems 
        { 
            get => _watchlistItems;
            set => SetProperty(ref _watchlistItems, value);
        }

        public ICommand RefreshWatchlistCommand { get; }

        public HomeViewModel(IDatabaseService databaseService, CryptoService cryptoService)
        {
            try
            {
                Console.WriteLine("[DEBUG] Initializing HomeViewModel...");
                
                _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
                _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));

                RefreshWatchlistCommand = new AsyncRelayCommand(RefreshWatchlist);
                
                // Start loading data immediately but don't block UI
                Task.Run(LoadStoredPrices);
                
                Console.WriteLine("[DEBUG] HomeViewModel constructor completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Error in HomeViewModel constructor: {ex.Message}");
                Console.WriteLine($"[STACK] {ex.StackTrace}");
            }
        }

        // Loads the last stored prices from the database when the app starts.
        private async Task LoadStoredPrices()
        {
            try
            {
                Console.WriteLine("[DEBUG] Loading stored watchlist prices from DB...");

                var storedWatchlist = await _databaseService.GetWatchlistAsync();
                
                Console.WriteLine($"[DEBUG] Found {storedWatchlist.Count} items in watchlist DB");
                
                var newWatchlist = new ObservableCollection<Watchlist>();
                
                foreach (var entry in storedWatchlist)
                {
                    newWatchlist.Add(entry);
                    Console.WriteLine($"[DEBUG] Added to collection: {entry.CryptoSymbol} - {entry.LastPrice}");
                }
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Replace the entire collection instead of modifying it
                        WatchlistItems = newWatchlist;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Error updating UI on main thread: {ex.Message}");
                    }
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load stored prices: {ex.Message}");
            }
        }
        
        //Fetches the latest prices from the API and updates the database + UI.
        public async Task RefreshWatchlist()
        {
            try
            {
                Console.WriteLine("[DEBUG] Fetching latest watchlist prices...");
                
                var (apiSuccess, latestPrices) = await _cryptoService.FetchLivePrices();
                
                if (!apiSuccess || latestPrices.Count == 0)
                {
                    Console.WriteLine("[ERROR] Failed to fetch live prices. Using stored values.");
                    await App.Current.MainPage.DisplayAlert("Error", "Failed to fetch live prices. Using last stored values.", "OK");
                    return;
                }
                
                Console.WriteLine("[DEBUG] Successfully fetched latest watchlist prices.");
                
                var symbolToApiName = new Dictionary<string, string>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "SOL", "solana" }
                };
                
                var updatedItems = new ObservableCollection<Watchlist>();
                
                foreach (var entry in WatchlistItems)
                {
                    if (!symbolToApiName.TryGetValue(entry.CryptoSymbol.ToUpper(), out string apiKey)) continue;
                    if (!latestPrices.TryGetValue(apiKey, out var latestPrice)) continue;
                    
                    Console.WriteLine($"[DEBUG] Updating UI & DB: {entry.CryptoSymbol} = {latestPrice}");
                    
                    entry.LastPrice = latestPrice;
                    entry.LastUpdated = DateTime.UtcNow;
                    
                    await _databaseService.UpdateWatchlistEntry(entry);
                    updatedItems.Add(entry);
                }
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WatchlistItems = updatedItems;
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to refresh watchlist: {ex.Message}");
            }
        }
    }
}