namespace QuizBattle.Domain.Entities;

/// <summary>
/// آواتار
/// </summary>
public class Avatar : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool IsPremium { get; set; } = false;
    public int? CoinPrice { get; set; } // اگر premium باشد
    public bool IsActive { get; set; } = true;
    public int OrderIndex { get; set; } = 0;
}

/// <summary>
/// نوتیفیکیشن
/// </summary>
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    // روابط
    public virtual User User { get; set; } = null!;
}
