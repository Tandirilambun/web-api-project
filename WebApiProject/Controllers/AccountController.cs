using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly IRoleServices _roleServices;
        private readonly ITokenServices _tokenServices;

        public AccountController(IUserServices userServices, IRoleServices roleServices, ITokenServices tokenServices)
        {
            _userServices = userServices;
            _roleServices = roleServices;
            _tokenServices = tokenServices;
        }

        [AllowAnonymous]
        [HttpPost("register-user")]
        public async Task<IActionResult> Register([FromBody] RegisterData model)
        {
            var user = await _userServices.RegisterAsync(model);
            var role = await _roleServices.SignUserToRole(user, model.role);

            return Ok(new
            {
                message = "User registered successfully.",
                user = new UserResponseDTO
                {
                    id = user.Id,
                    username = user.UserName,
                    name = user.name,
                    slug = user.slug,
                    twoFactorEnabled = user.TwoFactorEnabled,
                    emailConfirmed = user.EmailConfirmed,
                    pohneNumberConfirmed = user.PhoneNumberConfirmed
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("login-user")]
        public async Task<IActionResult> Login([FromBody] LoginData data)
        {
            var user = await _userServices.LoginAsync(data);
            var roles = await _roleServices.GetUserRoles(user);

            var token = _tokenServices.GenerateAccessToken(user, roles);

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600,
                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    name = user.name,
                    phone_number = user.phone_number,
                    slug = user.slug,
                    roles = roles
                }
            });
        }

        [Authorize]
        [HttpPost("logout-user")]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var expiryClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
            if(jti == null || expiryClaim == null)
            {
                return BadRequest(new { message = "Invalid token." });
            }

            var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryClaim)).UtcDateTime;
            await _tokenServices.RevokeTokenAsync(jti, expiry);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("generate-all-role")]
        public async Task<IActionResult> GenerateAllRoles()
        {
            try
            {
                List<IdentityRole> role_result = await _roleServices.GenerateAllRoles();
                return Ok(new { message = "Roles created successfully.", data = role_result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("jwt-key")]
        public async Task<IActionResult> GenerateJWTKey()
        {
            string jwt_key = _tokenServices.JwtKeyGenerate();
            return Ok(new { message = "Jwt Key berhasil di generate", key = jwt_key });
        }
    }
}