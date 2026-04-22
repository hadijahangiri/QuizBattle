namespace QuizBattle.Domain.Entities;

/// <summary>
/// چالش روزانه
/// </summary>
public class DailyChallenge : BaseEntity
{
    public DateTime ChallengeDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int ParticipantsCount { get; set; } = 0;
    
    // روابط
    public virtual ICollection<DailyChallengeQuestion> Questions { get; set; } = new List<DailyChallengeQuestion>();
    public virtual ICollection<DailyChallengeResult> Results { get; set; } = new List<DailyChallengeResult>();
}

/// <summary>
/// سوالات چالش روزانه
/// </summary>
public class DailyChallengeQuestion : BaseEntity
{
    public int DailyChallengeId { get; set; }
    public int QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    
    // روابط
    public virtual DailyChallenge DailyChallenge { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}

/// <summary>
/// نتیجه شرکت در چالش روزانه
/// </summary>
public class DailyChallengeResult : BaseEntity
{
    public int DailyChallengeId { get; set; }
    public int UserId { get; set; }
    public int CorrectAnswers { get; set; } = 0;
    public int TotalTimeSpent { get; set; } = 0; // میلی‌ثانیه
    public int Score { get; set; } = 0;
    public int Rank { get; set; } = 0;
    public DateTime CompletedAt { get; set; }
    
    // روابط
    public virtual DailyChallenge DailyChallenge { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<DailyChallengeAnswer> Answers { get; set; } = new List<DailyChallengeAnswer>();
}

/// <summary>
/// پاسخ‌های کاربر به چالش روزانه
/// </summary>
public class DailyChallengeAnswer : BaseEntity
{
    public int DailyChallengeResultId { get; set; }
    public int QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int TimeSpent { get; set; } = 0;
    public bool IsCorrect { get; set; } = false;
    public int Score { get; set; } = 0;
    
    // روابط
    public virtual DailyChallengeResult Result { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
    public virtual Answer? Answer { get; set; }
}
