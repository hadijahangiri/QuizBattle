using System;

namespace QuizBattle.Shared;

public class AppState
{
    public Guid CurrentUserId { get; set; } = Guid.Empty;
    public string Username { get; set; } = "کاربر QuizBattle";
    public string? AvatarUrl { get; set; } = "icon-192.png";
    public string? UserAvatar { get; set; } = "icon-192.png";
    public bool IsLeader { get; set; } = true;
    public bool HasCompletedTutorial { get; set; }
    public bool HasPlayedDailyChallengeToday { get; set; }
    public bool IsGuest { get; set; }
    public int Coins { get; set; }
    public int UserCoins { get; set; }
    public int Tickets { get; set; }
    public int TotalWins { get; set; }
    public int Level { get; set; } = 1;
    public int WinsTowardsNextLevel { get; set; }
    public int WinsNeededForNextLevel { get; set; } = 10;
    public int? DailyChallengeScore { get; set; }
    public int? DailyChallengeRank { get; set; }
    public DateTime? LastDailyLoginRewardDate { get; set; }
    public int DailyLoginStreak { get; set; }
    
    /// <summary>
    /// JWT Token برای احراز هویت
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// رویداد برای هدایت به صفحه ورود در صورت خطای احراز هویت
    /// </summary>
    public event Action? OnUnauthorized;

    public void NotifyUnauthorized() => OnUnauthorized?.Invoke();

    public event Action? OnChange;

    public bool IsLoggedIn => CurrentUserId != Guid.Empty && !string.IsNullOrEmpty(AuthToken);

    public void NotifyStateChanged() => OnChange?.Invoke();

    public bool AddWin()
    {
        TotalWins++;
        WinsTowardsNextLevel++;

        if (WinsNeededForNextLevel <= 0)
        {
            WinsNeededForNextLevel = 10;
        }

        if (WinsTowardsNextLevel >= WinsNeededForNextLevel)
        {
            WinsTowardsNextLevel -= WinsNeededForNextLevel;
            Level++;
            WinsNeededForNextLevel += 5;
            NotifyStateChanged();
            return true;
        }

        NotifyStateChanged();
        return false;
    }

    public void MarkDailyChallengeAsPlayed(int totalScore, int myRank)
    {
        HasPlayedDailyChallengeToday = true;
        DailyChallengeScore = totalScore;
        DailyChallengeRank = myRank;
        NotifyStateChanged();
    }

    public DailyLoginReward? TryClaimDailyLoginReward()
    {
        var today = DateTime.UtcNow.Date;
        if (LastDailyLoginRewardDate == today)
        {
            return null;
        }

        if (LastDailyLoginRewardDate == today.AddDays(-1))
        {
            DailyLoginStreak++;
        }
        else
        {
            DailyLoginStreak = 1;
        }

        LastDailyLoginRewardDate = today;
        var coins = 10 + (DailyLoginStreak - 1) * 5;
        return new DailyLoginReward(coins, DailyLoginStreak);
    }

    public bool BuyTicket(int ticketCount, int coinCost)
    {
        if (UserCoins < coinCost)
        {
            return false;
        }

        UserCoins -= coinCost;
        Tickets += ticketCount;
        NotifyStateChanged();
        return true;
    }

    public record struct DailyLoginReward(int coins, int streak);
}
