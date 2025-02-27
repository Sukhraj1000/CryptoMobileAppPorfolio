using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using CryptoApp.Services;

namespace CryptoApp.ViewModels
{
    public class DepositViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private User _currentUser;

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
            // Get logged-in user
            _currentUser = AuthService.GetCurrentUser(); 
            if (_currentUser != null)
            {
                UserBalance = _currentUser.Balance;
                var deposits = await _databaseService.GetUserDepositsAsync(_currentUser.Id);
                UserDeposits.Clear();
                foreach (var deposit in deposits)
                {
                    UserDeposits.Add(deposit);
                }
            }
        }

        private async Task DepositFunds()
        {
            var user = AuthService.GetCurrentUser();
            if (user == null)
            {
                Console.WriteLine("[ERROR] No user is currently logged in!");
                await Application.Current.MainPage.DisplayAlert("Error", "No user logged in!", "OK");
                return;
            }

            if (DepositAmount <= 0) return;

            var deposit = new Deposit
            {
                UserId = user.Id,
                Amount = DepositAmount,
                DepositDate = DateTime.UtcNow
            };

            await _databaseService.AddDepositAsync(deposit);
            user.Balance += DepositAmount;
            await _databaseService.UpdateUserBalanceAsync(user.Id, user.Balance);
        }

    }
}
