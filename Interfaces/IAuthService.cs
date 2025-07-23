using Ecom_api.Models;
using Ecom_api.Dto;  


namespace Ecom_api.Interfaces
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDto request);
        Task<TokenResponseDto?> LoginAsync(UserDto request);
        Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request);

        Task<User?> GetCurrentUserAsync();
    }
}
