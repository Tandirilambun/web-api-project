using System.Text.Json;
using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IUserServices
    {
        Task<UserResponseDTO> RegisterAsync(RegisterData model);
        Task<LoginResponse> LoginAsync(LoginData model);
        Task<UserProfileDTO> GetUserByIdAsync(string id);
        Task<UserProfileDTO> UpdateUserAsync(string id, Dictionary<string, JsonElement> updatedFields);

        Task LogoutAsync(string? rawRefreshToken);
        Task<string> IsUserAuthorized();
    }
}