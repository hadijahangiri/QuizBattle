using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// مبارزه گروهی
/// </summary>
public class GroupBattle : BaseEntity
{
    public Guid Group1Id { get; set; }
    public Guid? Group2Id { get; set; } // null تا زمانی که match پیدا شود
    public int Group1Score { get; set; } = 0;
    public int Group2Score { get; set; } = 0;
    public int PlayersPerTeam { get; set; } // تعداد بازیکنان هر تیم
    public GroupBattleStatus Status { get; set; } = GroupBattleStatus.WaitingForMatch;
    public Guid? WinnerGroupId { get; set; }
    public DateTime? MatchedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // 24 ساعت بعد از شروع
    public DateTime? CompletedAt { get; set; }
    
    // روابط
    public virtual Group Group1 { get; set; } = null!;
    public virtual Group? Group2 { get; set; }
    public virtual Group? WinnerGroup { get; set; }
    public virtual ICollection<GroupBattlePlayer> Players { get; set; } = new List<GroupBattlePlayer>();
    public virtual ICollection<GroupBattleMatch> Matches { get; set; } = new List<GroupBattleMatch>();
}

/// <summary>
/// بازیکنان شرکت کننده در مبارزه گروهی
/// </summary>
public class GroupBattlePlayer : BaseEntity
{
    public Guid GroupBattleId { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public int PlayerOrder { get; set; } // ترتیب بازیکن بر اساس سطح
    public bool HasPlayed { get; set; } = false;
    public int Score { get; set; } = 0;
    
    // روابط
    public virtual GroupBattle GroupBattle { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// مسابقه تک به تک در مبارزه گروهی
/// </summary>
public class GroupBattleMatch : BaseEntity
{
    public Guid GroupBattleId { get; set; }
    public Guid Group1PlayerId { get; set; }
    public Guid Group2PlayerId { get; set; }
    public int MatchOrder { get; set; }
    public int Player1Score { get; set; } = 0;
    public int Player2Score { get; set; } = 0;
    public Guid? WinnerUserId { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    
    // روابط
    public virtual GroupBattle GroupBattle { get; set; } = null!;
    public virtual GroupBattlePlayer Group1Player { get; set; } = null!;
    public virtual GroupBattlePlayer Group2Player { get; set; } = null!;
    public virtual User? Winner { get; set; }
    public virtual ICollection<GroupBattleMatchRound> Rounds { get; set; } = new List<GroupBattleMatchRound>();
}

/// <summary>
/// راند مسابقه در مبارزه گروهی
/// </summary>
public class GroupBattleMatchRound : BaseEntity
{
    public Guid GroupBattleMatchId { get; set; }
    public Guid CategoryId { get; set; }
    public int RoundNumber { get; set; }
    public int Player1Score { get; set; } = 0;
    public int Player2Score { get; set; } = 0;
    public RoundStatus Status { get; set; } = RoundStatus.NotStarted;
    
    // روابط
    public virtual GroupBattleMatch GroupBattleMatch { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<GroupBattleMatchQuestion> Questions { get; set; } = new List<GroupBattleMatchQuestion>();
}

/// <summary>
/// سوال راند مبارزه گروهی
/// </summary>
public class GroupBattleMatchQuestion : BaseEntity
{
    public Guid GroupBattleMatchRoundId { get; set; }
    public Guid QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public Guid? Player1AnswerId { get; set; }
    public Guid? Player2AnswerId { get; set; }
    public int Player1TimeSpent { get; set; } = 0;
    public int Player2TimeSpent { get; set; } = 0;
    public bool Player1IsCorrect { get; set; } = false;
    public bool Player2IsCorrect { get; set; } = false;
    
    // روابط
    public virtual GroupBattleMatchRound Round { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}
