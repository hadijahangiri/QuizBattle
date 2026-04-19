using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// کاربر بازی
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsGuest { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int Coins { get; set; } = 100; // سکه اولیه
    public int Score { get; set; } = 0;
    public int TotalWins { get; set; } = 0;
    public int TotalLosses { get; set; } = 0;
    public int TotalDraws { get; set; } = 0;
    public UserLevel Level { get; set; } = UserLevel.Beginner;
    public int CurrentStreak { get; set; } = 0; // روزهای متوالی ورود
    public DateTime? LastDailyRewardDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool HasCompletedTutorial { get; set; } = false;
    public string? DeviceToken { get; set; } // برای نوتیفیکیشن
    
    // روابط
    public virtual ICollection<Game> GamesAsPlayer1 { get; set; } = new List<Game>();
    public virtual ICollection<Game> GamesAsPlayer2 { get; set; } = new List<Game>();
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public virtual ICollection<Group> OwnedGroups { get; set; } = new List<Group>();
    public virtual ICollection<DailyChallengeResult> DailyChallengeResults { get; set; } = new List<DailyChallengeResult>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<QuestionReactionEntity> QuestionReactions { get; set; } = new List<QuestionReactionEntity>();
    
    /// <summary>
    /// محاسبه امتیاز مورد نیاز برای سطح بعدی
    /// </summary>
    public int GetScoreForNextLevel()
    {
        return Level switch
        {
            UserLevel.Beginner => 100,
            UserLevel.Novice => 300,
            UserLevel.Intermediate => 600,
            UserLevel.Advanced => 1000,
            UserLevel.Expert => 1500,
            UserLevel.Master => 2500,
            UserLevel.GrandMaster => 4000,
            UserLevel.Legend => int.MaxValue,
            _ => 100
        };
    }
    
    /// <summary>
    /// بررسی و ارتقای سطح کاربر
    /// </summary>
    public bool TryLevelUp()
    {
        if (Level == UserLevel.Legend) return false;
        
        if (Score >= GetScoreForNextLevel())
        {
            Level = (UserLevel)((int)Level + 1);
            return true;
        }
        return false;
    }
}
