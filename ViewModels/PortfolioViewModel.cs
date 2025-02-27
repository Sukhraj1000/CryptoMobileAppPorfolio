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
        public string SelectedSellCrypto { get; set; }
        public decimal SellAmount { get; set; }

        public ICommand BuyCryptoCommand { get; }
        public ICommand SellCryptoCommand { get; }

        public PortfolioViewModel(CryptoService cryptoService, IDatabaseService databaseService)
        {
            _cryptoService = cryptoService;
            _databaseService = databaseService;
            BuyCryptoCommand = new AsyncRelayCommand(BuyCrypto);
            SellCryptoCommand = new AsyncRelayCommand(SellCrypto);

            _currentUser = AuthService.GetCurrentUser();
            if (_currentUser == null)
            {
                Console.WriteLine("[ERROR] No user logged in!");
                return;
            }

            Task.Run(LoadPortfolioData);
        }

        private async Task BuyCrypto()
        {
            if (string.IsNullOrEmpty(SelectedBuyCrypto) || BuyAmount <= 0) return;
            if (_currentUser == null) return;

            var latestPrice = await _cryptoService.GetLatestPrice(SelectedBuyCrypto);
            if (latestPrice <= 0) return;

            decimal totalCost = latestPrice * BuyAmount;
            if (_currentUser.Balance < totalCost)
            {
                Console.WriteLine("[ERROR] Insufficient funds.");
                return;
            }

            _currentUser.Balance -= totalCost;
            await _databaseService.UpdateUserBalanceAsync(_currentUser.Id, _currentUser.Balance);

            var transaction = new Transaction
            {
                UserId = _currentUser.Id,
                Symbol = SelectedBuyCrypto,
                Amount = BuyAmount,
                Currency = "USD",
            };

            await _databaseService.AddTransactionAsync(transaction);
            await LoadPortfolioData();
        }

        private async Task SellCrypto()
        {
            if (string.IsNullOrEmpty(SelectedSellCrypto) || SellAmount <= 0) return;
            if (_currentUser == null) return;

            var holding = PortfolioHoldings.FirstOrDefault(h => h.Symbol == SelectedSellCrypto);
            if (holding == null || holding.Amount < SellAmount)
            {
                Console.WriteLine("[ERROR] Not enough holdings to sell.");
                return;
            }

            var latestPrice = await _cryptoService.GetLatestPrice(SelectedSellCrypto);
            if (latestPrice <= 0) return;

            decimal totalRevenue = latestPrice * SellAmount;
            _currentUser.Balance += totalRevenue;
            await _databaseService.UpdateUserBalanceAsync(_currentUser.Id, _currentUser.Balance);

            holding.Amount -= SellAmount;
            if (holding.Amount == 0)
            {
                await _databaseService.DeleteTransactionAsync(holding.Id);
                PortfolioHoldings.Remove(holding);
            }
            else
            {
                await _databaseService.UpdateTransactionAsync(holding);
            }

            await LoadPortfolioData();
        }

        private async Task LoadPortfolioData()
        {
            var holdings = await _databaseService.GetAllHoldingsAsync();
            if (holdings == null || holdings.Count == 0) return;

            PortfolioHoldings.Clear();
            foreach (var holding in holdings)
            {
                var latestPrice = await _cryptoService.GetLatestPrice(holding.Symbol);
                if (latestPrice > 0)
                {
                    TotalPortfolioValue += latestPrice * holding.Amount;
                }
                PortfolioHoldings.Add(holding);
            }

            PortfolioChange = 0;
        }
    }
}
