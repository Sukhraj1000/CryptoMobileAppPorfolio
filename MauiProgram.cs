using System;
using System.Threading.Tasks;
using CryptoApp.Converters;
using CryptoApp.Helpers;
using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.ViewModels;
using CryptoApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microcharts.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui;

namespace CryptoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Supabase Setup
        var supabaseUrl = "https://jfukhwbyszlclqfhecmn.supabase.co";
        var supabaseKey = "";
        var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey);

        builder.Services.AddSingleton(supabaseClient);
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        builder.UseMauiApp<App>()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
               })
               .UseSkiaSharp(); // This adds SkiaSharp support needed for Microcharts

        // Register Services
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CryptoService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddTransient<PercentageColorConverter>();
        builder.Services.AddSingleton<ListVisibilityConverter>();
        builder.Services.AddTransient<NotNullConverter>();

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
        
        // Initialise the Services static class with the service provider
        CryptoApp.Services.Services.Initialize(mauiApp.Services);

        // Initialise Supabase (but don't block app startup)
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
