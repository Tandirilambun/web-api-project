using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Controller
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {

        private IUserServices _userService;

        public UserController(IUserServices userService)
        {
            _userService = userService;
        }

        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var test = User;
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Retrieve user information from the database using the userId
            // For example, you can use a UserService to get the user details
            var userInfo = await _userService.GetUserByIdAsync(userId);
            return Ok(new GlobalJsonResponse<UserProfileDTO>(
                message: "User information retrieved successfully", 
                data: userInfo
            ));
        }
    }


}