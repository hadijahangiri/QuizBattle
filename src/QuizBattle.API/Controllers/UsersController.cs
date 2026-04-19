using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.API.Extensions;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// دریافت پروفایل کاربر جاری
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = User.GetRequiredUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// دریافت پروفایل کامل کاربر جاری
    /// </summary>
    [HttpGet("me/profile")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = User.GetRequiredUserId();
        var profile = await _userService.GetProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    /// <summary>
    /// دریافت پروفایل عمومی کاربر دیگر (فقط اطلاعات عمومی)
    /// </summary>
    [HttpGet("{id}/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(Guid id)
    {
        var profile = await _userService.GetProfileAsync(id);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    /// <summary>
    /// بروزرسانی پروفایل کاربر جاری
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserDto dto)
    {
        var userId = User.GetRequiredUserId();
        try
        {
            var user = await _userService.UpdateAsync(userId, dto);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// تبدیل کاربر مهمان به کاربر عادی
    /// </summary>
    [HttpPost("me/convert-to-regular")]
    public async Task<ActionResult<UserDto>> ConvertToRegular([FromBody] ConvertGuestRequest request)
    {
        var userId = User.GetRequiredUserId();
        try
        {
            var user = await _userService.ConvertGuestToRegularAsync(userId, request.Email, request.PhoneNumber);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// جستجوی کاربران
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<UserProfileDto>>> Search([FromQuery] string query, [FromQuery] int limit = 10)
    {
        var users = await _userService.SearchUsersAsync(query, limit);
        return Ok(users);
    }

    /// <summary>
    /// علامت‌گذاری آموزش به عنوان تکمیل شده
    /// </summary>
    [HttpPost("me/tutorial-completed")]
    public async Task<IActionResult> MarkTutorialCompleted()
    {
        var userId = User.GetRequiredUserId();
        await _userService.MarkTutorialCompletedAsync(userId);
        return NoContent();
    }
}

public record ConvertGuestRequest(string? Email, string? PhoneNumber);
