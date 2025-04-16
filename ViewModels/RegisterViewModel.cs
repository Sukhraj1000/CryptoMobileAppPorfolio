using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.Views;

namespace CryptoApp.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty] 
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task Register()
        {
            if (IsBusy)
                return;

            // Basic validation
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match", "OK");
                return;
            }
            
            if (Password.Length < 6)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password must be at least 6 characters", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                // Register the user in Supabase via the auth service
                var (success, message) = await _authService.RegisterAsync(Username, Password);
                
                if (success)
                {
                    // Clear the form fields
                    Username = string.Empty;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;
                    
                    // Show success message
                    await Application.Current.MainPage.DisplayAlert("Success", message, "OK");
                    
                    var loginViewModel = CryptoApp.Services.Services.GetService<LoginViewModel>() ?? 
                        new LoginViewModel(_authService);
                        
                    var loginPage = new LoginPage(loginViewModel);
                    
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        Application.Current.MainPage = loginPage;
                    });
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Registration Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred during registration. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToLogin()
        {
            // Get properly initialized LoginViewModel from service locator
            var loginViewModel = CryptoApp.Services.Services.GetService<LoginViewModel>() ?? 
                new LoginViewModel(_authService);
                
            var loginPage = new LoginPage(loginViewModel);
            
            // Direct navigation to login page
            await MainThread.InvokeOnMainThreadAsync(() => 
            {
                Application.Current.MainPage = loginPage;
            });
        }
    }
} 