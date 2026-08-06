using AuthenticationAPI.DTO;

namespace AuthenticationAPI.interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync (RegisterRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task LogoutAsync(string userId);
        Task<AuthResponseDto> Verify2FaAsync(Verify2FaDto dto);
        Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<AuthResponseDto> ResetUserPasswordAsync(string userId);
    }
}
