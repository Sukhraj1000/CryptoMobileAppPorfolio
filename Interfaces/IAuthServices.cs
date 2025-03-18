using System.Threading.Tasks;

namespace CryptoApp.Interfaces
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task<(bool success, string message)> RegisterAsync(string username, string password);
        void Logout();
    }
}