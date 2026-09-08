using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApiProject.Interfaces;
using WebApiProject.Models;
using WebApiProject.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace WebApiProject.Services
{
    public class TokenServices : ITokenServices
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        public TokenServices(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }
        public string GenerateAccessToken(User user, IList<string> roles)
        {

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Name, user.UserName!),
                new("token_type", "user")
            };

            claims.AddRange(roles.Select(role => new Claim("role", role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expire = double.Parse(_configuration["Jwt:ExpireMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expire),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string JwtKeyGenerate()
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return key;
        }

        public Task RevokeTokenAsync(string jti, DateTime expiry)
        {
            var ttl = expiry - DateTime.UtcNow;
            if( ttl > TimeSpan.Zero)
                _cache.Set($"blacklist:{jti}", true, ttl);
            
            return Task.CompletedTask;
        }

        public Task<bool> IsTokenRevokedAsync(string jti)
        {
            return Task.FromResult(_cache.TryGetValue($"blacklist:{jti}", out _));
        }
    
        public app_refresh_token GenerateRefreshToken()
        {
            return new app_refresh_token
            {
                id = Guid.NewGuid(),
                token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                expired_at = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}