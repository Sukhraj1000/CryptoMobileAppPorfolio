using Microsoft.Maui.Controls;
using CryptoApp.Views;

namespace CryptoApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(PortfolioPage), typeof(PortfolioPage));
        Routing.RegisterRoute(nameof(DepositPage), typeof(DepositPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        Current.CurrentItem = Current.Items.First();
    }
}