using System.Threading.Tasks;

namespace CryptoApp.Interfaces
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
    }
}