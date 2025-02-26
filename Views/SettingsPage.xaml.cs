using CryptoApp.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoApp.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}