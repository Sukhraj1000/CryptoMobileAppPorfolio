using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.Views;

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
        public ICommand GoToRegisterCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            LoginCommand = new AsyncRelayCommand(ExecuteLogin);
            GoToRegisterCommand = new AsyncRelayCommand(GoToRegister);
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
                    Console.WriteLine("Login successful, navigating to tabbed interface");
                    
                    try 
                    {
                        // Create our custom tabbed container instead of HomePage
                        var mainContainer = new Views.MainContainer();
                        
                        // Set the MainContainer as MainPage
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            Application.Current.MainPage = mainContainer;
                        });
                    }
                    catch (Exception navEx)
                    {
                        Console.WriteLine($"Navigation error: {navEx.Message}");
                        Console.WriteLine($"Navigation stack trace: {navEx.StackTrace}");
                        await ShowAlert("Error", "Navigation failed. Please try again.");
                    }
                    
                    // Clear login info for security
                    Username = string.Empty;
                    Password = string.Empty;
                }
                else
                {
                    await ShowAlert("Error", "Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login Error: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                await ShowAlert("Error", "An unexpected error occurred. Please try again.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoToRegister()
        {
            // Use service locator to get RegisterViewModel
            var registerViewModel = CryptoApp.Services.Services.GetService<RegisterViewModel>() ?? 
                new RegisterViewModel(_authService);
                
            // Create RegisterPage with properly initialized ViewModel
            var registerPage = new RegisterPage(registerViewModel);
            
            // Set as MainPage
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current.MainPage = registerPage;
            });
        }

        private Task ShowAlert(string title, string message)
        {
            return Application.Current?.MainPage?.DisplayAlert(title, message, "OK") ?? Task.CompletedTask;
        }
    }
}
