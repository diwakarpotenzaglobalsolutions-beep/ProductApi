using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using MyMvcApp.Services;

namespace MyMvcApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleAssign : ControllerBase
    {

        private readonly IAuthServices service;
        public RoleAssign(IAuthServices services1)
        {
            this.service = services1;
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("Role-Assign")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignDTO dto, CancellationToken cancellationToken = default)
        {

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userID))
                return Unauthorized(ApiErrorResponse.Create(401));
            if (dto.UserId == Guid.Empty)
                return BadRequest(ApiErrorResponse.Create(400, "A target userId is required."));
            var result = await service.RoleAsignAsync(dto.UserId, dto, cancellationToken);

            if (!result)
            {
                return NotFound(ApiErrorResponse.Create(404, "User not found."));
            }

            return Ok(new
            {
                success = true,
                message = "Role assigned successfully",
                data = result
            });
        }




        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken = default)
        {
            var users = await service.GetAllUserAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Users retrieved successfully.",
                data = users
            });
        }



    }
}
