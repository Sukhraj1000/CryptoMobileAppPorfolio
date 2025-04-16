using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoApp.Interfaces;
using CryptoApp.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace CryptoApp.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IAuthService _authService;
    
    private bool _showAccountValue = true;
    public bool ShowAccountValue
    {
        get => _showAccountValue;
        set 
        {
            if (SetProperty(ref _showAccountValue, value))
            {
                // Just save the preference - we'll use polling to detect changes in other ViewModels
                Preferences.Default.Set("ShowAccountValue", value);
                
                System.Diagnostics.Debug.WriteLine($"Account value visibility set to: {(value ? "Visible" : "Hidden")}");
                
                // Force an update to ensure it takes effect immediately
                try
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        MessagingCenter.Send<SettingsViewModel, bool>(this, "AccountValueVisibilityChanged", value);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error notifying about preference change: {ex.Message}");
                }
            }
        }
    }
    
    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                try
                {
                    _themeService?.SetTheme(value);
                    System.Diagnostics.Debug.WriteLine($"Theme toggled to: {(value ? "Dark" : "Light")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error toggling theme: {ex.Message}");
                }
            }
        }
    }
    
    public ICommand SignOutCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public SettingsViewModel(IThemeService themeService, IAuthService authService)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        
        // Load saved theme preference
        try
        {
            _isDarkTheme = _themeService.GetSavedTheme();
            System.Diagnostics.Debug.WriteLine($"Loaded theme preference: {(_isDarkTheme ? "Dark" : "Light")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            _isDarkTheme = false; 
        }
        
        // Load the saved account value visibility preference
        try
        {
            _showAccountValue = Preferences.Default.Get("ShowAccountValue", true);
            System.Diagnostics.Debug.WriteLine($"Loaded account value visibility preference: {(_showAccountValue ? "Visible" : "Hidden")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading account value visibility preference: {ex.Message}");
            _showAccountValue = true; // Default to showing account value
        }
        
        SignOutCommand = new AsyncRelayCommand(SignOut);
        ToggleThemeCommand = new RelayCommand<bool>(ToggleTheme);
    }
    
    private void ToggleTheme(bool isDarkTheme)
    {
        try
        {
            IsDarkTheme = isDarkTheme;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ToggleTheme: {ex.Message}");
        }
    }

    private async Task SignOut()
    {
        try
        {
            // Confirm before signing out
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Sign Out", 
                "Are you sure you want to sign out?", 
                "Yes", "No");
                
            if (!confirmed)
                return;
                
            // Log the user out
            _authService.Logout();
            
            var loginViewModel = CryptoApp.Services.Services.GetService<LoginViewModel>() ?? 
                new LoginViewModel(_authService);
                
            var loginPage = new LoginPage(loginViewModel);
            
            // Navigate directly to login page
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current.MainPage = loginPage;
            });
            
            // Show confirmation
            await Application.Current.MainPage.DisplayAlert(
                "Signed Out", 
                "You have been signed out successfully", 
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error signing out: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert(
                "Error", 
                "An error occurred while signing out", 
                "OK");
        }
    }
}