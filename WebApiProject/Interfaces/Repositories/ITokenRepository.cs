using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface ITokenRepository
    {
        Task AddRefreshTokenAsync(app_refresh_token refresh_token);
        Task<app_refresh_token?> GetByHashedTokenAsync(string hashedToken);
        Task<List<app_refresh_token>> GetListRefreshTokenOnNullRevokeByUserId(string user_id);
        Task SaveAsync();
        
    }
}