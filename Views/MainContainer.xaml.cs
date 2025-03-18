using Microsoft.Maui.Controls;
using CryptoApp.ViewModels;
using CryptoApp.Interfaces;
using CryptoApp.Services;

namespace CryptoApp.Views
{
    public partial class MainContainer : TabbedPage
    {
        public MainContainer()
        {
            InitializeComponent();
            
            try
            {
                // Get all required services for ViewModels
                var databaseService = CryptoApp.Services.Services.GetService<IDatabaseService>();
                var cryptoService = CryptoApp.Services.Services.GetService<CryptoService>();
                var themeService = CryptoApp.Services.Services.GetService<IThemeService>();
                var authService = CryptoApp.Services.Services.GetService<IAuthService>();
                
                // Ensure all required services are available
                if (databaseService == null || cryptoService == null || 
                    themeService == null || authService == null)
                {
                    Console.WriteLine("Error: One or more required services are not available");
                    return;
                }
                
                // Initialize tabs with properly initialized ViewModels
                var homeViewModel = new HomeViewModel(databaseService, cryptoService);
                var portfolioViewModel = new PortfolioViewModel(cryptoService, databaseService, authService);
                var depositViewModel = new DepositViewModel(databaseService);
                var settingsViewModel = new SettingsViewModel(themeService, authService);
                
                // Add tabs with their ViewModels
                Children.Add(new HomePage(homeViewModel) { Title = "Home" });
                Children.Add(new PortfolioPage(portfolioViewModel) { Title = "Portfolio" });
                Children.Add(new DepositPage(depositViewModel) { Title = "Deposit" });
                Children.Add(new SettingsPage(settingsViewModel) { Title = "Settings" });
                
                // Set the first tab as current
                CurrentPage = Children[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing MainContainer: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 