using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface ITokenServices
    {
        // Define your token-related methods here (e.g., GenerateToken, ValidateToken)
        string GenerateAccessToken(User user, IList<string> roles);
        string JwtKeyGenerate();
        Task RevokeTokenAsync(string jti, DateTime expiry);
        Task<bool> IsTokenRevokedAsync(string jti);
        RefreshTokenResult GenerateRefreshToken();
        // string GetRefreshToken();
        // Task<bool> ValidateTokenAsync(string token);
        Task<AuthResponse> RefreshNewToken(string rawRefreshTokenFromCookie);
        Task RevokeRefreshTokenAsync(string rawToken);
        Task AddRefreshToken(app_refresh_token refresh_token, User user);
    }
}