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

        public string SelectedBuyCrypto { get; set; }
        public decimal BuyAmount { get; set; }

        public ICommand BuyCryptoCommand { get; }
        public ICommand RefreshPortfolioCommand { get; }

        public PortfolioViewModel(CryptoService cryptoService, IDatabaseService databaseService, IAuthService authService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            BuyCryptoCommand = new AsyncRelayCommand(BuyCrypto);
            RefreshPortfolioCommand = new AsyncRelayCommand(RefreshPortfolioValue);

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
            if (string.IsNullOrEmpty(SelectedBuyCrypto) || BuyAmount <= 0) return;
            if (_currentUser == null) return;

            var latestPrice = await _cryptoService.GetLatestPrice(SelectedBuyCrypto);
            if (latestPrice <= 0)
            {
                Console.WriteLine("[ERROR] Failed to fetch latest price.");
                return;
            }

            decimal totalCost = latestPrice * BuyAmount;
            if (_currentUser.Balance < totalCost)
            {
                Console.WriteLine("[ERROR] Insufficient funds.");
                return;
            }

            // Deduct from User Cash Balance
            _currentUser.Balance -= totalCost;
            await _databaseService.UpdateUserBalanceAsync(_currentUser.Id, _currentUser.Balance);

            // Update Crypto Holdings in Wallet
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

        public async Task RefreshPortfolioValue()
        {
            try
            {
                Console.WriteLine("[DEBUG] Refreshing portfolio value...");

                Console.WriteLine("[DEBUG] Fetching latest crypto prices...");
                var latestPrices = await _cryptoService.FetchLivePrices();
                if (latestPrices.Count == 0)
                {
                    Console.WriteLine("[ERROR] No live prices retrieved! API response is empty.");
                    return; 
                }
                Console.WriteLine("[DEBUG] Successfully fetched latest prices.");

                var transactions = await _databaseService.GetUserTransactionsAsync(_currentUser.Id);
                if (transactions.Count == 0) return;

                decimal totalFinalValue = 0;
                decimal totalInitialValue = 0;
                decimal weightedPercentageChange = 0;

                //  Map ticker symbols to CoinGecko API names
                var symbolToApiName = new Dictionary<string, string>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "SOL", "solana" }
                };

                foreach (var transaction in transactions)
                {
                    if (transaction.TransactionType != "BUY") continue;

                    string cryptoKey = transaction.CryptoSymbol.ToUpper();

                    Console.WriteLine($"[DEBUG] Processing transaction: {cryptoKey}, Amount: {transaction.Amount}, Buy Price: {transaction.PriceAtTransaction}");

                    if (symbolToApiName.TryGetValue(cryptoKey, out string apiKey) && latestPrices.TryGetValue(apiKey, out var latestPrice))
                    {
                        Console.WriteLine($"[DEBUG] Latest {cryptoKey} price: {latestPrice}");

                        transaction.FinalTotalValue = transaction.Amount * latestPrice;
                        totalFinalValue += transaction.FinalTotalValue.GetValueOrDefault();
                        totalInitialValue += transaction.TotalValue;

                        transaction.PercentageChange = ((transaction.FinalTotalValue.GetValueOrDefault() - transaction.TotalValue) / transaction.TotalValue) * 100;
                        weightedPercentageChange += transaction.PercentageChange.GetValueOrDefault() * transaction.TotalValue;
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] No latest price found for {cryptoKey}");
                    }
                }

                // Store Weighted % Change in UI
                PortfolioChange = totalInitialValue > 0 ? weightedPercentageChange / totalInitialValue : 0;

                //  Set Total Portfolio Value (SUM of final_total_value from transactions)
                TotalPortfolioValue = totalFinalValue;

                Console.WriteLine($"[DEBUG] Updated Total Portfolio Value: {TotalPortfolioValue}");
                Console.WriteLine($"[DEBUG] Weighted Portfolio Change: {PortfolioChange}%");

                await _databaseService.UpdateTransactionFinalValues(_currentUser.Id, transactions);

                OnPropertyChanged(nameof(TotalPortfolioValue));
                OnPropertyChanged(nameof(PortfolioChange));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to refresh portfolio value: {ex.Message}");
            }
        }








    }
}
