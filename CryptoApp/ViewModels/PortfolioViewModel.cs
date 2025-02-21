using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;

namespace CryptoApp.ViewModels;  

public class PortfolioViewModel : BaseViewModel
{
    private readonly IDatabaseService? _databaseService; 
    public ObservableCollection<Transaction> Transactions { get; set; } = new();
    
    public ICommand AddTransactionCommand { get; }
    public ICommand DeleteTransactionCommand { get; }

    public PortfolioViewModel()
    {
        Transactions = new ObservableCollection<Transaction>();
        AddTransactionCommand = new AsyncRelayCommand<Transaction>(AddTransactionAsync);
        DeleteTransactionCommand = new AsyncRelayCommand<Transaction>(DeleteTransactionAsync);
    }
    
    public PortfolioViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        AddTransactionCommand = new AsyncRelayCommand<Transaction>(AddTransactionAsync);
        DeleteTransactionCommand = new AsyncRelayCommand<Transaction>(DeleteTransactionAsync);
        LoadTransactions();
    }

    private async void LoadTransactions()
    {
        if (_databaseService == null) return; 
        var transactions = await _databaseService.GetTransactionsAsync();
        Transactions.Clear();
        foreach (var transaction in transactions)
            Transactions.Add(transaction);
    }

    private async Task AddTransactionAsync(Transaction? transaction) 
    {
        if (transaction == null || _databaseService == null) return; 
        await _databaseService.AddTransactionAsync(transaction);
        LoadTransactions();
    }

    private async Task DeleteTransactionAsync(Transaction? transaction) 
    {
        if (transaction == null || _databaseService == null) return; 
        await _databaseService.DeleteTransactionAsync(transaction);
        LoadTransactions();
    }
}
