using CryptoApp.Interfaces;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CryptoApp.Services
{
    public class ThemeService : IThemeService
    {
        private const string ThemePreferenceKey = "app_theme";
        
        public AppTheme CurrentTheme 
        { 
            get 
            {
                try
                {
                    return Application.Current?.RequestedTheme ?? AppTheme.Light;
                }
                catch
                {
                    return AppTheme.Light;
                }
            } 
        }
        
        public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;
        
        public void Initialize()
        {
            try
            {
                // Check if Application.Current is available
                if (Application.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot initialize theme: Application.Current is null");
                    return;
                }
                
                bool savedTheme = GetSavedTheme();
                SetTheme(savedTheme);
                System.Diagnostics.Debug.WriteLine($"Theme service initialized with {(savedTheme ? "Dark" : "Light")} theme");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing theme service: {ex.Message}");
            }
        }
        
        public void SetTheme(bool isDarkTheme)
        {
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
                    SaveTheme(isDarkTheme);
                    System.Diagnostics.Debug.WriteLine($"Theme set to {(isDarkTheme ? "Dark" : "Light")}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cannot set theme: Application.Current is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting theme: {ex.Message}");
            }
        }
        
        public bool GetSavedTheme()
        {
            try
            {
                if (Preferences.Default != null)
                {
                    return Preferences.Get(ThemePreferenceKey, false);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting saved theme: {ex.Message}");
                return false;
            }
        }
        
        public void SaveTheme(bool isDarkTheme)
        {
            try
            {
                if (Preferences.Default != null)
                {
                    Preferences.Set(ThemePreferenceKey, isDarkTheme);
                    System.Diagnostics.Debug.WriteLine($"Theme preference saved: {(isDarkTheme ? "Dark" : "Light")}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save theme: Preferences.Default is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
            }
        }
    }
} 