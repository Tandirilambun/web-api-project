using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IRoleServices
    {
        Task<List<IdentityRole>> GenerateAllRoles();
        Task<bool> SignUserToRole(User user, string role);
    }
}