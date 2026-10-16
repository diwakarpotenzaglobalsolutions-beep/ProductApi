using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Models;

public class RegisterDto
{
    [Required]
    [MaxLength(10)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{8,128}$",
        ErrorMessage = "Password must be 8-128 characters and contain uppercase, lowercase, a number, and a special character. Spaces are not allowed."
    )]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserDataDto
{
    public string Role { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class LoginResponseDto : UserDataDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}



public class UploadProfileImageDto
{
    [Required]
    public IFormFile ProfileImage{get;set;}=null!;
}

 
public class ForGotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email {set;get;}=string.Empty;
}

public class VerifyOtpDTO
{
    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Otp { get; set; }=string.Empty;
}

public class ResetPasswordDTO
 { 

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{8,128}$",
        ErrorMessage = "Password must be 8-128 characters and contain uppercase, lowercase, a number, and a special character. Spaces are not allowed.")]
    public string Password { get; set; } = string.Empty;
}


public class ChangePasswordDTO
{
    [Required]
    public string CurrentPassword { get; set; }=string.Empty;

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{8,128}$",
        ErrorMessage = "Password must be 8-128 characters and contain uppercase, lowercase, a number, and a special character. Spaces are not allowed.")]

    public string  NewPassword{ get; set; }=string.Empty;
}


public class EditProfileDTO
{
    public string UserName { get; set; }=string.Empty;
    public string PhoneNumber{get;set;}=string.Empty;
}


public class RoleAssignDTO
{
    [Required]
    public Guid UserId { get; set; }
    [Required]
    [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be Admin or User.")]
    public  string RoleName { get; set; }=string.Empty;
}


public class AllUserDTO
{

    public Guid ID { get; set; }
    public string UserName { get; set; }=string.Empty;
    public string RoleName { get; set; }=string.Empty;
    public string UserImage { get; set; }=string.Empty;
}


