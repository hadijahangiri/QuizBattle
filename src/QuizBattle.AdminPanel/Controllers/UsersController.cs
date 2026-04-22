using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.AdminPanel.Models;
using QuizBattle.AdminPanel.Services;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IApiClient _apiClient;

    public UsersController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        const int pageSize = 20;
        var allUsers = await _apiClient.GetAllUsersAsync();

        // Apply search filter
        var users = allUsers.AsEnumerable();
        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u =>
                u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(search)));
        }

        var totalCount = users.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userList = users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                Coins = u.Coins,
                TotalWins = u.TotalWins,
                TotalLosses = u.TotalLosses,
                TotalGames = u.TotalWins + u.TotalLosses,
                Level = u.Level,
                IsActive = true,
                IsGuest = u.IsGuest,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList();

        var viewModel = new UserListViewModel
        {
            Users = userList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            SearchTerm = search
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _apiClient.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new UserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Coins = user.Coins,
            TotalWins = user.TotalWins,
            TotalLosses = user.TotalLosses,
            TotalGames = user.TotalWins + user.TotalLosses,
            Level = user.Level,
            IsActive = true,
            IsGuest = user.IsGuest,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return View(viewModel);
    }
}
