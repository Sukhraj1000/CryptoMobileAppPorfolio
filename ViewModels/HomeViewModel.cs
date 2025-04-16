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
using Microcharts;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace CryptoApp.ViewModels
{
    /// <summary>
    /// View model for the home page, managing watchlist and price charts.
    /// </summary>
    public class HomeViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly CryptoService _cryptoService;

        /// <summary>
        /// Collection of cryptocurrencies in the user's watchlist.
        /// </summary>
        private ObservableCollection<Watchlist> _watchlistItems = new();
        public ObservableCollection<Watchlist> WatchlistItems 
        { 
            get => _watchlistItems;
            set => SetProperty(ref _watchlistItems, value);
        }

        // Chart properties for different cryptocurrencies
        private Chart _btcChart;
        public Chart BtcChart
        {
            get => _btcChart;
            set => SetProperty(ref _btcChart, value);
        }

        private Chart _ethChart;
        public Chart EthChart
        {
            get => _ethChart;
            set => SetProperty(ref _ethChart, value);
        }

        private Chart _solChart;
        public Chart SolChart
        {
            get => _solChart;
            set => SetProperty(ref _solChart, value);
        }

        /// <summary>
        /// Indicates whether the price charts are currently loading.
        /// </summary>
        private bool _isLoadingCharts;
        public bool IsLoadingCharts
        {
            get => _isLoadingCharts;
            set => SetProperty(ref _isLoadingCharts, value);
        }

        /// <summary>
        /// Command to refresh the watchlist data.
        /// </summary>
        public ICommand RefreshWatchlistCommand { get; }

        /// <summary>
        /// Command to refresh the price charts.
        /// </summary>
        public ICommand RefreshChartsCommand { get; }

        public HomeViewModel(IDatabaseService databaseService, CryptoService cryptoService)
        {
            try
            {
                Console.WriteLine("[DEBUG] Initializing HomeViewModel...");
                
                _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
                _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));

                RefreshWatchlistCommand = new AsyncRelayCommand(RefreshWatchlist);
                RefreshChartsCommand = new AsyncRelayCommand(RefreshCharts);
                
                Task.Run(LoadStoredPrices);
                Task.Run(RefreshCharts);
                
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

        // Fetch price history and create charts
        public async Task RefreshCharts()
        {
            try
            {
                IsLoadingCharts = true;
                
                Console.WriteLine("[DEBUG] Fetching price history for charts...");
                
                // Fetch all data concurrently
                var btcHistoryTask = _cryptoService.GetPriceHistory("BTC", 7);
                var ethHistoryTask = _cryptoService.GetPriceHistory("ETH", 7);
                var solHistoryTask = _cryptoService.GetPriceHistory("SOL", 7);
                
                await Task.WhenAll(btcHistoryTask, ethHistoryTask, solHistoryTask);
                
                var btcHistory = await btcHistoryTask;
                var ethHistory = await ethHistoryTask;
                var solHistory = await solHistoryTask;
                
                // Create charts on the UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BtcChart = CreateChart(btcHistory, SKColor.Parse("#F7931A"));  // Bitcoin orange
                    EthChart = CreateChart(ethHistory, SKColor.Parse("#627EEA"));  // Ethereum blue
                    SolChart = CreateChart(solHistory, SKColor.Parse("#14F195"));  // Solana green
                    
                    IsLoadingCharts = false;
                    
                    Console.WriteLine("[DEBUG] Charts updated successfully");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to refresh charts: {ex.Message}");
                IsLoadingCharts = false;
            }
        }
        
        // Helper method to create a line chart from price history data
        private LineChart CreateChart(CryptoPriceHistory history, SKColor color)
        {
            if (history == null || history.PricePoints.Count == 0)
                return null;
                
            // Get the min and max values to create a better scale
            var minPrice = (float)history.PricePoints.Min(p => p.Price);
            var maxPrice = (float)history.PricePoints.Max(p => p.Price);
            
            // Calculate a proper range for y-axis (add 5% padding)
            var range = maxPrice - minPrice;
            var padding = range * 0.05f;
            minPrice = Math.Max(0, minPrice - padding);
            maxPrice = maxPrice + padding;
            
            // Group data points by day and select exactly one representative point per day
            var sortedPoints = history.PricePoints.OrderBy(p => p.Timestamp).ToList();
            
            // Group all points by date (one entry per day)
            var pointsByDay = sortedPoints
                .GroupBy(p => p.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
                
            // Get a list of dates in our 7-day range
            var startDate = sortedPoints.First().Timestamp.Date;
            var dates = new List<DateTime>();
            
            // Ensure we have exactly 7 days, no duplicates
            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);
                if (date > sortedPoints.Last().Timestamp.Date)
                    break;
                dates.Add(date);
            }
            
            // Create a single data point for each day
            var keyPoints = new List<PricePoint>();
            
            foreach (var date in dates)
            {
                if (pointsByDay.TryGetValue(date, out var pointsOnDay) && pointsOnDay.Any())
                {
                    // Use midday point for best representation of day's price
                    var middayPoint = pointsOnDay
                        .OrderBy(p => Math.Abs((p.Timestamp.Hour * 60 + p.Timestamp.Minute) - (12 * 60)))
                        .FirstOrDefault();
                        
                    keyPoints.Add(middayPoint ?? pointsOnDay.First());
                }
            }
            
            // Format price labels based on price magnitude
            Func<double, string> formatPrice = (price) => {
                if (price >= 1000)
                    return $"${price:N0}";
                else if (price >= 100)
                    return $"${price:N0}"; 
                else if (price >= 1)
                    return $"${price:N2}"; 
                else
                    return $"${price:N4}"; 
            };
            
            var priceLabelColor = new SKColor(
                (byte)Math.Min(255, color.Red + 40),
                (byte)Math.Min(255, color.Green + 40),
                (byte)Math.Min(255, color.Blue + 40),
                255);
            
            var entries = new List<ChartEntry>();
            var processedDates = new HashSet<DateTime>();
            foreach (var point in keyPoints.OrderBy(p => p.Timestamp))
            {
                var date = point.Timestamp.Date;
                
                if (processedDates.Contains(date))
                    continue;
                    
                processedDates.Add(date);
                
                entries.Add(new ChartEntry((float)point.Price)
                {
                    Color = color,
                    ValueLabel = formatPrice((double)point.Price),
                    Label = point.Timestamp.ToString("ddd"),
                    TextColor = SKColors.White,
                    ValueLabelColor = priceLabelColor
                });
            }
                
            if (entries.Count < 7 && sortedPoints.Count > entries.Count)
            {
                var missingDays = dates.Where(date => !processedDates.Contains(date)).ToList();
                
                foreach (var missingDate in missingDays)
                {
                    // Find the closest points before and after
                    var prevPoint = keyPoints.Where(p => p.Timestamp.Date < missingDate)
                        .OrderByDescending(p => p.Timestamp).FirstOrDefault();
                    var nextPoint = keyPoints.Where(p => p.Timestamp.Date > missingDate)
                        .OrderBy(p => p.Timestamp).FirstOrDefault();
                        
                    if (prevPoint != null && nextPoint != null)
                    {
                        // Interpolate price
                        var daysBetween = (nextPoint.Timestamp.Date - prevPoint.Timestamp.Date).TotalDays;
                        var daysFromPrev = (missingDate - prevPoint.Timestamp.Date).TotalDays;
                        var ratio = daysFromPrev / daysBetween;
                        
                        var interpolatedPrice = prevPoint.Price + 
                            ((nextPoint.Price - prevPoint.Price) * (decimal)ratio);
                            
                        var newPoint = new PricePoint
                        {
                            Price = interpolatedPrice,
                            Timestamp = missingDate.AddHours(12)
                        };
                        
                        keyPoints.Add(newPoint);
                        processedDates.Add(missingDate);
                        
                        // Add to entries with proper label
                        entries.Add(new ChartEntry((float)newPoint.Price)
                        {
                            Color = color,
                            ValueLabel = formatPrice((double)newPoint.Price),
                            Label = newPoint.Timestamp.ToString("ddd"),
                            TextColor = SKColors.White,
                            ValueLabelColor = priceLabelColor
                        });
                    }
                }
            }
            
            // Sort entries by timestamp for proper display
            entries = entries.OrderBy(e => {
                var matchingPoint = keyPoints.FirstOrDefault(p => 
                    Math.Abs(Convert.ToDouble(p.Price) - Convert.ToDouble(e.Value)) < 0.01);
                return matchingPoint?.Timestamp ?? DateTime.MinValue;
            }).ToList();
            
            return new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Spline,
                LineSize = 4,
                PointMode = PointMode.Circle,
                PointSize = 15,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 30,
                ValueLabelTextSize = 30,
                LabelColor = SKColors.White,
                AnimationDuration = TimeSpan.FromMilliseconds(500),
                ValueLabelOrientation = Orientation.Horizontal,
                MinValue = minPrice,
                MaxValue = maxPrice,
                LabelOrientation = Orientation.Horizontal,
                Margin = 20,
                ShowYAxisLines = false,
                ShowYAxisText = false,
                ValueLabelOption = ValueLabelOption.TopOfElement
            };
        }
    }
}