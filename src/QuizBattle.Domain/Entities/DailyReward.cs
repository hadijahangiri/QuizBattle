namespace QuizBattle.Domain.Entities;

/// <summary>
/// جایزه روزانه
/// </summary>
public class DailyReward : BaseEntity
{
    public int Day { get; set; } // روز چندم (1-7)
    public int CoinReward { get; set; }
    public string? SpecialReward { get; set; } // جایزه ویژه (مثلا آواتار)
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// ثبت دریافت جایزه روزانه توسط کاربر
/// </summary>
public class UserDailyReward : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DailyRewardId { get; set; }
    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
    public int CoinsReceived { get; set; }
    
    // روابط
    public virtual User User { get; set; } = null!;
    public virtual DailyReward DailyReward { get; set; } = null!;
}
