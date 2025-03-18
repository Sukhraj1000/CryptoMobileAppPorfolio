using CryptoApp.Interfaces;
using CryptoApp.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CryptoApp;
public partial class App : Application
{
    private readonly IThemeService _themeService;
    
    public App(IThemeService themeService)
    {
        try
        {
            InitializeComponent();
            _themeService = themeService;
            
            if (_themeService != null)
            {
                ApplySavedTheme();
            }
            
            var authService = CryptoApp.Services.Services.GetService<IAuthService>();
            MainPage = new LoginPage(new ViewModels.LoginViewModel(authService));
            
            this.PropertyChanged += OnAppPropertyChanged;
            
            _themeService?.Initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in App constructor: {ex.Message}");
        }
    }
    
    private void OnAppPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPage))
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainPage set, ensuring theme is applied...");
                ApplySavedTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme on MainPage set: {ex.Message}");
            }
        }
    }
    
    private void ApplySavedTheme()
    {
        try
        {
            if (_themeService != null)
            {
                bool isDarkTheme = _themeService.GetSavedTheme();
                UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
                System.Diagnostics.Debug.WriteLine($"Theme set to {UserAppTheme}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ThemeService is null, using default light theme");
                UserAppTheme = AppTheme.Light;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ApplySavedTheme: {ex.Message}");
            UserAppTheme = AppTheme.Light;
        }
    }
}