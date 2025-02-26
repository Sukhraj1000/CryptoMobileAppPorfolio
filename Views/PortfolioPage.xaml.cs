using CryptoApp.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoApp.Views;

public partial class PortfolioPage : ContentPage
{
    public PortfolioPage(PortfolioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}