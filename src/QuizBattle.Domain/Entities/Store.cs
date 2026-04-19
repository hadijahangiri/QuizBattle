using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// آیتم فروشگاه
/// </summary>
public class StoreItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CoinAmount { get; set; } // تعداد سکه
    public decimal PriceInToman { get; set; } // قیمت به تومان
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPopular { get; set; } = false;
    public decimal? DiscountPercent { get; set; }
    public DateTime? DiscountExpiresAt { get; set; }
    public int OrderIndex { get; set; } = 0;
}

/// <summary>
/// تراکنش
/// </summary>
public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public int CoinAmount { get; set; }
    public decimal? PriceInToman { get; set; } // فقط برای خرید
    public string? Description { get; set; }
    public Guid? StoreItemId { get; set; } // فقط برای خرید
    public Guid? RelatedUserId { get; set; } // برای هدیه دادن سکه
    public string? PaymentReferenceId { get; set; } // شماره پیگیری پرداخت
    public bool IsSuccessful { get; set; } = false;
    
    // روابط
    public virtual User User { get; set; } = null!;
    public virtual User? RelatedUser { get; set; }
    public virtual StoreItem? StoreItem { get; set; }
}

/// <summary>
/// درخواست هدیه سکه
/// </summary>
public class CoinGiftRequest : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public int CoinAmount { get; set; }
    public string? Message { get; set; }
    public bool IsAccepted { get; set; } = false;
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    
    // روابط
    public virtual User Sender { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
}
