using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace CryptoApp.ViewModels;

public class SettingsViewModel
{
    public bool EnableNotifications { get; set; }
    public ICommand SignOutCommand { get; }

    public SettingsViewModel()
    {
        SignOutCommand = new AsyncRelayCommand(SignOut);
    }

    private async Task SignOut()
    {
        // Logic for signing out
        await Shell.Current.GoToAsync("//LoginPage");
    }
}