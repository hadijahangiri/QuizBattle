using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.AdminPanel.Models;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var usersRepo = _unitOfWork.Repository<User>();
        var questionsRepo = _unitOfWork.Repository<Question>();
        var gamesRepo = _unitOfWork.Repository<Game>();
        var categoriesRepo = _unitOfWork.Repository<Category>();

        var users = await usersRepo.GetAllAsync();
        var questions = await questionsRepo.GetAllAsync();
        var games = await gamesRepo.GetAllAsync();
        var categories = await categoriesRepo.GetAllAsync();

        var today = DateTime.Today;
        var weekAgo = DateTime.Today.AddDays(-7);

        var viewModel = new DashboardViewModel
        {
            TotalUsers = users.Count(),
            TotalQuestions = questions.Count(),
            TotalGames = games.Count(),
            TotalCategories = categories.Count(),
            ActiveGames = games.Count(g => g.Status == GameStatus.InProgress),
            ReportedQuestions = questions.Count(q => q.ReportCount > 0),
            TodaysGames = games.Count(g => g.CreatedAt.Date == today),
            NewUsersThisWeek = users.Count(u => u.CreatedAt >= weekAgo),
            RecentGames = games
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .Select(g => new RecentGameViewModel
                {
                    Id = g.Id,
                    Player1Name = g.Player1?.Username ?? "نامشخص",
                    Player2Name = g.Player2?.Username ?? "نامشخص",
                    Player1Score = g.Player1Score,
                    Player2Score = g.Player2Score,
                    CreatedAt = g.CreatedAt,
                    Status = g.Status
                }).ToList(),
            TopUsers = users
                .OrderByDescending(u => u.TotalWins)
                .Take(5)
                .Select(u => new TopUserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl ?? "",
                    TotalWins = u.TotalWins,
                    TotalGames = u.TotalWins + u.TotalLosses,
                    Coins = u.Coins
                }).ToList(),
            GamesPerDay = games
                .Where(g => g.CreatedAt >= weekAgo)
                .GroupBy(g => g.CreatedAt.Date.ToString("MM/dd"))
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return View(viewModel);
    }
}
