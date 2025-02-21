using CryptoApp.Interfaces;
using CryptoApp.Services;
using CryptoApp.ViewModels;
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

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "crypto.db");
        builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(dbPath));

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<PortfolioViewModel>();
        builder.Services.AddSingleton<HomeViewModel>();

        return builder.Build();
    }
}