using Microsoft.Extensions.DependencyInjection;

namespace CryptoApp.Services
{
    public static class Services
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Service provider not initialized");
                return null;
            }
            
            return _serviceProvider.GetService<T>();
        }
    }
} 