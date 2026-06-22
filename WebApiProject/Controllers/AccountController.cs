using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Interfaces;
using WebApiProject.Models;
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
            try
            {
                var user = await _userServices.RegisterAsync(model);
                var role = await _roleServices.SignUserToRole(user, model.role);

                return Ok(new { message = "User registered successfully.", user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // [AllowAnonymous]
        // [HttpPost("login-user")]
        // public async Task<IActionResult> Login([FromBody] LoginData data)
        // {
            
        // }

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