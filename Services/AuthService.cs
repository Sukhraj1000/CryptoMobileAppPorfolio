using System;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;
using CryptoApp.Models;

namespace CryptoApp.Services
{
    public class AuthService
    {
        private readonly Supabase.Client _client;

        public AuthService()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? throw new InvalidOperationException("SUPABASE_URL is not set.");
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? throw new InvalidOperationException("SUPABASE_KEY is not set.");


            _client = new Supabase.Client(url, key, new SupabaseOptions { AutoRefreshToken = true });
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var session = await _client.Auth.SignIn(email, password);
                return session != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            try
            {
                var user = await _client.Auth.SignUp(email, password);
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await _client.Auth.SignOut();
        }
    }
}