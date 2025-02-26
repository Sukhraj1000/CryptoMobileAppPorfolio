using System.Threading.Tasks;
using CryptoApp.Helpers;
using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.ViewModels;
using CryptoApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace CryptoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder.UseMauiApp<App>();

        // Register Services
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CryptoService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddTransient<PercentageColorConverter>();
        builder.Services.AddSingleton<ListVisibilityConverter>();

        // Register ViewModels
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<PortfolioViewModel>();
        builder.Services.AddSingleton<DepositViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Register Views
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<PortfolioPage>();
        builder.Services.AddSingleton<DepositPage>();
        builder.Services.AddSingleton<SettingsPage>();

        var mauiApp = builder.Build(); 

        // Initialise Database AFTER building the app
            Task.Run(async () =>
        {
            var dbService = mauiApp.Services.GetRequiredService<IDatabaseService>();
            await dbService.InitializeAsync();
    
            // Runs the Supabase test
            await dbService.TestSupabaseConnection();
        });


        return mauiApp;
    }
}