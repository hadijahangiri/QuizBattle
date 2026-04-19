using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize(Policy = "AdminOnly")]
public class GameSettingsController : Controller
{
    private readonly IGameSettingsService _gameSettingsService;

    public GameSettingsController(IGameSettingsService gameSettingsService)
    {
        _gameSettingsService = gameSettingsService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _gameSettingsService.GetSettingsAsync();
        ViewData["Title"] = "تنظیمات بازی";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(GameSettingsDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تنظیمات بازی";
            return View(model);
        }

        if (model.OpponentResponseTimeoutHours < 1)
        {
            ModelState.AddModelError(nameof(model.OpponentResponseTimeoutHours), "زمان باید حداقل 1 ساعت باشد.");
            ViewData["Title"] = "تنظیمات بازی";
            return View(model);
        }

        var success = await _gameSettingsService.UpdateSettingsAsync(model);
        if (!success)
        {
            TempData["Error"] = "ذخیره تنظیمات با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "تنظیمات بازی با موفقیت ذخیره شد.";
        }

        return RedirectToAction(nameof(Index));
    }
}
