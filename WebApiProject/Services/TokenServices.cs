using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApiProject.Interfaces;
using WebApiProject.Models;
using WebApiProject.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace WebApiProject.Services
{
    public class TokenServices : ITokenServices
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly string _refreshTokenSecretKey;
        private ITokenRepository _tokenRepository;
        private IRoleServices _roleService;
        public TokenServices(IConfiguration configuration, IMemoryCache cache, IOptions<RefreshTokenSettings> options, ITokenRepository tokenRepository, IRoleServices roleService)
        {
            _configuration = configuration;
            _cache = cache;
            _refreshTokenSecretKey = options.Value.SecretKey;
            _tokenRepository = tokenRepository;
            _roleService = roleService;

            if (string.IsNullOrWhiteSpace(_refreshTokenSecretKey))
                throw new InvalidOperationException("RefreshTokenSettings:SecretKey is not configured"); // fail fast saat startup, bukan silent null saat runtime
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
            if (ttl > TimeSpan.Zero)
                _cache.Set($"blacklist:{jti}", true, ttl);

            return Task.CompletedTask;
        }

        public Task<bool> IsTokenRevokedAsync(string jti)
        {
            return Task.FromResult(_cache.TryGetValue($"blacklist:{jti}", out _));
        }

        public RefreshTokenResult GenerateRefreshToken()
        {
            // generate raw random bytes dulu
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            string rawToken = Convert.ToBase64String(randomBytes);

            // hash raw token sebelum disimpan — HMAC dengan secret key, bukan hash polos
            string hashedToken = HashToken(rawToken);

            var entity = new app_refresh_token
            {
                id = Guid.NewGuid(), // sesuaikan tipe dengan kolom asli di DB
                token = hashedToken, // yang disimpan ke DB = hash
                expired_at = DateTime.UtcNow.AddDays(7)
            };
            return new RefreshTokenResult
            {
                Entity = entity,
                RawToken = rawToken
            };
        }

        private string HashToken(string rawToken)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_refreshTokenSecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(hash);
        }

        public async Task<AuthResponse> RefreshNewToken(string rawRefreshTokenFromCookie)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshTokenFromCookie))
                throw new ValidationExceptions("Refresh token is required");

            string hashedInput = HashToken(rawRefreshTokenFromCookie);
            app_refresh_token? storedToken = await _tokenRepository.GetByHashedTokenAsync(hashedInput);

            if (storedToken == null)
                throw new ValidationExceptions("Invalid refresh token");

            // Reuse detection: token ini sudah pernah dirotasi tapi dipakai lagi → kemungkinan dicuri
            if (storedToken.replaced_by_token != null || storedToken.revoked_at != null)
            {
                await RevokeAllUserTokensAsync(storedToken.user_id);
                throw new ValidationExceptions("Token reuse detected, please login again");
            }

            if (storedToken.expired_at < DateTime.UtcNow)
                throw new ValidationExceptions("Refresh token expired");

            var user = storedToken.user;
            List<string> roles = await _roleService.GetUserRoles(user);
            string newAccessToken = GenerateAccessToken(user, roles);

            RefreshTokenResult newRefreshToken = GenerateRefreshToken();

            newRefreshToken.Entity.user_id = user.Id;
            newRefreshToken.Entity.expired_at = DateTime.UtcNow.AddDays(7);

            await _tokenRepository.AddRefreshTokenAsync(newRefreshToken.Entity);
            await _tokenRepository.SaveAsync(); // supaya newRefreshToken.id ke-generate sebelum dipakai di bawah

            storedToken.revoked_at = DateTime.UtcNow;
            storedToken.replaced_by_token = newRefreshToken.Entity.token;
            await _tokenRepository.SaveAsync();

            return new AuthResponse(
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken.RawToken,
                ExpiresIn: 3600
            );
        }

        private async Task RevokeAllUserTokensAsync(string userId)
        {
            List<app_refresh_token> tokens = await _tokenRepository.GetListRefreshTokenOnNullRevokeByUserId(userId);
            foreach (var t in tokens) t.revoked_at = DateTime.UtcNow;
            await _tokenRepository.SaveAsync();
        }

        public async Task RevokeRefreshTokenAsync(string rawToken)
        {
            string hashedToken = HashToken(rawToken);
            var storedToken = await _tokenRepository.GetByHashedTokenAsync(hashedToken);
            if (storedToken != null && storedToken.revoked_at == null)
            {
                storedToken.revoked_at = DateTime.UtcNow;
                await _tokenRepository.SaveAsync();
            }
        }
    }
}