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

        public AccountController(IUserServices userServices)
        {
            _userServices = userServices;
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
            LoginResponseDTO login_response = await _userServices.LoginAsync(data);
            return Ok(new GlobalJsonResponse<LoginResponseDTO>(
                message: "User logged in successfully.",
                data: login_response
            ));
        }

        [Authorize]
        [HttpPost("logout-user")]
        public async Task<IActionResult> Logout()
        {
            await _userServices.LogoutAsync();
            return NoContent();
        }
    }
}