using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Data;
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
        private readonly WebApiDbContext _db;
        private ITokenServices _tokenServices;
        private IRoleServices _roleServices;
        private ITokenRepository _tokenRepository;
        private IHttpContextAccessor _http;

        public UserServices(UserManager<User> userManager,
        SignInManager<User> signInManager, WebApiDbContext db,
        ITokenServices tokenServices, IRoleServices roleServices,
        ITokenRepository tokenRepository, IHttpContextAccessor http)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenServices = tokenServices;
            _roleServices = roleServices;
            _tokenRepository = tokenRepository;
            _http = http;
            _db = db;
        }

        public ClaimsPrincipal? User => _http.HttpContext?.User;

        public async Task<UserResponseDTO> RegisterAsync(RegisterData model)
        {
            await ValidateRegisterData(model);

            string slug_name = StringHelper.ConvertToSlug(model.name);

            User user = new User
            {
                UserName = model.username,
                Email = model.email,
                name = model.name,
                PhoneNumber = model.phone_number,
                slug = slug_name
            };

            var result = await _userManager.CreateAsync(user, model.password);
            var role = await _roleServices.SignUserToRole(user, model.role);

            if (result.Succeeded || role)
            {
                return new UserResponseDTO
                {
                    id = user.Id,
                    username = user.UserName,
                    name = user.name,
                    slug = user.slug,
                    twoFactorEnabled = user.TwoFactorEnabled,
                    emailConfirmed = user.EmailConfirmed,
                    pohneNumberConfirmed = user.PhoneNumberConfirmed
                };
            }
            else
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginData model)
        {
            await ValidateLoginData(model);

            User? user = await _userManager.FindByNameAsync(model.username);
            if (user == null) throw new ValidationExceptions("Invalid username or password");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.password, false);
            if (!result.Succeeded) throw new ValidationExceptions("Invalid username or password");

            List<string> roles = await _roleServices.GetUserRoles(user);
            string access_token = _tokenServices.GenerateAccessToken(user, roles);

            app_refresh_token refresh_token = _tokenServices.GenerateRefreshToken();

            await AddRefreshToken(refresh_token, user.Id);

            LoginResponseDTO.user_data user_data = new LoginResponseDTO.user_data
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                name = user.name,
                phone_number = user.PhoneNumber,
                slug = user.slug,
                roles = roles
            };

            return new LoginResponseDTO()
            {
                access_token = access_token,
                token_type = "Bearer",
                expires_in = 3600,
                refresh_token = refresh_token.token,
                user = user_data
            };
        }

        public async Task LogoutAsync()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var expiryClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
            if (jti == null || expiryClaim == null)
            {
                throw new AppExceptions("Invalid token.", 400);
            }
            var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryClaim)).UtcDateTime;
            await _tokenServices.RevokeTokenAsync(jti, expiry);
        }

        public async Task<UserProfileDTO> GetUserByIdAsync(string id)
        {
            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundExceptions("User not found");
            }

            UserProfileDTO userProfile = new UserProfileDTO
            {
                id = user.Id,
                username = user.UserName,
                name = user.name,
                slug = user.slug,
                twoFactorEnabled = user.TwoFactorEnabled,
                emailConfirmed = user.EmailConfirmed,
                phoneNumberConfirmed = user.PhoneNumberConfirmed,
                email = user.Email,
                phone_number = user.PhoneNumber
            };

            return userProfile;
        }

        public async Task<UserProfileDTO> UpdateUserAsync(string id, Dictionary<string, JsonElement> updatedFields)
        {
            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundExceptions("User not found");
            }

            var transaction = await _db.Database.BeginTransactionAsync();
            using (transaction)
            {
                try
                {
                    foreach (var field in updatedFields)
                    {
                        switch (field.Key.ToLower())
                        {
                            case "username":
                                string? new_username = field.Value.GetString();
                                if (!string.IsNullOrWhiteSpace(new_username))
                                {
                                    var setResultUsername = await _userManager.SetUserNameAsync(user, new_username);
                                    if (!setResultUsername.Succeeded)
                                        throw new ValidationExceptions(string.Join(", ", setResultUsername.Errors.Select(e => e.Description)));
                                }
                                break;

                            case "name":
                                user.name = field.Value.GetString() ?? user.name;
                                user.slug = StringHelper.ConvertToSlug(user.name);
                                break;

                            case "email":
                                string? new_email = field.Value.GetString();
                                if (!StringHelper.isValidEmail(new_email ?? ""))
                                    throw new ValidationExceptions($"Email '{new_email}' is not valid");
                                var setResultEmail = await _userManager.SetEmailAsync(user, new_email);
                                if (!setResultEmail.Succeeded)
                                    throw new ValidationExceptions(string.Join(", ", setResultEmail.Errors.Select(e => e.Description)));
                                user.EmailConfirmed = false; // Reset email confirmation status
                                break;

                            case "phone_number":
                                string? new_phone_number = field.Value.GetString() ?? user.PhoneNumber;
                                var setResultPhone = await _userManager.SetPhoneNumberAsync(user, new_phone_number);
                                if (!setResultPhone.Succeeded)
                                    throw new ValidationExceptions(string.Join(", ", setResultPhone.Errors.Select(e => e.Description)));
                                user.PhoneNumberConfirmed = false; // Reset phone number confirmation status
                                break;

                            default:
                                throw new ValidationExceptions($"Field '{field.Key}' is not valid for update");
                        }
                    }

                    var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        throw new AppExceptions(string.Join(", ", result.Errors.Select(e => e.Description)), 500);
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw new AppExceptions("An error occurred while updating user information", 500);
                }
            }

            return new UserProfileDTO
            {
                id = user.Id,
                username = user.UserName,
                name = user.name,
                slug = user.slug,
                twoFactorEnabled = user.TwoFactorEnabled,
                emailConfirmed = user.EmailConfirmed,
                phoneNumberConfirmed = user.PhoneNumberConfirmed,
                email = user.Email,
                phone_number = user.PhoneNumber
            };
        }

        public async Task AddRefreshToken(app_refresh_token refresh_token, string id_user)
        {
            refresh_token.user_id = id_user;
            refresh_token.created_at = DateTime.Now;
            await _tokenRepository.AddRefreshTokenAsync(refresh_token);
            await _db.SaveChangesAsync();
        }

        public async Task<string> IsUserAuthorized()
        {
            var userId = User?.FindFirst("sub")?.Value;
            if (userId == null)
                throw new AppExceptions("Unauthorized", 401);
            
            return userId;
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