namespace QuizBattle.Domain.Entities;

/// <summary>
/// تنظیمات عمومی بازی
/// </summary>
public class GameSettings : BaseEntity
{
    public int OpponentResponseTimeoutHours { get; set; } = 12;
}
