using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// بازی تک به تک
/// </summary>
public class Game : BaseEntity
{
    public Guid Player1Id { get; set; }
    public Guid Player2Id { get; set; }
    public int Player1Score { get; set; } = 0;
    public int Player2Score { get; set; } = 0;
    public int CurrentRound { get; set; } = 1;
    public GameStatus Status { get; set; } = GameStatus.Pending;
    public Guid? WinnerId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // روابط
    public virtual User Player1 { get; set; } = null!;
    public virtual User Player2 { get; set; } = null!;
    public virtual User? Winner { get; set; }
    public virtual ICollection<GameRound> Rounds { get; set; } = new List<GameRound>();
    
    /// <summary>
    /// بررسی اینکه آیا بازی به پایان رسیده است (6 راند)
    /// </summary>
    public bool IsCompleted => CurrentRound > 6 || Status == GameStatus.Completed;
    
    /// <summary>
    /// تعیین برنده
    /// </summary>
    public void DetermineWinner()
    {
        if (Player1Score > Player2Score)
            WinnerId = Player1Id;
        else if (Player2Score > Player1Score)
            WinnerId = Player2Id;
        // در صورت مساوی، WinnerId null می‌ماند
        
        Status = GameStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// راند بازی (3 سوال در هر راند)
/// </summary>
public class GameRound : BaseEntity
{
    public Guid GameId { get; set; }
    public int RoundNumber { get; set; }
    public Guid CategoryId { get; set; }
    public Guid CategorySelectorId { get; set; } // کسی که موضوع را انتخاب کرده
    public RoundStatus Status { get; set; } = RoundStatus.NotStarted;
    public int Player1Score { get; set; } = 0;
    public int Player2Score { get; set; } = 0;
    public DateTime? Player1AnsweredAt { get; set; }
    public DateTime? Player2AnsweredAt { get; set; }
    
    // روابط
    public virtual Game Game { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
    public virtual User CategorySelector { get; set; } = null!;
    public virtual ICollection<RoundQuestion> Questions { get; set; } = new List<RoundQuestion>();
}

/// <summary>
/// سوالات هر راند
/// </summary>
public class RoundQuestion : BaseEntity
{
    public Guid GameRoundId { get; set; }
    public Guid QuestionId { get; set; }
    public int QuestionOrder { get; set; } // ترتیب سوال در راند (1-3)
    
    // پاسخ بازیکن 1
    public Guid? Player1AnswerId { get; set; }
    public int Player1TimeSpent { get; set; } = 0; // میلی‌ثانیه
    public bool Player1IsCorrect { get; set; } = false;
    public int Player1Score { get; set; } = 0;
    public DateTime? Player1StartedAt { get; set; }
    
    // پاسخ بازیکن 2
    public Guid? Player2AnswerId { get; set; }
    public int Player2TimeSpent { get; set; } = 0;
    public bool Player2IsCorrect { get; set; } = false;
    public int Player2Score { get; set; } = 0;
    public DateTime? Player2StartedAt { get; set; }
    
    // استفاده از کمکی‌ها
    public HelperType? Player1HelperUsed { get; set; }
    public HelperType? Player2HelperUsed { get; set; }
    
    // روابط
    public virtual GameRound GameRound { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}

/// <summary>
/// کمکی‌های استفاده شده در بازی
/// </summary>
public class GameHelperUsage : BaseEntity
{
    public Guid GameId { get; set; }
    public Guid UserId { get; set; }
    public HelperType HelperType { get; set; }
    public int CoinsCost { get; set; }
    public Guid? RoundQuestionId { get; set; }
    
    // روابط
    public virtual Game Game { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
