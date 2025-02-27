using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;

namespace CryptoApp.ViewModels
{
    public class DepositViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private Guid _userId = Guid.NewGuid(); // Replace with actual user ID logic

        private decimal _depositAmount;
        public decimal DepositAmount
        {
            get => _depositAmount;
            set => SetProperty(ref _depositAmount, value);
        }

        private decimal _userBalance;
        public decimal UserBalance
        {
            get => _userBalance;
            set => SetProperty(ref _userBalance, value);
        }

        public ObservableCollection<Deposit> UserDeposits { get; private set; } = new();
        public ICommand DepositCommand { get; }

        public DepositViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            DepositCommand = new AsyncRelayCommand(DepositFunds);
            Task.Run(LoadUserData);
        }

        private async Task LoadUserData()
        {
            var user = await _databaseService.GetUserByIdAsync(_userId);
            if (user != null)
            {
                UserBalance = user.Balance;
                var deposits = await _databaseService.GetUserDepositsAsync(_userId);
                UserDeposits.Clear();
                foreach (var deposit in deposits)
                {
                    UserDeposits.Add(deposit);
                }
            }
        }

        private async Task DepositFunds()
        {
            if (DepositAmount <= 0) return;

            var user = await _databaseService.GetUserByIdAsync(_userId);
            if (user == null)
            {
                Console.WriteLine("User does not exist. Cannot deposit.");
                return;
            }

            decimal newBalance = user.Balance + DepositAmount;
            await _databaseService.UpdateUserBalanceAsync(_userId, newBalance);

            var deposit = new Deposit
            {
                UserId = _userId,
                Amount = DepositAmount,
                Date = DateTime.UtcNow
            };

            await _databaseService.AddDepositAsync(deposit);
            UserDeposits.Add(deposit);
            UserBalance = newBalance;
        }
    }
}
