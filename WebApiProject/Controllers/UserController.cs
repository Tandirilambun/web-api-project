using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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
            var userId = await _userService.IsUserAuthorized();
            var userInfo = await _userService.GetUserByIdAsync(userId);
            return Ok(new GlobalJsonResponse<UserProfileDTO>(
                message: "User information retrieved successfully", 
                data: userInfo
            ));
        }

        [HttpPatch("update-user-info")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] Dictionary<string, JsonElement> updatedFields)
        {
            var userId = await _userService.IsUserAuthorized();
            UserProfileDTO updatedUserInfo = await _userService.UpdateUserAsync(userId, updatedFields);

            return Ok(new GlobalJsonResponse<UserProfileDTO>(
                message: "User information updated successfully",
                data: updatedUserInfo
            ));
        }
    }
}