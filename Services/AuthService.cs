using System;
using System.Threading.Tasks;
using CryptoApp.Interfaces;
using CryptoApp.Models;
using Supabase;
using Supabase.Postgrest;

namespace CryptoApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly Supabase.Client _client;
        private static User _currentUser;

        public AuthService(Supabase.Client client)  
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var userResponse = await _client
                    .From<User>()
                    .Filter("username", Constants.Operator.Equals, username)
                    .Single();

                if (userResponse == null)
                {
                    Console.WriteLine("User not found.");
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(password, userResponse.PasswordHash))
                {
                    Console.WriteLine("Invalid password.");
                    return false;
                }

                _currentUser = userResponse; 
                Console.WriteLine("Login successful.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }

        public static User GetCurrentUser()
        {
            return _currentUser;
        }

    }

}