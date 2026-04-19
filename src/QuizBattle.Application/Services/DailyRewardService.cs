using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Services;

public class DailyRewardService : IDailyRewardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DailyRewardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DailyRewardStatusDto> GetStatusAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
            throw new Exception("کاربر یافت نشد");

        var rewards = await _unitOfWork.Repository<DailyReward>()
            .Query()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Day)
            .ToListAsync();

        if (!rewards.Any())
        {
            rewards = await SeedDefaultDailyRewardsAsync();
        }

        var userRewards = await _unitOfWork.Repository<UserDailyReward>()
            .Query()
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var canClaimToday = user.LastDailyRewardDate?.Date != today;
        var rewardCount = rewards.Count > 0 ? rewards.Count : 20;
        var nextRewardDay = ((user.CurrentStreak % rewardCount) + 1);

        var rewardDtos = rewards.Select(r => new DailyRewardDto(
            r.Day,
            r.CoinReward,
            r.SpecialReward,
            canClaimToday && r.Day == nextRewardDay,
            r.Day < nextRewardDay
        )).ToList();

        return new DailyRewardStatusDto(
            user.CurrentStreak,
            rewardDtos,
            nextRewardDay
        );
    }

    public async Task<ClaimDailyRewardResultDto> ClaimRewardAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
            throw new Exception("کاربر یافت نشد");

        var rewards = await _unitOfWork.Repository<DailyReward>()
            .Query()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Day)
            .ToListAsync();

        if (!rewards.Any())
        {
            rewards = await SeedDefaultDailyRewardsAsync();
        }

        var today = DateTime.UtcNow.Date;

        if (user.LastDailyRewardDate?.Date == today)
        {
            return new ClaimDailyRewardResultDto(false, 0, null, user.CurrentStreak);
        }

        var yesterday = today.AddDays(-1);
        if (user.LastDailyRewardDate?.Date != yesterday)
        {
            user.CurrentStreak = 0;
        }

        user.CurrentStreak++;
        var rewardDay = ((user.CurrentStreak - 1) % rewards.Count) + 1;

        var reward = await _unitOfWork.Repository<DailyReward>()
            .FirstOrDefaultAsync(r => r.Day == rewardDay && r.IsActive);

        if (reward == null)
        {
            reward = rewards.FirstOrDefault(r => r.Day == rewardDay);
        }

        if (reward == null)
        {
            return new ClaimDailyRewardResultDto(false, 0, null, user.CurrentStreak);
        }

        user.Coins += reward.CoinReward;
        user.LastDailyRewardDate = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<User>().UpdateAsync(user);

        var userReward = new UserDailyReward
        {
            UserId = userId,
            DailyRewardId = reward.Id,
            CoinsReceived = reward.CoinReward
        };

        await _unitOfWork.Repository<UserDailyReward>().AddAsync(userReward);
        await _unitOfWork.SaveChangesAsync();

        return new ClaimDailyRewardResultDto(
            true,
            reward.CoinReward,
            reward.SpecialReward,
            user.CurrentStreak
        );
    }

    private async Task<List<DailyReward>> SeedDefaultDailyRewardsAsync()
    {
        var defaultRewards = new List<DailyReward>
        {
            new DailyReward { Day = 1, CoinReward = 10 },
            new DailyReward { Day = 2, CoinReward = 15 },
            new DailyReward { Day = 3, CoinReward = 20 },
            new DailyReward { Day = 4, CoinReward = 25 },
            new DailyReward { Day = 5, CoinReward = 30 },
            new DailyReward { Day = 6, CoinReward = 40 },
            new DailyReward { Day = 7, CoinReward = 50, SpecialReward = "⭐" },
            new DailyReward { Day = 8, CoinReward = 55 },
            new DailyReward { Day = 9, CoinReward = 60 },
            new DailyReward { Day = 10, CoinReward = 70 },
            new DailyReward { Day = 11, CoinReward = 80 },
            new DailyReward { Day = 12, CoinReward = 90 },
            new DailyReward { Day = 13, CoinReward = 100, SpecialReward = "💎" },
            new DailyReward { Day = 14, CoinReward = 110 },
            new DailyReward { Day = 15, CoinReward = 120 },
            new DailyReward { Day = 16, CoinReward = 130 },
            new DailyReward { Day = 17, CoinReward = 140 },
            new DailyReward { Day = 18, CoinReward = 150 },
            new DailyReward { Day = 19, CoinReward = 175 },
            new DailyReward { Day = 20, CoinReward = 200, SpecialReward = "🏆" }
        };

        foreach (var reward in defaultRewards)
        {
            await _unitOfWork.Repository<DailyReward>().AddAsync(reward);
        }

        await _unitOfWork.SaveChangesAsync();
        return defaultRewards;
    }
}
