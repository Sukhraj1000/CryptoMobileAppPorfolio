using CryptoApp.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoApp.Views;

public partial class DepositPage : ContentPage
{
    public DepositPage(DepositViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}