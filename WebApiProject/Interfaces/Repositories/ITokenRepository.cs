using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface ITokenRepository
    {
        Task AddRefreshTokenAsync(app_refresh_token refresh_token);
    }
}