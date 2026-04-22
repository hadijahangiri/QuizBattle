using System.Security.Claims;

namespace QuizBattle.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    public static int GetCurrentUserId(this ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("کاربر احراز هویت نشده است");
        return userId.Value;
    }

    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }
}
