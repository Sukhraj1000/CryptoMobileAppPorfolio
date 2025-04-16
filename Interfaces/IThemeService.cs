using Microsoft.Maui.ApplicationModel;

namespace CryptoApp.Interfaces
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        bool IsDarkTheme { get; }
        
        // Initialize the theme service and apply saved theme
        void Initialize();
        
        //Set the app theme (light or dark)
        void SetTheme(bool isDarkTheme);
        
        // Get the user's theme preference from preferences storage
        bool GetSavedTheme();
        
        // Save the user's theme preference to preferences storage
        void SaveTheme(bool isDarkTheme);
    }
} 