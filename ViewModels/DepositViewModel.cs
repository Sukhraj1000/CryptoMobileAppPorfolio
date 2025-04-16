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
    /// View model for the deposit page, managing user deposits and transaction history.
    /// </summary>
    public class DepositViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private User _currentUser;

        /// <summary>
        /// Amount to be deposited.
        /// </summary>
        private decimal _depositAmount;
        public decimal DepositAmount
        {
            get => _depositAmount;
            set => SetProperty(ref _depositAmount, value);
        }

        /// <summary>
        /// User's current balance.
        /// </summary>
        private decimal _userBalance;
        public decimal UserBalance
        {
            get => _userBalance;
            set => SetProperty(ref _userBalance, value);
        }

        /// <summary>
        /// Collection of the user's deposit transactions.
        /// </summary>
        public ObservableCollection<Deposit> UserDeposits { get; private set; } = new();

        /// <summary>
        /// Command to execute the deposit operation.
        /// </summary>
        public ICommand DepositCommand { get; }

        public DepositViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            DepositCommand = new AsyncRelayCommand(DepositFunds);
            Task.Run(LoadUserData);
        }

        private async Task LoadUserData()
        {
            _currentUser = AuthService.GetCurrentUser();
            if (_currentUser != null)
            {
                UserBalance = _currentUser.Balance;

                var deposits = await _databaseService.GetUserDepositsAsync(_currentUser.Id);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UserDeposits.Clear();
                    foreach (var deposit in deposits
                                 .OrderByDescending(d => d.DepositDate) 
                                 .Take(6)) 
                    {
                        UserDeposits.Add(deposit);
                    }
                });

                Console.WriteLine($"[DEBUG] Loaded {UserDeposits.Count} latest deposits.");
            }
            else
            {
                Console.WriteLine("[ERROR] No user is currently logged in.");
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

            if (DepositAmount <= 0)
            {
                Console.WriteLine("[ERROR] Invalid deposit amount.");
                await Application.Current.MainPage.DisplayAlert("Error", "Enter a valid amount.", "OK");
                return;
            }

            var deposit = new Deposit
            {
                UserId = user.Id,
                Amount = DepositAmount,
                DepositDate = DateTime.UtcNow
            };

            await _databaseService.AddDepositAsync(deposit);
            user.Balance += DepositAmount;
            await _databaseService.UpdateUserBalanceAsync(user.Id, user.Balance);

            await LoadUserData(); 

            Console.WriteLine("[SUCCESS] Deposit added.");
        }


    }
}
