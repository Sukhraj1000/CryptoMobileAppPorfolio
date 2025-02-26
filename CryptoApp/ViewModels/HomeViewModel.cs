using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using CryptoApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoApp.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly CryptoService _cryptoService;
    private readonly IDatabaseService _databaseService;

    public ObservableCollection<CryptoAsset> Watchlist { get; private set; } = new();
    public ObservableCollection<Transaction> PortfolioHoldings { get; private set; } = new();

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

    public ICommand RefreshCommand { get; }
    public ICommand AddSampleCommand { get; }

    public HomeViewModel(CryptoService cryptoService, IDatabaseService databaseService)
    {
        _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

        AddSampleCommand = new AsyncRelayCommand(AddSampleTransaction);
        RefreshCommand = new AsyncRelayCommand(FetchLiveCryptoPrices);

        _cryptoService.PriceUpdates.Subscribe(asset =>
        {
            var existing = Watchlist.FirstOrDefault(w => w.Symbol == asset.Symbol);
            if (existing != null)
            {
                existing.Price = asset.Price;
            }
            else
            {
                Watchlist.Add(asset);
            }

            OnPropertyChanged(nameof(Watchlist));
            UpdatePortfolioWithNewPrice(asset);
        });

        Task.Run(LoadPortfolioData);
    }

    private async Task FetchLiveCryptoPrices()
    {
        await _cryptoService.FetchLivePrices();
        await LoadPortfolioData();
    }

    private async Task LoadPortfolioData()
    {
        var holdings = await _databaseService.GetAllHoldingsAsync();
        if (holdings == null || holdings.Count == 0)
        {
            Console.WriteLine("No holdings found.");
            return;
        }

        decimal previousValue = TotalPortfolioValue;
        TotalPortfolioValue = 0;

        PortfolioHoldings.Clear();
        foreach (var holding in holdings)
        {
            var latestPrice = _cryptoService.GetLatestPrice(holding.Symbol);
            if (latestPrice > 0)
            {
                TotalPortfolioValue += latestPrice * holding.Amount;
            }

            PortfolioHoldings.Add(holding);
        }

        PortfolioChange = previousValue == 0 ? 0 : ((TotalPortfolioValue - previousValue) / Math.Max(previousValue, 1)) * 100;

        OnPropertyChanged(nameof(TotalPortfolioValue));
        OnPropertyChanged(nameof(PortfolioChange));
        OnPropertyChanged(nameof(PortfolioHoldings));
    }

    private void UpdatePortfolioWithNewPrice(CryptoAsset updatedAsset)
    {
        if (!PortfolioHoldings.Any()) return;

        decimal newTotalValue = PortfolioHoldings
            .Where(h => h.Symbol == updatedAsset.Symbol)
            .Sum(h => updatedAsset.Price * h.Amount);

        if (newTotalValue > 0)
        {
            TotalPortfolioValue = newTotalValue;
            PortfolioChange = ((TotalPortfolioValue - newTotalValue) / Math.Max(newTotalValue, 1)) * 100;
        }

        OnPropertyChanged(nameof(TotalPortfolioValue));
        OnPropertyChanged(nameof(PortfolioChange));
    }

    private async Task AddSampleTransaction()
    {
        var sampleTransaction = new Transaction
        {
            CryptoName = "Ethereum",
            Symbol = "ETH",
            Amount = 1.0m,
            BuyPrice = 3000.00m,
            Currency = "USD",
            Date = DateTime.UtcNow 
        };

        await _databaseService.AddTransactionAsync(sampleTransaction);
        await LoadPortfolioData();
    }
}
