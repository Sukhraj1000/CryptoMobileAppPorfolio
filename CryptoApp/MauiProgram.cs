using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Supabase;

namespace CryptoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Retrieve Supabase environment variables
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
        var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true,
            // SessionHandler = new SupabaseSessionHandler() <-- This must be implemented by the developer
        };

        // Register Supabase as a singleton
        builder.Services.AddSingleton(provider => new Supabase.Client(url, key, options));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}