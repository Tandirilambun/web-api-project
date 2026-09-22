using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiProject.Interfaces;

namespace WebApiProject.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilController : ControllerBase
    {
        private readonly IRoleServices _roleServices;
        private readonly ITokenServices _tokenServices;

        public UtilController(IRoleServices roleServices, ITokenServices tokenServices)
        {
            _roleServices = roleServices;
            _tokenServices = tokenServices;
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
