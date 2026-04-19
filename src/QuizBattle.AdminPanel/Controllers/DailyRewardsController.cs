using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizBattle.AdminPanel.Models;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class DailyRewardsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DailyRewardsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var rewardRepo = _unitOfWork.Repository<DailyReward>();
        var rewards = await rewardRepo.Query().OrderBy(r => r.Day).ToListAsync();

        if (!rewards.Any())
        {
            rewards = await SeedDefaultDailyRewardsAsync();
        }

        var viewModel = new DailyRewardListViewModel
        {
            Rewards = rewards.Select(r => new DailyRewardAdminViewModel
            {
                Id = r.Id,
                Day = r.Day,
                CoinReward = r.CoinReward,
                SpecialReward = r.SpecialReward,
                IsActive = r.IsActive
            }).ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new EditDailyRewardViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EditDailyRewardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rewardRepo = _unitOfWork.Repository<DailyReward>();
        var existing = await rewardRepo.Query().FirstOrDefaultAsync(r => r.Day == model.Day);
        if (existing != null)
        {
            ModelState.AddModelError("Day", "برای این روز قبلاً جایزه‌ای ثبت شده است.");
            return View(model);
        }

        var reward = new DailyReward
        {
            Day = model.Day,
            CoinReward = model.CoinReward,
            SpecialReward = model.SpecialReward,
            IsActive = model.IsActive
        };

        await rewardRepo.AddAsync(reward);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "جایزه روزانه جدید با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var reward = await _unitOfWork.Repository<DailyReward>().GetByIdAsync(id);
        if (reward == null)
        {
            return NotFound();
        }

        return View(new EditDailyRewardViewModel
        {
            Id = reward.Id,
            Day = reward.Day,
            CoinReward = reward.CoinReward,
            SpecialReward = reward.SpecialReward,
            IsActive = reward.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditDailyRewardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reward = await _unitOfWork.Repository<DailyReward>().GetByIdAsync(model.Id);
        if (reward == null)
        {
            return NotFound();
        }

        reward.Day = model.Day;
        reward.CoinReward = model.CoinReward;
        reward.SpecialReward = model.SpecialReward;
        reward.IsActive = model.IsActive;
        reward.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<DailyReward>().UpdateAsync(reward);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "جایزه روزانه با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var reward = await _unitOfWork.Repository<DailyReward>().GetByIdAsync(id);
        if (reward == null)
        {
            return NotFound();
        }

        reward.IsActive = !reward.IsActive;
        reward.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<DailyReward>().UpdateAsync(reward);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"جایزه روزانه {(reward.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var reward = await _unitOfWork.Repository<DailyReward>().GetByIdAsync(id);
        if (reward == null)
        {
            return NotFound();
        }

        await _unitOfWork.Repository<DailyReward>().DeleteAsync(reward);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "جایزه روزانه با موفقیت حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<DailyReward>> SeedDefaultDailyRewardsAsync()
    {
        var rewardRepo = _unitOfWork.Repository<DailyReward>();
        var defaultRewards = new List<DailyReward>
        {
            new DailyReward { Day = 1, CoinReward = 10, IsActive = true },
            new DailyReward { Day = 2, CoinReward = 15, IsActive = true },
            new DailyReward { Day = 3, CoinReward = 20, IsActive = true },
            new DailyReward { Day = 4, CoinReward = 25, IsActive = true },
            new DailyReward { Day = 5, CoinReward = 30, IsActive = true },
            new DailyReward { Day = 6, CoinReward = 40, IsActive = true },
            new DailyReward { Day = 7, CoinReward = 50, SpecialReward = "⭐", IsActive = true },
            new DailyReward { Day = 8, CoinReward = 55, IsActive = true },
            new DailyReward { Day = 9, CoinReward = 60, IsActive = true },
            new DailyReward { Day = 10, CoinReward = 70, IsActive = true },
            new DailyReward { Day = 11, CoinReward = 80, IsActive = true },
            new DailyReward { Day = 12, CoinReward = 90, IsActive = true },
            new DailyReward { Day = 13, CoinReward = 100, SpecialReward = "💎", IsActive = true },
            new DailyReward { Day = 14, CoinReward = 110, IsActive = true },
            new DailyReward { Day = 15, CoinReward = 120, IsActive = true },
            new DailyReward { Day = 16, CoinReward = 130, IsActive = true },
            new DailyReward { Day = 17, CoinReward = 140, IsActive = true },
            new DailyReward { Day = 18, CoinReward = 150, IsActive = true },
            new DailyReward { Day = 19, CoinReward = 175, IsActive = true },
            new DailyReward { Day = 20, CoinReward = 200, SpecialReward = "🏆", IsActive = true }
        };

        foreach (var reward in defaultRewards)
        {
            await rewardRepo.AddAsync(reward);
        }

        await _unitOfWork.SaveChangesAsync();
        return defaultRewards;
    }
}
