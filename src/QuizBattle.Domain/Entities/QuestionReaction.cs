using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// واکنش کاربر به سوال (پسندیدم، نپسندیدم، گزارش)
/// </summary>
public class QuestionReactionEntity : BaseEntity
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
    public QuestionReaction Reaction { get; set; }
    public string? ReportReason { get; set; } // در صورت گزارش
    public bool IsReviewed { get; set; } = false; // برای گزارش‌ها
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedById { get; set; }
    
    // روابط
    public virtual User User { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}
