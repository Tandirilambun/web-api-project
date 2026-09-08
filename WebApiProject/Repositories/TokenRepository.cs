using WebApiProject.Data;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Repositories
{
    public class TokenRepositories : ITokenRepository
    {
        private WebApiDbContext _db;
        public TokenRepositories(WebApiDbContext db) => _db = db;

        public async Task AddRefreshTokenAsync(app_refresh_token refresh_token)
        {
            await _db.app_refresh_tokens.AddAsync(refresh_token);
        }
    } 
}