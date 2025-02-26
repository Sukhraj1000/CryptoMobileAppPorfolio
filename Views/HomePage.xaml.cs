using CryptoApp.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoApp.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}