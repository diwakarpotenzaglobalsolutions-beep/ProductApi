using System;
using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Entities;

public class User
{
  [Key]
  public Guid ID{set;get;}
  [Required]
  [MaxLength(10)]
  public string UserName { get; set; }=string.Empty;

  [Required]
  [EmailAddress]
  public string Email { get; set; }=string.Empty;
  [Required]
  public string PasswordHash { get; set; }=string.Empty;
 [Required]
  [MaxLength(10)]
  public string PhoneNumber { get; set; }=string.Empty;

  public string Roles { get; set; } = "User";

 public string? ResetOtpHash { get; set; }
 public string? ResetSessionHash { get; set; }
public DateTime? ResetOtpExpiry { get; set; }
public bool IsOtpVerified { get; set; } = false;
public int OtpFailedAttempts { get; set; } = 0;

  public string RefreshToken { get; set; }=string.Empty;

  public DateTime RefreshTokenExpiry {get;set;} = DateTime.UtcNow;

  public string? ProfileImage { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
}
