using System.Text.Json;
using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IUserServices
    {
        Task<UserResponseDTO> RegisterAsync(RegisterData model);
        Task<LoginResponseDTO> LoginAsync(LoginData model);
        Task<UserProfileDTO> GetUserByIdAsync(string id);
        Task<UserProfileDTO> UpdateUserAsync(string id, Dictionary<string, JsonElement> updatedFields);

        Task LogoutAsync();
        Task<string> IsUserAuthorized();
    }
}