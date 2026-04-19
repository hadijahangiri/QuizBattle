using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// واکنش کاربر به سوال (پسندیدم، نپسندیدم، گزارش)
/// </summary>
public class QuestionReactionEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public QuestionReaction Reaction { get; set; }
    public string? ReportReason { get; set; } // در صورت گزارش
    public bool IsReviewed { get; set; } = false; // برای گزارش‌ها
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    
    // روابط
    public virtual User User { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}
