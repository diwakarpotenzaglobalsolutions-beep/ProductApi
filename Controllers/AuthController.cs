using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using MyMvcApp.Services;

namespace MyMvcApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string ResetCookieName = "password-reset-session";

        private CookieOptions ResetCookieOptions() => new()
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/Auth",
            MaxAge = TimeSpan.FromMinutes(5),
            IsEssential = true
        };

        private readonly IAuthServices service;
        public AuthController(IAuthServices services1)
        {
            this.service = services1;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<ActionResult<UserDataDto>> Register(RegisterDto request)
        {
            var user = await service.RegisterAsync(request);
            if (user is null)
                return Conflict(ApiErrorResponse.Create(409, "Email already exists"));
            return Ok(new
            {
                success=true,
                message="User register successfully",
                data=user
            });    
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
        {
            var result = await service.LoginAsync(request);
            if (result is null)
                return NotFound(ApiErrorResponse.Create(404, "Email or password is wrong"));
            return Ok(
                new
                {
                    success=true,
                    message="Login successfully",
                    data=result
                }
            );
        }

        [AllowAnonymous]
        [HttpPost("Refresh-Token")]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken(RefreshTokenDto request)
        {
            var result = await service.RefreshTokenAsync(request);
            if (result is null)
                return Unauthorized(ApiErrorResponse.Create(401, "Invalid or expired refresh token"));
            return Ok(result);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("GetUserProfile")]
        public async Task<ActionResult<UserDataDto>> GetUserProfileData()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var result = await service.GetUserProfileData(userId);

            if (result is null)
                return NotFound(ApiErrorResponse.Create(404, "User profile not found."));

            return Ok(new
            {
                success = true,
                message = "Profile featch successfully",
                data = result
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("Upload-Profile-Image")]
        [Consumes("multipart/form-data")]

        public async Task<ActionResult?> UploadProfileImage([FromForm] UploadProfileImageDto profileImageDto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var imageUrl = await service.UploadProfileImageAsync(userId, profileImageDto.ProfileImage);

            if (imageUrl is null)
                return NotFound(ApiErrorResponse.Create(404, "User not found."));

            return Ok(new
            {

                success = true,
                message = "Profile image uploaded successfully.",
                data = new { profileImage = imageUrl }

            });
        }




        [AllowAnonymous]
        [HttpPost("Forgot-Password")]



        public async Task<ActionResult> ForGotPassword(
    [FromBody] ForGotPasswordDTO forGotPasswordDTO)
        {
            var result = await service.ForGotPasswordAsync(forGotPasswordDTO);

            if (result is null)
            {
                return NotFound(ApiErrorResponse.Create(404, "No account found with the provided email address."));
            }

            Response.Cookies.Append(ResetCookieName, result, ResetCookieOptions());
            return Ok(new
            {
                success = true,
                message = "OTP sent successfully. Please check your email inbox."
            });
        }


        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO verifyOtpDTO)
        {
            var result = await service.VerifyOtpAsync(verifyOtpDTO, Request.Cookies[ResetCookieName]);

            if (result is null)
                return BadRequest(ApiErrorResponse.Create(400, "Invalid or expired OTP"));

            Response.Cookies.Append(ResetCookieName, result, ResetCookieOptions());
            return Ok(new
            {
                success = true,
                message = "OTP verified successfully"
            });
        }


        [AllowAnonymous]
        [HttpPost("Reset-Password")]

        public async Task<IActionResult> ReSetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            var result = await service.ResetPasswordAsync(resetPasswordDTO, Request.Cookies[ResetCookieName]);
            if (!result)
                return BadRequest(ApiErrorResponse.Create(400, "Invalid request"));

            Response.Cookies.Delete(ResetCookieName, ResetCookieOptions());
            return Ok(new
            {
                success = true,
                message = "Password reset successfully"
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("Change-Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));
            var result = await service.ChangePasswordAsync(userId, changePasswordDTO);

            if (!result)
                return BadRequest(ApiErrorResponse.Create(400, "Current password is incorrect or has already changed."));

            return Ok(new
            {
                success = true,
                message = "Password changed successfully."
            });
        }


        [Authorize(Roles = "Admin,User")]
        [HttpPost("Update-Profile")]

        public async Task<IActionResult> UpdateProfile([FromBody] EditProfileDTO editProfileDTO)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userID))
                return Unauthorized(ApiErrorResponse.Create(401));
            var result = await service.EditProfileAsync(editProfileDTO, userID);
            if (!result)
                return NotFound(ApiErrorResponse.Create(404, "User Not Found"));

            return Ok(new
            {
                success = true,
                message = "Profile update successfully",
                data = result
            });
        }





    }
}
