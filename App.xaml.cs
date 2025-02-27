using CryptoApp.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CryptoApp;
public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(loginPage); 
    }
}
