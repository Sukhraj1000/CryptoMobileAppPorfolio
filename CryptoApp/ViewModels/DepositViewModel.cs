using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;

namespace CryptoApp.ViewModels;

public class DepositViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    public Transaction NewTransaction { get; set; } = new();

    public ICommand AddTransactionCommand { get; }

    public DepositViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        AddTransactionCommand = new AsyncRelayCommand(AddTransactionAsync);
    }

    private async Task AddTransactionAsync()
    {
        if (string.IsNullOrEmpty(NewTransaction.CryptoName) || NewTransaction.Amount <= 0)
            return;

        await _databaseService.AddTransactionAsync(NewTransaction);
        NewTransaction = new Transaction(); 
    }
}