using Microsoft.EntityFrameworkCore;
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

        public async Task<app_refresh_token?> GetByHashedTokenAsync(string hashedToken)
        {
            return await _db.app_refresh_tokens
                .Include(t => t.user)
                .FirstOrDefaultAsync(t => t.token == hashedToken);
        }

        public async Task<List<app_refresh_token>> GetListRefreshTokenOnNullRevokeByUserId(string user_id)
        {
            var tokens = await _db.app_refresh_tokens
                .Where(t => t.user_id == user_id && t.revoked_at == null)
                .ToListAsync();

            return tokens;
        }

        public async Task SaveAsync() => await _db.SaveChangesAsync();
    }
}