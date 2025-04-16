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
using Microsoft.Maui.ApplicationModel;

namespace CryptoApp.ViewModels
{
    /// <summary>
    /// View model for the portfolio page, managing user's cryptocurrency holdings and transactions.
    /// </summary>
    public class PortfolioViewModel : ObservableObject
    {
        private readonly CryptoService _cryptoService;
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private User _currentUser;

        /// <summary>
        /// Collection of the user's cryptocurrency holdings.
        /// </summary>
        public ObservableCollection<Transaction> PortfolioHoldings { get; private set; } = new();

        /// <summary>
        /// Available cryptocurrencies for purchase.
        /// </summary>
        public ObservableCollection<string> AvailableCryptos { get; } = new() { "BTC", "ETH", "SOL" };

        /// <summary>
        /// Total value of the user's portfolio.
        /// </summary>
        private decimal _totalPortfolioValue;
        public decimal TotalPortfolioValue
        {
            get => Preferences.Default.Get("ShowAccountValue", true) ? _totalPortfolioValue : 0;
            set => SetProperty(ref _totalPortfolioValue, value);
        }
        
        /// <summary>
        /// Formatted display string for the total portfolio value.
        /// </summary>
        public string TotalPortfolioValueDisplay => 
            Preferences.Default.Get("ShowAccountValue", true) 
                ? $"${TotalPortfolioValue:N2}" 
                : "Portfolio Hidden";

        /// <summary>
        /// Percentage change in portfolio value.
        /// </summary>
        private decimal _portfolioChange;
        public decimal PortfolioChange
        {
            get => _portfolioChange;
            set => SetProperty(ref _portfolioChange, value);
        }

        /// <summary>
        /// User's available balance.
        /// </summary>
        private decimal _userBalance = 0;
        public decimal UserBalance
        {
            get => Preferences.Default.Get("ShowAccountValue", true) ? _userBalance : 0;
            set => SetProperty(ref _userBalance, value);
        }
        
        /// <summary>
        /// Formatted display string for the user's balance.
        /// </summary>
        public string UserBalanceDisplay => 
            Preferences.Default.Get("ShowAccountValue", true) 
                ? $"${UserBalance:N2}" 
                : "Balance Hidden";

        private string _selectedBuyCrypto;
        public string SelectedBuyCrypto 
        { 
            get => _selectedBuyCrypto;
            set 
            {
                if (SetProperty(ref _selectedBuyCrypto, value) && !string.IsNullOrEmpty(value))
                {
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
                            await Task.Delay(500);
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

        private string _selectedCryptoPrice = "Select a cryptocurrency";
        public string SelectedCryptoPrice
        {
            get => _selectedCryptoPrice;
            set => SetProperty(ref _selectedCryptoPrice, value);
        }

        public ICommand BuyCryptoCommand { get; }
        public ICommand RefreshPortfolioCommand { get; }
        public ICommand CryptoSelectionChangedCommand { get; }

        // Added price cache and throttling
        private Dictionary<string, (decimal price, DateTime timestamp)> _priceCache = new();
        private Dictionary<string, DateTime> _lastRequestTime = new();
        private const int MIN_REQUEST_INTERVAL_MS = 2000; 

        private IDispatcherTimer _prefsCheckTimer;
        
        public PortfolioViewModel(CryptoService cryptoService, IDatabaseService databaseService, IAuthService authService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Initialize default values
            _selectedCryptoPrice = "Select a cryptocurrency";
            _totalPrice = 0;
            _buyAmount = 0;
            _selectedBuyCrypto = null;

            BuyCryptoCommand = new AsyncRelayCommand(BuyCrypto);
            RefreshPortfolioCommand = new AsyncRelayCommand(RefreshPortfolioValue);
            CryptoSelectionChangedCommand = new AsyncRelayCommand(CryptoSelectionChanged);

            _currentUser = AuthService.GetCurrentUser();
            if (_currentUser == null)
            {
                Console.WriteLine("[ERROR] No user logged in!");
                return;
            }
            
            _prefsCheckTimer = Application.Current.Dispatcher.CreateTimer();
            _prefsCheckTimer.Interval = TimeSpan.FromMilliseconds(500);
            _prefsCheckTimer.Tick += (s, e) => CheckPreferenceChanges();
            _prefsCheckTimer.Start();

            // Initialise the last known value
            _lastShowAccountValue = Preferences.Default.Get("ShowAccountValue", true);
            
            MessagingCenter.Subscribe<SettingsViewModel, bool>(this, "AccountValueVisibilityChanged", (sender, showValue) => {
                _lastShowAccountValue = showValue;
                Application.Current.Dispatcher.Dispatch(() => {
                    // Force UI refresh for affected properties
                    OnPropertyChanged(nameof(UserBalance));
                    OnPropertyChanged(nameof(TotalPortfolioValue));
                    OnPropertyChanged(nameof(UserBalanceDisplay));
                    OnPropertyChanged(nameof(TotalPortfolioValueDisplay));
                    
                    Console.WriteLine($"[DEBUG] Account value visibility changed via message to: {(showValue ? "Visible" : "Hidden")}");
                });
            });

            // Initialise ViewModel and UI asynchronously
            InitializeAsync();
        }
        
        private async void InitializeAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] Initializing PortfolioViewModel...");
                
                // First load stored portfolio data which should be instant
                await LoadStoredPortfolioData();
                
                // Then load user balance which is fast
                await LoadUserBalance();
                
                // Trigger a load of crypto prices - this updates the UI
                await FetchCryptoPrices(ignoreRateLimits: true);
                
                // Only after prices are loaded, calculates total portfolio value with live prices
                await RefreshPortfolioValue();
                
                _selectedBuyCrypto = null;
                
                Console.WriteLine("[DEBUG] PortfolioViewModel initialization complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to initialize PortfolioViewModel: {ex.Message}");
            }
        }
        
        private async Task LoadStoredPortfolioData()
        {
            if (_currentUser == null) return;
            
            try
            {
                Console.WriteLine("[DEBUG] Loading stored portfolio data...");
                
                // Load last stored transactions
                var storedTransactions = await _databaseService.GetUserTransactionsAsync(_currentUser.Id);
                
                if (storedTransactions.Count > 0)
                {
                    // Use stored total value
                    TotalPortfolioValue = storedTransactions.Sum(t => t.FinalTotalValue);
                    
                    // Use stored percentage change if available
                    if (storedTransactions.Any(t => t.PercentageChange != 0))
                    {
                        decimal totalInitialValue = storedTransactions.Sum(t => t.TotalValue);
                        decimal weightedPercentageChange = 0;
                        
                        foreach (var transaction in storedTransactions)
                        {
                            weightedPercentageChange += transaction.PercentageChange * transaction.TotalValue;
                        }
                        
                        PortfolioChange = totalInitialValue > 0 ? weightedPercentageChange / totalInitialValue : 0;
                        Console.WriteLine($"[DEBUG] Loaded stored Portfolio Change: {PortfolioChange:N2}%");
                    }
                    
                    // Update the UI with stored data
                    OnPropertyChanged(nameof(TotalPortfolioValue));
                    OnPropertyChanged(nameof(TotalPortfolioValueDisplay));
                    OnPropertyChanged(nameof(PortfolioChange));
                }
                else
                {
                    TotalPortfolioValue = 0;
                    PortfolioChange = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load stored portfolio data: {ex.Message}");
            }
        }

        private async Task LoadUserBalance()
        {
            if (_currentUser == null) return;

            try {
                Console.WriteLine($"[DEBUG] Loading cash balance for User ID: {_currentUser.Id}");
                UserBalance = await _databaseService.GetUserBalanceAsync(_currentUser.Id);
                Console.WriteLine($"[DEBUG] Loaded available balance: ${UserBalance:N2}");
                
                OnPropertyChanged(nameof(UserBalance));
                OnPropertyChanged(nameof(UserBalanceDisplay));
            }
            catch (Exception ex) {
                Console.WriteLine($"[ERROR] Failed to load user balance: {ex.Message}");
            }
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
                
                // First load stored data to show something immediately
                await LoadStoredPortfolioData();
                
                // Then fetch fresh prices to update the view
                await FetchCryptoPrices(ignoreRateLimits: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to refresh portfolio value: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred while refreshing your portfolio.", "OK");
            }
        }
        
        private async Task FetchCryptoPrices(bool ignoreRateLimits = false)
        {
            try
            {
                Console.WriteLine("[DEBUG] Fetching latest crypto prices...");
                
                // Fetch all prices in one API call
                var (apiSuccess, latestPrices) = await _cryptoService.FetchLivePrices();
                
                if (!apiSuccess || latestPrices.Count == 0)
                {
                    Console.WriteLine("[ERROR] Failed to fetch live prices!");
                    return;
                }
                
                Console.WriteLine("[DEBUG] Successfully fetched latest prices.");
                
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
                
                // Update transactions with new prices
                var transactions = await _databaseService.GetUserTransactionsAsync(_currentUser.Id);
                if (transactions.Count == 0) return;
                
                var transactionsToUpdate = new List<Transaction>();
                decimal totalFinalValue = 0;
                decimal totalInitialValue = 0;
                decimal weightedPercentageChange = 0;
                
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
                
                // Update Portfolio Value and Change
                TotalPortfolioValue = totalFinalValue;
                PortfolioChange = totalInitialValue > 0 ? weightedPercentageChange / totalInitialValue : 0;
                
                Console.WriteLine($"[DEBUG] Updated Total Portfolio Value: ${TotalPortfolioValue:N2}");
                Console.WriteLine($"[DEBUG] Weighted Portfolio Change: {PortfolioChange:N2}%");
                
                // Save final_total_value & percentage_change in the DB
                await _databaseService.UpdateTransactionFinalValues(_currentUser.Id, transactionsToUpdate);
                
                // Update the UI
                OnPropertyChanged(nameof(TotalPortfolioValue));
                OnPropertyChanged(nameof(TotalPortfolioValueDisplay));
                OnPropertyChanged(nameof(PortfolioChange));
                
                // Only update the UI price if a cryptocurrency is explicitly selected by the user
                if (!string.IsNullOrEmpty(SelectedBuyCrypto) && _priceCache.TryGetValue(SelectedBuyCrypto, out var priceData))
                {
                    SelectedCryptoPrice = $"Current Price: ${priceData.price:N2}";
                    TotalPrice = priceData.price * BuyAmount;
                    
                    OnPropertyChanged(nameof(SelectedCryptoPrice));
                    OnPropertyChanged(nameof(TotalPrice));
                    
                    Console.WriteLine($"[DEBUG] Updated price for {SelectedBuyCrypto}: {SelectedCryptoPrice}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch crypto prices: {ex.Message}");
            }
        }

        private async Task CryptoSelectionChanged()
        {
            if (string.IsNullOrEmpty(SelectedBuyCrypto))
            {
                Console.WriteLine("[WARNING] CryptoSelectionChanged called with null or empty selection");
                return;
            }
                
            try
            {
                Console.WriteLine($"[DEBUG] Crypto selection changed to {SelectedBuyCrypto}");
                
                // First, show loading state
                SelectedCryptoPrice = "Loading price...";
                
                // Check if we have the price in cache
                if (_priceCache.TryGetValue(SelectedBuyCrypto, out var priceData))
                {
                    // Use cached price data
                    decimal price = priceData.price;
                    SelectedCryptoPrice = $"Current Price: ${price:N2}";
                    TotalPrice = price * BuyAmount;
                    
                    Console.WriteLine($"[DEBUG] Using cached price for {SelectedBuyCrypto}: ${price:N2}");
                    
                    // Update UI
                    OnPropertyChanged(nameof(SelectedCryptoPrice));
                    OnPropertyChanged(nameof(TotalPrice));
                }
                else
                {
                    // No cached price available, don't make API call - just show unavailable 
                    SelectedCryptoPrice = "Price data not loaded";
                    TotalPrice = 0;
                    
                    Console.WriteLine($"[WARNING] No cached price for {SelectedBuyCrypto}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process crypto selection change: {ex.Message}");
                SelectedCryptoPrice = "Error getting price";
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
            try
            {
                if (_priceCache.TryGetValue(symbol, out var cachedData))
                {
                    if ((DateTime.UtcNow - cachedData.timestamp).TotalSeconds < 30)
                    {
                        Console.WriteLine($"[DEBUG] Using cached price for {symbol}: ${cachedData.price:N2} (cached {(DateTime.UtcNow - cachedData.timestamp).TotalSeconds:N0}s ago)");
                        return cachedData.price;
                    }
                }
                
                if (_lastRequestTime.TryGetValue(symbol, out var lastRequest))
                {
                    var timeSinceLastRequest = (DateTime.UtcNow - lastRequest).TotalMilliseconds;
                    if (timeSinceLastRequest < MIN_REQUEST_INTERVAL_MS)
                    {
                        if (_priceCache.ContainsKey(symbol))
                        {
                            Console.WriteLine($"[DEBUG] Throttling requests for {symbol}, using cached price");
                            return _priceCache[symbol].price;
                        }
                        
                        var delayTime = (int)(MIN_REQUEST_INTERVAL_MS - timeSinceLastRequest);
                        Console.WriteLine($"[DEBUG] Throttling requests for {symbol}, waiting {delayTime}ms");
                        await Task.Delay(delayTime);
                    }
                }
                
                _lastRequestTime[symbol] = DateTime.UtcNow;
                
                Console.WriteLine($"[DEBUG] Fetching fresh price for {symbol}");
                decimal price = await _cryptoService.GetCurrentPrice(symbol);
                
                if (price > 0)
                {
                    _priceCache[symbol] = (price, DateTime.UtcNow);
                    Console.WriteLine($"[DEBUG] Fetched fresh price for {symbol}: ${price:N2}");
                }
                else if (_priceCache.ContainsKey(symbol))
                {
                    Console.WriteLine($"[DEBUG] API returned invalid price (${price}), falling back to cache for {symbol}");
                    price = _priceCache[symbol].price;
                }
                else
                {
                    Console.WriteLine($"[ERROR] Could not fetch price for {symbol} and no cache available");
                }
                
                return price;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in GetCachedPrice for {symbol}: {ex.Message}");
                
                if (_priceCache.ContainsKey(symbol))
                {
                    Console.WriteLine($"[DEBUG] Using cached price as fallback after error for {symbol}");
                    return _priceCache[symbol].price;
                }
                
                return 0;
            }
        }

        private bool _lastShowAccountValue = true;
        
        private void CheckPreferenceChanges()
        {
            try
            {
                bool currentValue = Preferences.Default.Get("ShowAccountValue", true);
                
                if (currentValue != _lastShowAccountValue)
                {
                    _lastShowAccountValue = currentValue;
                    
                    OnPropertyChanged(nameof(UserBalance));
                    OnPropertyChanged(nameof(TotalPortfolioValue));
                    OnPropertyChanged(nameof(UserBalanceDisplay));
                    OnPropertyChanged(nameof(TotalPortfolioValueDisplay));
                    
                    Console.WriteLine($"[DEBUG] Account value visibility changed to: {(currentValue ? "Visible" : "Hidden")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error checking preference changes: {ex.Message}");
            }
        }

        ~PortfolioViewModel()
        {
            Cleanup();
        }
        
        private void Cleanup()
        {
            try
            {
                if (_prefsCheckTimer != null)
                {
                    _prefsCheckTimer.Stop();
                    _prefsCheckTimer = null;
                }
                
                MessagingCenter.Unsubscribe<SettingsViewModel, bool>(this, "AccountValueVisibilityChanged");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error during cleanup: {ex.Message}");
            }
        }
    }
}
