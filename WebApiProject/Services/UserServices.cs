using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Services
{
    public class UserServices : IUserServices
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserServices(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<User> RegisterAsync(RegisterData model)
        {
            var user = new User
            {
                UserName = model.username,
                Email = model.email,
                name = model.name,
                phone_number = model.phone_number
            };

            var result = await _userManager.CreateAsync(user, model.password);

            if (result.Succeeded)
            {
                return user;
            }
            else
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}