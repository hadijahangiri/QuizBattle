using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    /// <summary>
    /// ورود کاربر مهمان - ایجاد حساب جدید و صدور توکن
    /// </summary>
    [HttpPost("guest")]
    public async Task<ActionResult<AuthResultDto>> LoginAsGuest([FromBody] GuestLoginRequest request)
    {
        var user = await _userService.CreateGuestAsync(request.Username, request.AvatarUrl);
        var token = GenerateJwtToken(user.Id, user.Username);
        
        return Ok(new AuthResultDto(user.Id, user.Username, user.AvatarUrl, token));
    }

    /// <summary>
    /// ورود با شماره موبایل یا ایمیل (برای کاربران ثبت‌نام شده)
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequest request)
    {
        // تلاش برای پیدا کردن کاربر با ایمیل یا شماره موبایل
        var user = await _userService.GetByEmailAsync(request.EmailOrPhone)
                ?? await _userService.GetByPhoneAsync(request.EmailOrPhone);
        
        if (user == null)
            return Unauthorized("کاربر یافت نشد");

        // TODO: اضافه کردن تأیید رمز عبور در صورت نیاز
        var token = GenerateJwtToken(user.Id, user.Username);
        
        return Ok(new AuthResultDto(user.Id, user.Username, user.AvatarUrl ?? "", token));
    }

    /// <summary>
    /// دریافت توکن جدید برای کاربر موجود (refresh)
    /// </summary>
    [HttpPost("refresh/{userId}")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound("کاربر یافت نشد");

        var token = GenerateJwtToken(user.Id, user.Username);
        
        return Ok(new AuthResultDto(user.Id, user.Username, user.AvatarUrl ?? "", token));
    }

    private string GenerateJwtToken(int userId, string username)
    {
        var key = _configuration["Jwt:Key"] ?? "QuizBattleSecretKey123456789012345678901234";
        var issuer = _configuration["Jwt:Issuer"] ?? "QuizBattle";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30), // توکن 30 روزه
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// DTOs
public record GuestLoginRequest(string Username, string AvatarUrl);
public record LoginRequest(string EmailOrPhone);
public record AuthResultDto(int UserId, string Username, string AvatarUrl, string Token);
