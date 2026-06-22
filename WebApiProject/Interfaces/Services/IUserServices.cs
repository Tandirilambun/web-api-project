using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IUserServices
    {
        Task<User> RegisterAsync(RegisterData model);
    }
}