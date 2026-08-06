using AuthenticationAPI.Models;

namespace AuthenticationAPI.interfaces
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
