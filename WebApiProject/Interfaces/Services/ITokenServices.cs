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
        app_refresh_token GenerateRefreshToken();
        // string GetRefreshToken();
        // Task<bool> ValidateTokenAsync(string token);
    }
}