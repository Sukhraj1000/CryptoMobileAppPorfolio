using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;

namespace CryptoApp.ViewModels;

public class PortfolioViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    public ObservableCollection<Transaction> Transactions { get; set; } = new();

    public ICommand DeleteTransactionCommand { get; }

    public PortfolioViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        DeleteTransactionCommand = new AsyncRelayCommand<Transaction>(DeleteTransactionAsync);
        LoadTransactions();
    }

    private async void LoadTransactions()
    {
        var holdings = await _databaseService.GetAllHoldingsAsync();
        Transactions.Clear();
        foreach (var holding in holdings)
            Transactions.Add(holding);
    }

    private async Task DeleteTransactionAsync(Transaction transaction)
    {
        await _databaseService.DeleteTransactionAsync(transaction);
        LoadTransactions();
    }
}