using CryptoApp.Interfaces;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CryptoApp.Services
{
    /// <summary>
    /// Service responsible for managing application theme preferences and appearance.
    /// </summary>
    public class ThemeService : IThemeService
    {
        private const string ThemePreferenceKey = "app_theme";
        
        /// <summary>
        /// Gets the current application theme.
        /// </summary>
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
        
        /// <summary>
        /// Gets whether the current theme is dark mode.
        /// </summary>
        public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;
        
        /// <summary>
        /// Initialises the theme service and applies the saved theme preference.
        /// </summary>
        public void Initialize()
        {
            try
            {
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
        
        /// <summary>
        /// Sets the application theme to either light or dark mode.
        /// </summary>
        /// <param name="isDarkTheme">True for dark theme, false for light theme.</param>
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
        
        /// <summary>
        /// Retrieves the user's saved theme preference from persistent storage.
        /// </summary>
        /// <returns>True if dark theme is preferred, false for light theme.</returns>
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
        
        /// <summary>
        /// Saves the user's theme preference to persistent storage.
        /// </summary>
        /// <param name="isDarkTheme">True for dark theme, false for light theme.</param>
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