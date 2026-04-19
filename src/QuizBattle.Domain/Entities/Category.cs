namespace QuizBattle.Domain.Entities;

/// <summary>
/// دسته‌بندی سوالات
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int OrderIndex { get; set; } = 0;
    
    // روابط
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
