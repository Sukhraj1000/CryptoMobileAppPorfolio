using CryptoApp.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoApp.Views;

public partial class LoginPage : ContentPage
{
    private void OnUsernameCompleted(object sender, EventArgs e)
    {
        ((Entry)sender).Unfocus(); 
    }

    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        ((Entry)sender).Unfocus(); 
    }

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; 
    }
}