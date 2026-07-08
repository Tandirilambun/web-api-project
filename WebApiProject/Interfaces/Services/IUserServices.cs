using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IUserServices
    {
        Task<User> RegisterAsync(RegisterData model);
        Task<User> LoginAsync(LoginData model);
        Task<UserProfileDTO> GetUserByIdAsync(string id);
    }
}