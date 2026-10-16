using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyMvcApp.Data;
using MyMvcApp.Entities;
using MyMvcApp.Models;

namespace MyMvcApp.Services;



public interface IAuthServices
{
    Task<UserDataDto?> GetUserProfileData(Guid userID, CancellationToken cancellationToken = default);
    Task<LoginResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);
    Task<UserDataDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    Task<string?> UploadProfileImageAsync(Guid userID, IFormFile profileImage, CancellationToken cancellationToken = default);

    Task<string?> ForGotPasswordAsync(ForGotPasswordDTO dto, CancellationToken cancellationToken = default);

    Task<string?> VerifyOtpAsync(VerifyOtpDTO dto, string? resetSession, CancellationToken cancellationToken = default);

    Task<bool> ResetPasswordAsync(ResetPasswordDTO dto, string? resetSession, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userID, ChangePasswordDTO dto, CancellationToken cancellationToken = default);

    Task<bool> EditProfileAsync(EditProfileDTO dto, Guid userID, CancellationToken cancellationToken = default);

    Task<bool> RoleAsignAsync(Guid guid, RoleAssignDTO roleAssignDTO, CancellationToken cancellationToken = default);

    Task<List<AllUserDTO>> GetAllUserAsync(CancellationToken cancellationToken = default);
}

public class AuthServices : IAuthServices
{
    private readonly IConfiguration configuration;
    private readonly MyDbContext context;
    private readonly IEmailService emailService;
    public AuthServices(IConfiguration configuration1, MyDbContext context1, IEmailService emailService)
    {
        this.configuration = configuration1;
        this.context = context1;
        this.emailService = emailService;
    }



    public async Task<UserDataDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await context.users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return null;
        var user = new User();
        user.Roles = "User";
        user.UserName = request.UserName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await context.users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToUserData(user);
    }


    public async Task<LoginResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        User? user = await context.users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
            return null;

        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return null;

        return new LoginResponseDto
        {
            Id = user.ID,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            Role = string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles,
            Token = CreateToken(user),
            RefreshToken = await GenrateAndSaveRefreshToken(user, cancellationToken),
        };
    }


    public async Task<UserDataDto?> GetUserProfileData(Guid userID, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.users.AsNoTracking()
            .Where(user => user.ID == userID)
            .Select(user => new UserDataDto
            {
                Id = user.ID,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                ProfileImage = user.ProfileImage,
                Role = string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }




    public async Task<string?> UploadProfileImageAsync(Guid userID, IFormFile profileImage, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (profileImage == null || profileImage.Length == 0)
            throw new RequestValidationException("Please select an image.");

        const long maxFileSize = 5 * 1024 * 1024;

        if (profileImage.Length > maxFileSize)
            throw new RequestValidationException("Image size must be under 5 MB.", StatusCodes.Status413PayloadTooLarge);


        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        if (!allowedTypes.Contains(profileImage.ContentType))
            throw new RequestValidationException("Only JPG, PNG and WebP images are allowed.", StatusCodes.Status415UnsupportedMediaType);




        var user = await context.users.FirstOrDefaultAsync(u => u.ID == userID, cancellationToken);

        if (user == null)
            return null;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),
        "wwwroot",
        "uploads",
        "profiles"
        );

        Directory.CreateDirectory(uploadsFolder);


        var extension = profileImage.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new RequestValidationException("Invalid image type.")
        };

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var strem = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(strem, cancellationToken);
        }

        user.ProfileImage = $"/uploads/profiles/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return user.ProfileImage;

    }





    private static UserDataDto ToUserData(User user)
    {
        return new UserDataDto
        {
            Id = user.ID,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            Role = string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };
    }




    private async Task<string> GenrateAndSaveRefreshToken(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);
        await context.SaveChangesAsync(cancellationToken);
        return refreshToken;

    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        User? user = await context.users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);
        if (user is null || user.RefreshTokenExpiry <= DateTime.UtcNow)
            return null;

        return new LoginResponseDto
        {
            Id = user.ID,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            Role = string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles,
            Token = CreateToken(user),
            RefreshToken = await GenrateAndSaveRefreshToken(user, cancellationToken)
        };
    }


    private string CreateToken(User user)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.NameIdentifier,user.ID.ToString()),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles)
            };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("AppSettings:Issuer"),
            audience: configuration.GetValue<string>("AppSettings:audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }



    private string HasOtp(string email, string otp)
    {
        var secret = configuration["PASSWORD_RESET_OTP_SECRET"]
            ?? configuration["PasswordReset:OtpSecret"]
            ?? configuration["AppSettings:Token"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Configure PasswordReset:OtpSecret or PASSWORD_RESET_OTP_SECRET.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        var data = Encoding.UTF8.GetBytes($"password-reset:{email.Trim().ToLowerInvariant()}:{otp}");

        return Convert.ToHexString(hmac.ComputeHash(data));
    }

    private bool IsValidOtp(User user, string otp) =>
        user.ResetOtpHash is not null && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(user.ResetOtpHash),
            Convert.FromHexString(HasOtp(user.Email, otp)));

    public async Task<string?> ForGotPasswordAsync(ForGotPasswordDTO forGotPasswordDTO, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await context.users.FirstOrDefaultAsync(u => u.Email == forGotPasswordDTO.Email, cancellationToken);
        if (user == null)
            return null;

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var resetSession = CreateResetSession();

        user.ResetOtpHash = HasOtp(user.Email, otp);
        user.ResetSessionHash = HashResetSession(resetSession);
        user.ResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);
        user.IsOtpVerified = false;
        user.OtpFailedAttempts = 0;

        await emailService.SendEmailAsync(
            user.Email,
            "Password Reset OTP",
            $"Your OTP is {otp}. It expires in 5 minutes.",
            cancellationToken
        );
        await context.SaveChangesAsync(cancellationToken);

        return resetSession;
    }





    private static string CreateResetSession() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string HashResetSession(string resetSession) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetSession)));

    private IQueryable<User>? GetResetSessionQuery(string? resetSession, bool verified)
    {
        if (string.IsNullOrWhiteSpace(resetSession) || resetSession.Length != 64)
            return null;

        var sessionHash = HashResetSession(resetSession);
        var now = DateTime.UtcNow;
        return context.users.Where(user => user.ResetSessionHash == sessionHash
            && user.IsOtpVerified == verified && user.ResetOtpExpiry > now
            && user.OtpFailedAttempts < 5);
    }

    public async Task<string?> VerifyOtpAsync(VerifyOtpDTO dto, string? resetSession, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionUsers = GetResetSessionQuery(resetSession, verified: false);
        if (sessionUsers is null)
            return null;

        var user = await sessionUsers.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (user is null)
            return null;

        if (!IsValidOtp(user, dto.Otp))
        {
            await sessionUsers.Where(candidate => candidate.ID == user.ID)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(candidate => candidate.OtpFailedAttempts, candidate => candidate.OtpFailedAttempts + 1), cancellationToken);
            return null;
        }

        var verifiedSession = CreateResetSession();
        var verifiedHash = HashResetSession(verifiedSession);
        // Conditional updates consume the OTP only once, even for concurrent requests.
        var updated = await sessionUsers
            .Where(candidate => candidate.ID == user.ID && candidate.ResetOtpHash == user.ResetOtpHash)
            .ExecuteUpdateAsync(update => update
                .SetProperty(candidate => candidate.IsOtpVerified, true)
                .SetProperty(candidate => candidate.ResetSessionHash, verifiedHash)
                .SetProperty(candidate => candidate.ResetOtpHash, (string?)null), cancellationToken);
        return updated == 1 ? verifiedSession : null;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDTO dto, string? resetSession, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionUsers = GetResetSessionQuery(resetSession, verified: true);
        if (sessionUsers is null)
            return false;

        var user = await sessionUsers.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (user is null)
            return false;

        var passwordHash = new PasswordHasher<User>().HashPassword(user, dto.Password);
        var now = DateTime.UtcNow;
        // Keep the session condition in the write so concurrent resets cannot both succeed.
        return await sessionUsers.Where(candidate => candidate.ID == user.ID)
            .ExecuteUpdateAsync(update => update
                .SetProperty(candidate => candidate.PasswordHash, passwordHash)
                .SetProperty(candidate => candidate.RefreshToken, string.Empty)
                .SetProperty(candidate => candidate.RefreshTokenExpiry, now)
                .SetProperty(candidate => candidate.ResetSessionHash, (string?)null)
                .SetProperty(candidate => candidate.ResetOtpHash, (string?)null)
                .SetProperty(candidate => candidate.ResetOtpExpiry, (DateTime?)null)
                .SetProperty(candidate => candidate.IsOtpVerified, false)
                .SetProperty(candidate => candidate.OtpFailedAttempts, 0)
                .SetProperty(candidate => candidate.UpdatedAt, now), cancellationToken) == 1;
    }

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            600000,
            HashAlgorithmName.SHA256,
            32
        );
        return $"600000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }


    public async Task<bool> ChangePasswordAsync(Guid userID, ChangePasswordDTO changePasswordDTO, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await context.users.FirstOrDefaultAsync(u => u.ID == userID, cancellationToken);
        if (user is null)
            return false;

        var hasher = new PasswordHasher<User>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, changePasswordDTO.CurrentPassword) == PasswordVerificationResult.Failed)
            return false;

        user.PasswordHash = hasher.HashPassword(user, changePasswordDTO.NewPassword);
        user.RefreshToken = string.Empty;
        user.RefreshTokenExpiry = DateTime.UtcNow;
        user.ResetSessionHash = null;
        user.ResetOtpHash = null;
        user.ResetOtpExpiry = null;
        user.IsOtpVerified = false;
        user.OtpFailedAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }



    public async Task<bool> EditProfileAsync(EditProfileDTO editProfileDTO, Guid userID, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await context.users.FirstOrDefaultAsync(u => u.ID == userID, cancellationToken);
        if (user is null)
            return false;

        user.UserName = editProfileDTO.UserName;
        user.PhoneNumber = editProfileDTO.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }



    public async Task<bool> RoleAsignAsync(Guid guid, RoleAssignDTO roleAssignDTO, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (roleAssignDTO.RoleName != "Admin" && roleAssignDTO.RoleName != "User")
        {
            return false;
        }

        var user = await context.users.FirstOrDefaultAsync(u => u.ID == guid, cancellationToken);
        if (user is null)
            return false;

        user.Roles = roleAssignDTO.RoleName;
        user.ID = roleAssignDTO.UserId;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }


    public async Task<List<AllUserDTO>> GetAllUserAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = await context.users
            .AsNoTracking()
            .Select(x => new AllUserDTO
            {
                ID = x.ID,
                UserName = x.UserName,
                RoleName = x.Roles,
                UserImage = x.ProfileImage??""
            })
            .ToListAsync(cancellationToken);

        return users;
    }


}


