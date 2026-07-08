using Microsoft.AspNetCore.Identity;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Services
{
    public class RoleServices : IRoleServices
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleServices(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<List<IdentityRole>> GenerateAllRoles()
        {
            List<string> roles = new List<string>
            {
                "Admin",
                "Owner",
                "User"
            };

            List<IdentityRole> success_created = new List<IdentityRole>();
            try
            {
                foreach (var role in roles)
                {
                    bool is_roles_exist = await _roleManager.RoleExistsAsync(role);
                    if (!is_roles_exist)
                    {
                        var result = await _roleManager.CreateAsync(new IdentityRole(role));
                        if (result.Succeeded)
                        {
                            IdentityRole created = await _roleManager.FindByNameAsync(role);
                            success_created.Add(created);
                        }
                    }
                }
                return success_created;
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<List<string>> GetUserRoles(User user)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                return roles.ToList();
            }
            catch (System.Exception)
            {
                throw new Exception("Error retrieving user roles.");
            }
        }

        public async Task<bool> SignUserToRole(User user, string role)
        {
            try
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                return result.Succeeded;
            }
            catch (System.Exception)
            {
                throw new Exception("Error signing user to role.");
            }
        }
    }
}