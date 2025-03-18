using CryptoApp.Interfaces;
using CryptoApp.Models;
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

        public async Task<(bool success, string message)> RegisterAsync(string username, string password)
        {
            try
            {
                // Check if username already exists
                var existingUser = await _client
                    .From<User>()
                    .Filter("username", Constants.Operator.Equals, username)
                    .Get();

                if (existingUser.Models.Count > 0)
                {
                    return (false, "Username already exists. Please choose another one.");
                }

                // Create a new user
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Balance = 1000 // Give new users some starting balance
                };

                // Insert into database
                await _client.From<User>().Insert(newUser);

                Console.WriteLine($"User registered successfully: {username}");
                return (true, "Registration successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                return (false, $"Registration failed: {ex.Message}");
            }
        }

        public void Logout()
        {
            _currentUser = null;
            Console.WriteLine("User logged out successfully");
        }

        public static User GetCurrentUser()
        {
            return _currentUser;
        }
    }
}