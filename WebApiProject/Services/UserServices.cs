using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Exceptions;
using WebApiProject.Helper;
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
            await ValidateRegisterData(model);

            string slug_name = StringHelper.ConvertToSlug(model.name);

            var user = new User
            {
                UserName = model.username,
                Email = model.email,
                name = model.name,
                phone_number = model.phone_number,
                slug = slug_name
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

        public async Task<User> LoginAsync(LoginData model)
        {
            await ValidateLoginData(model);

            var user = await _userManager.FindByNameAsync(model.username);
            if (user == null)
            {
                throw new ValidationExceptions("Invalid username or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.password, false);
            if (!result.Succeeded)
            {
                throw new ValidationExceptions("Invalid username or password");
            }

            return user;
        }

        public async Task<UserProfileDTO> GetUserByIdAsync(string id)
        {
            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundExceptions("User not found");
            }

            var userProfile = new UserProfileDTO
            {
                id = user.Id,
                username = user.UserName,
                name = user.name,
                slug = user.slug,
                twoFactorEnabled = user.TwoFactorEnabled,
                emailConfirmed = user.EmailConfirmed,
                pohneNumberConfirmed = user.PhoneNumberConfirmed,
                email = user.Email,
                phone_number = user.phone_number
            };

            return userProfile;
        }

        #region Validation Methods

        private async Task ValidateRegisterData(RegisterData model)
        {
            if (string.IsNullOrWhiteSpace(model.username))
            {
                throw new ValidationExceptions("Username is required");
            }

            if (string.IsNullOrWhiteSpace(model.email))
            {
                throw new ValidationExceptions("Email is required");
            }

            if (string.IsNullOrWhiteSpace(model.password))
            {
                throw new ValidationExceptions("Password is required");
            }

            if (string.IsNullOrWhiteSpace(model.name))
            {
                throw new ValidationExceptions("Name is required");
            }

            bool valid_email = StringHelper.isValidEmail(model.email);
            if (!valid_email)
            {
                throw new ValidationExceptions($"Email '{model.email}' is not valid");
            }

            if (string.Compare(model.password, model.confirmPassword) != 0)
            {
                throw new ValidationExceptions("Password and Confirm Password do not match");
            }

            User? username_exist = await _userManager.FindByNameAsync(model.username);
            User? email_exist = await _userManager.FindByEmailAsync(model.email);

            if (username_exist != null)
            {
                throw new ConflictExceptions("Username already exists");
            }

            if (email_exist != null)
            {
                throw new ConflictExceptions("Email already exists");
            }
        }

        private async Task ValidateLoginData(LoginData model)
        {
            if (string.IsNullOrWhiteSpace(model.username))
            {
                throw new ValidationExceptions("Username is required");
            }

            if (string.IsNullOrWhiteSpace(model.password))
            {
                throw new ValidationExceptions("Password is required");
            }
        }
        
        #endregion

    }
}