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