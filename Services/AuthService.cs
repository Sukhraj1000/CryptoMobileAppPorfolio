using CryptoApp.Interfaces;
using CryptoApp.Models;
using Supabase.Postgrest;

namespace CryptoApp.Services
{
    /// <summary>
    /// Service responsible for handling user authentication and session management.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly Supabase.Client _client;
        private static User _currentUser;

        public AuthService(Supabase.Client client)  
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Authenticates a user with their username and password.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>True if authentication is successful, false otherwise.</returns>
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

        /// <summary>
        /// Registers a new user with the provided credentials.
        /// </summary>
        /// <param name="username">The desired username.</param>
        /// <param name="password">The desired password.</param>
        /// <returns>A tuple containing success status and a message.</returns>
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

        /// <summary>
        /// Logs out the current user and clears the session.
        /// </summary>
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