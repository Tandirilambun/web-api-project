using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Exceptions;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly ITokenServices _tokenServices;

        public AccountController(IUserServices userServices, ITokenServices tokenServices)
        {
            _userServices = userServices;
            _tokenServices = tokenServices;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public async Task<IActionResult> Login()
        {
            return Ok();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new GlobalJsonResponse<object>(message: "Unauthorized", data: null));

            var user = await _userServices.GetUserByIdAsync(userId);
            return Ok(new GlobalJsonResponse<UserProfileDTO>(
                message: "User profile retrieved successfully.",
                data: user
            ));
        }

        [AllowAnonymous]
        [HttpPost("register-user")]
        public async Task<IActionResult> Register([FromBody] RegisterData model)
        {
            UserResponseDTO user = await _userServices.RegisterAsync(model);
            return Ok(new GlobalJsonResponse<UserResponseDTO>(
                message: "User registered successfully.",
                data: user
            ));
        }

        [AllowAnonymous]
        [HttpPost("login-user")]
        public async Task<IActionResult> Login([FromBody] LoginData data)
        {
            LoginResponse login_response = await _userServices.LoginAsync(data);
            Response.Cookies.Append("refresh_token", login_response.refresh_token, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Ok(new GlobalJsonResponse<LoginResponseDTO>(
                message: "User logged in successfully.",
                data: login_response.response
            ));
        }

        [HttpPost("test-endpoint")]
        public async Task<IActionResult> TestEndpoint()
        {
            var userId = User;
            return Ok(new { message = "Test endpoint accessed successfully.", userId });
        }

        [Authorize]
        [HttpPost("logout-user")]
        public async Task<IActionResult> Logout()
        {
            Request.Cookies.TryGetValue("refresh_token", out var rawRefreshToken);
            await _userServices.LogoutAsync(rawRefreshToken);
            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            }); // harus pakai options yang SAMA (path/domain) dengan saat Set-Cookie, kalau tidak, Delete tidak akan match

            return NoContent();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var test = User.FindFirst("sub")?.Value;
            var test2 = User;
            if (!Request.Cookies.TryGetValue("refresh_token", out var rawToken))
                return Unauthorized(new { message = "No refresh token provided" });
            try
            {
                var result = await _tokenServices.RefreshNewToken(rawToken);

                Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Ok(result);
            }
            catch (ValidationExceptions ex)
            {
                Response.Cookies.Delete("refresh_token"); // paksa client re-login kalau refresh gagal
                return Unauthorized(new { message = ex.Message });
            }
        }

    }
}