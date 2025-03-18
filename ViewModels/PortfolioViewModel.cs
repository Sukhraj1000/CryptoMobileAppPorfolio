using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using CryptoApp.Services;

namespace CryptoApp.ViewModels
{
    public class PortfolioViewModel : ObservableObject
    {
        private readonly CryptoService _cryptoService;
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private User _currentUser;

        public ObservableCollection<Transaction> PortfolioHoldings { get; private set; } = new();
        public ObservableCollection<string> AvailableCryptos { get; } = new() { "BTC", "ETH", "SOL" };

        private decimal _totalPortfolioValue;
        public decimal TotalPortfolioValue
        {
            get => _totalPortfolioValue;
            set => SetProperty(ref _totalPortfolioValue, value);
        }

        private decimal _portfolioChange;
        public decimal PortfolioChange
        {
            get => _portfolioChange;
            set => SetProperty(ref _portfolioChange, value);
        }

        private decimal _userBalance = 0;
        public decimal UserBalance
        {
            get => _userBalance;
            set => SetProperty(ref _userBalance, value);
        }

        private string _selectedBuyCrypto;
        public string SelectedBuyCrypto 
        { 
            get => _selectedBuyCrypto;
            set 
            {
                if (SetProperty(ref _selectedBuyCrypto, value) && !string.IsNullOrEmpty(value))
                {
                    // When crypto selection changes, update price info
                    Task.Run(async () => await CryptoSelectionChanged());
                }
            }
        }
        
        private decimal _buyAmount;
        public decimal BuyAmount 
        { 
            get => _buyAmount;
            set 
            {
                if (SetProperty(ref _buyAmount, value))
                {
                    // When amount changes, update the total price, but don't make API call
                    // Just uses the cached price if available
                    Task.Run(async () => {
                        if (!string.IsNullOrEmpty(SelectedBuyCrypto) && _priceCache.TryGetValue(SelectedBuyCrypto, out var cachedPrice))
                        {
                            TotalPrice = cachedPrice.price * value;
                            Console.WriteLine($"[DEBUG] Updated price using cached value: ${TotalPrice:N2}");
                        }
                        else
                        {
                            // Only call the full price calculation if we don't have a cached price
                            await Task.Delay(500); // Added small delay for debouncing
                            CalculateTotalPrice();
                        }
                    });
                }
            }
        }
        
        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        private string _selectedCryptoPrice = "0.00";
        public string SelectedCryptoPrice
        {
            get => _selectedCryptoPrice;
            set => SetProperty(ref _selectedCryptoPrice, value);
        }

        public ICommand BuyCryptoCommand { get; }
        public ICommand RefreshPortfolioCommand { get; }
        public ICommand CryptoSelectionChangedCommand { get; }

        // Add price cache and throttling
        private Dictionary<string, (decimal price, DateTime timestamp)> _priceCache = new();
        private Dictionary<string, DateTime> _lastRequestTime = new();
        private const int MIN_REQUEST_INTERVAL_MS = 2000; // Minimum of 2 seconds between requests for same crypto

        public PortfolioViewModel(CryptoService cryptoService, IDatabaseService databaseService, IAuthService authService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            BuyCryptoCommand = new AsyncRelayCommand(BuyCrypto);
            RefreshPortfolioCommand = new AsyncRelayCommand(RefreshPortfolioValue);
            CryptoSelectionChangedCommand = new AsyncRelayCommand(CryptoSelectionChanged);

            _currentUser = AuthService.GetCurrentUser();
            if (_currentUser == null)
            {
                Console.WriteLine("[ERROR] No user logged in!");
                return;
            }

            Task.Run(LoadUserBalance);
            Task.Run(RefreshPortfolioValue);
        }

        private async Task LoadUserBalance()
        {
            if (_currentUser == null) return;

            Console.WriteLine($"[DEBUG] Loading cash balance for User ID: {_currentUser.Id}");
            UserBalance = await _databaseService.GetUserBalanceAsync(_currentUser.Id);
            Console.WriteLine($"[DEBUG] Loaded available balance: {UserBalance}");
        }

        private async Task BuyCrypto()
        {
            if (string.IsNullOrEmpty(SelectedBuyCrypto) || BuyAmount <= 0) 
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a cryptocurrency and enter an amount to buy.", "OK");
                return;
            }
            
            if (_currentUser == null) 
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You must be logged in to buy crypto.", "OK");
                return;
            }

            try
            {
                // Get price using the cache system
                var latestPrice = await GetCachedPrice(SelectedBuyCrypto);
                if (latestPrice <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Could not get current price. Please try again.", "OK");
                    return;
                }

                decimal totalCost = latestPrice * BuyAmount;
                
                // Double-check if the total cost matches our calculated price
                if (Math.Abs(totalCost - TotalPrice) > 0.01m)
                {
                    // Price has changed since calculation, update the total price
                    TotalPrice = totalCost;
                }
                
                if (_currentUser.Balance < totalCost)
                {
                    await Application.Current.MainPage.DisplayAlert("Insufficient Funds", 
                        $"You need ${totalCost:N2} to buy {BuyAmount} {SelectedBuyCrypto}, but your balance is only ${_currentUser.Balance:N2}.", 
                        "OK");
                    return;
                }

                // Ask for confirmation
                bool confirmed = await Application.Current.MainPage.DisplayAlert("Confirm Purchase", 
                    $"Buy {BuyAmount} {SelectedBuyCrypto} for ${totalCost:N2}?", 
                    "Confirm", "Cancel");
                    
                if (!confirmed) return;

                // Deduct from User Cash Balance
                _currentUser.Balance -= totalCost;
                await _databaseService.UpdateUserBalanceAsync(_currentUser.Id, _currentUser.Balance);

                await _databaseService.UpdateCryptoBalanceAsync(_currentUser.Id, SelectedBuyCrypto, BuyAmount);

                // Store Transaction Record
                var transaction = new Transaction
                {
                    UserId = _currentUser.Id,
                    CryptoSymbol = SelectedBuyCrypto,
                    Amount = BuyAmount,
                    PriceAtTransaction = latestPrice,
                    TotalValue = totalCost,
                    TransactionType = "BUY",
                    TransactionDate = DateTime.UtcNow
                };
                await _databaseService.AddTransactionAsync(transaction);

                Console.WriteLine($"[DEBUG] Bought {BuyAmount} {SelectedBuyCrypto} at ${latestPrice}");

                //  Load Updated Balances & Portfolio
                await LoadUserBalance();
                await RefreshPortfolioValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to buy crypto: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred while buying crypto.", "OK");
            }
        }

        public async Task RefreshPortfolioValue()
        {
            try
            {
                Console.WriteLine("[DEBUG] Refreshing portfolio value...");

                Console.WriteLine("[DEBUG] Fetching latest crypto prices...");
                var (apiSuccess, latestPrices) = await _cryptoService.FetchLivePrices();

                List<Transaction> transactions;

                if (!apiSuccess || latestPrices.Count == 0) // API FAILED
                {
                    Console.WriteLine("[ERROR] Failed to fetch live prices! Using last stored values.");
                    await App.Current.MainPage.DisplayAlert("Error", "Failed to fetch live prices. Using last stored portfolio value.", "OK");

                    // Load last stored values if API fails
                    var storedTransactions = await _databaseService.GetUserTransactionsAsync(_currentUser.Id);
    
                    if (storedTransactions.Count > 0)
                    {
                        TotalPortfolioValue = storedTransactions.Sum(t => t.FinalTotalValue);
                    }
                    else
                    {
                        TotalPortfolioValue = 0; // Fallback in case no transactions exist
                    }

                    Console.WriteLine($"[DEBUG] Loaded stored Total Portfolio Value: {TotalPortfolioValue}");

                    OnPropertyChanged(nameof(TotalPortfolioValue));
                    return;
                }


                Console.WriteLine("[DEBUG] Successfully fetched latest prices.");
                transactions = await _databaseService.GetUserTransactionsAsync(_currentUser.Id);
                if (transactions.Count == 0) return;

                var transactionsToUpdate = new List<Transaction>();
                decimal totalFinalValue = 0;
                decimal totalInitialValue = 0;
                decimal weightedPercentageChange = 0;

                // Map ticker symbols to CoinGecko API names
                var symbolToApiName = new Dictionary<string, string>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "SOL", "solana" }
                };
                
                // Update our price cache with the latest API data
                foreach (var symbol in symbolToApiName)
                {
                    if (latestPrices.TryGetValue(symbol.Value, out decimal price))
                    {
                        // Update the cache with latest prices from the bulk API call
                        _priceCache[symbol.Key] = (price, DateTime.UtcNow);
                        Console.WriteLine($"[DEBUG] Updated price cache for {symbol.Key}: ${price:N2}");
                    }
                }

                foreach (var transaction in transactions)
                {
                    if (transaction.TransactionType != "BUY") continue;

                    string cryptoKey = transaction.CryptoSymbol.ToUpper();

                    Console.WriteLine($"[DEBUG] Processing transaction: {cryptoKey}, Amount: {transaction.Amount}, Buy Price: {transaction.PriceAtTransaction}");

                    if (symbolToApiName.TryGetValue(cryptoKey, out string apiKey) && latestPrices.TryGetValue(apiKey, out var latestPrice))
                    {
                        Console.WriteLine($"[DEBUG] Latest {cryptoKey} price: {latestPrice}");

                        transaction.FinalTotalValue = transaction.Amount * latestPrice;
                        totalFinalValue += transaction.FinalTotalValue;
                        totalInitialValue += transaction.TotalValue;

                        transaction.PercentageChange = ((transaction.FinalTotalValue - transaction.TotalValue) / transaction.TotalValue) * 100;
                        weightedPercentageChange += transaction.PercentageChange * transaction.TotalValue;

                        transactionsToUpdate.Add(transaction);
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] No latest price found for {cryptoKey}");
                    }
                }

                // Store Weighted % Change in UI
                PortfolioChange = totalInitialValue > 0 ? weightedPercentageChange / totalInitialValue : 0;

                // Set Total Portfolio Value (SUM of final_total_value from transactions)
                TotalPortfolioValue = totalFinalValue;

                Console.WriteLine($"[DEBUG] Updated Total Portfolio Value: {TotalPortfolioValue}");
                Console.WriteLine($"[DEBUG] Weighted Portfolio Change: {PortfolioChange}%");

                // Save final_total_value & percentage_change in the DB
                await _databaseService.UpdateTransactionFinalValues(_currentUser.Id, transactionsToUpdate);

                OnPropertyChanged(nameof(TotalPortfolioValue));
                OnPropertyChanged(nameof(PortfolioChange));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to refresh portfolio value: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred while refreshing your portfolio.", "OK");
            }
        }

        private async Task CryptoSelectionChanged()
        {
            if (string.IsNullOrEmpty(SelectedBuyCrypto))
                return;
                
            try
            {
                // Get the price, preferring cache when available
                decimal latestPrice = await GetCachedPrice(SelectedBuyCrypto);
                SelectedCryptoPrice = $"Current Price: ${latestPrice:N2}";
                
                // Recalculate total price with new crypto selection
                CalculateTotalPrice();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get price for {SelectedBuyCrypto}: {ex.Message}");
                SelectedCryptoPrice = "Price unavailable";
                TotalPrice = 0;
            }
        }
        
        private async void CalculateTotalPrice()
        {
            if (string.IsNullOrEmpty(SelectedBuyCrypto) || BuyAmount <= 0)
            {
                TotalPrice = 0;
                return;
            }
            
            try
            {
                // Get the price, preferring cache when available
                decimal latestPrice = await GetCachedPrice(SelectedBuyCrypto);
                TotalPrice = latestPrice * BuyAmount;
                Console.WriteLine($"[DEBUG] Calculated total price: ${TotalPrice:N2} for {BuyAmount} {SelectedBuyCrypto} at ${latestPrice:N2} each");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to calculate total price: {ex.Message}");
                TotalPrice = 0;
            }
        }
        
        // Add method for cached price retrieval with throttling
        private async Task<decimal> GetCachedPrice(string symbol)
        {
            // Check if we have a recent price in cache
            if (_priceCache.TryGetValue(symbol, out var cachedData))
            {
                // If cache is less than 30 seconds old, use it
                if ((DateTime.UtcNow - cachedData.timestamp).TotalSeconds < 30)
                {
                    Console.WriteLine($"[DEBUG] Using cached price for {symbol}: ${cachedData.price:N2} (cached {(DateTime.UtcNow - cachedData.timestamp).TotalSeconds:N0}s ago)");
                    return cachedData.price;
                }
            }
            
            // Check if we need to throttle requests
            if (_lastRequestTime.TryGetValue(symbol, out var lastRequest))
            {
                var timeSinceLastRequest = (DateTime.UtcNow - lastRequest).TotalMilliseconds;
                if (timeSinceLastRequest < MIN_REQUEST_INTERVAL_MS)
                {
                    // Wait until we can make another request if we have a cached price
                    if (_priceCache.ContainsKey(symbol))
                    {
                        Console.WriteLine($"[DEBUG] Throttling requests for {symbol}, using cached price");
                        return _priceCache[symbol].price;
                    }
                    
                    // If no cache exists, wait for the remaining time
                    var delayTime = (int)(MIN_REQUEST_INTERVAL_MS - timeSinceLastRequest);
                    Console.WriteLine($"[DEBUG] Throttling requests for {symbol}, waiting {delayTime}ms");
                    await Task.Delay(delayTime);
                }
            }
            
            // Update the last request time
            _lastRequestTime[symbol] = DateTime.UtcNow;
            
            // Get fresh price
            decimal price = await _cryptoService.GetLatestPrice(symbol);
            
            // Cache the result
            _priceCache[symbol] = (price, DateTime.UtcNow);
            
            Console.WriteLine($"[DEBUG] Fetched fresh price for {symbol}: ${price:N2}");
            return price;
        }
    }
}
