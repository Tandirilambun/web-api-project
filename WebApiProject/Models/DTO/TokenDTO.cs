namespace WebApiProject.Models
{
    public record RefreshTokenRequest(string RefreshToken);

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserResponseDTO? User = null
    );

    public class RefreshTokenResult
    {
        public app_refresh_token Entity { get; set; } = default!;
        public string RawToken { get; set; } = default!;
    }

    public class RefreshTokenSettings
    {
        public string SecretKey { get; set; } = default!;
    }

    
}