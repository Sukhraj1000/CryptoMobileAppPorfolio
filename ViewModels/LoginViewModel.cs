
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;


namespace CryptoApp.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            LoginCommand = new AsyncRelayCommand(ExecuteLogin);
        }

        private async Task ExecuteLogin()
        {
            Console.WriteLine($"[DEBUG] Username: '{Username}', Password Length: {Password?.Length}");

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await ShowAlert("Error", "Please enter username and password.");
                return;
            }
    
            IsBusy = true;
            try
            {
                bool success = await _authService.LoginAsync(Username, Password);
                if (success)
                {
                    await ShowAlert("Success", "Login successful!");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Application.Current.MainPage = new AppShell();
                    });
                }
                else
                {
                    await ShowAlert("Error", "Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Login Error: {ex.Message}");
                await ShowAlert("Error", "An unexpected error occurred. Please try again.");
            }
            finally
            {
                IsBusy = false;
            }
        }


        private Task ShowAlert(string title, string message)
        {
            return Application.Current?.MainPage?.DisplayAlert(title, message, "OK") ?? Task.CompletedTask;
        }
    }
}
