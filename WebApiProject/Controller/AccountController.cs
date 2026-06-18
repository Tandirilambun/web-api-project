using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Models;
using WebApiProject.Models.ViewModels;

namespace WebApiProject.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Implement your account-related actions here (e.g., Register, Login, Logout)

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserViewModel model)
        {
            return Ok("User registered successfully.");
            // var user = new User { UserName = email, Email = email, name = name };
            // var result = await _userManager.CreateAsync(user, password);

            // if (result.Succeeded)
            // {
            // }
            // else
            // {
            //     return BadRequest(result.Errors);
            // }
        }
    }
}