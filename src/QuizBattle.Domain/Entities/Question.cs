namespace QuizBattle.Domain.Entities;

/// <summary>
/// سوال
/// </summary>
public class Question : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public int Difficulty { get; set; } = 1; // 1-5
    
    // گزینه‌ها (برای سادگی مدیریت در پنل ادمین)
    public string Option1 { get; set; } = string.Empty;
    public string Option2 { get; set; } = string.Empty;
    public string Option3 { get; set; } = string.Empty;
    public string Option4 { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    
    // آمار
    public int TimesUsed { get; set; } = 0;
    public int CorrectCount { get; set; } = 0;
    public int WrongCount { get; set; } = 0;
    public int ReportCount { get; set; } = 0;
    public int LikesCount { get; set; } = 0;
    public int DislikesCount { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    public bool IsReviewed { get; set; } = false; // بررسی شده توسط ادمین
    
    // روابط
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<QuestionReactionEntity> Reactions { get; set; } = new List<QuestionReactionEntity>();
}

/// <summary>
/// گزینه‌های پاسخ
/// </summary>
public class Answer : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
    public int QuestionId { get; set; }
    public int OrderIndex { get; set; } = 0;
    
    // روابط
    public virtual Question Question { get; set; } = null!;
}
