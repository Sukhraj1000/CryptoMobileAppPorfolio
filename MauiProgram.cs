using System.Threading.Tasks;
using CryptoApp.Converters;
using CryptoApp.Helpers;
using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.ViewModels;
using CryptoApp.Views;
using Microsoft.Extensions.DependencyInjection;


namespace CryptoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Supabase Setup
        var supabaseUrl = "https://jfukhwbyszlclqfhecmn.supabase.co";
        var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImpmdWtod2J5c3psY2xxZmhlY21uIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzkyMTc3NDgsImV4cCI6MjA1NDc5Mzc0OH0.KikzTEL99_ngl6mzCpep76wFzcrEDi_aqRfed_SgApg";
        var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey);

        builder.Services.AddSingleton(supabaseClient);
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        builder.UseMauiApp<App>();

        // Register Services
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CryptoService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddTransient<PercentageColorConverter>();
        builder.Services.AddSingleton<ListVisibilityConverter>();

        // Register ViewModels
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<RegisterViewModel>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<PortfolioViewModel>();
        builder.Services.AddSingleton<DepositViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Register Views
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<RegisterPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<PortfolioPage>();
        builder.Services.AddSingleton<DepositPage>();
        builder.Services.AddSingleton<SettingsPage>();
        
        var mauiApp = builder.Build();
        
        // Initialize the Services static class with the service provider
        CryptoApp.Services.Services.Initialize(mauiApp.Services);

        // Initialize Supabase (but don't block app startup)
        Task.Run(async () =>
        {
            try
            {
                await supabaseClient.InitializeAsync();  
                Console.WriteLine("Supabase Initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supabase Initialization Error: {ex.Message}");
            }
        });
        
        return mauiApp;
    }
}
