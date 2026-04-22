using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Entities;

/// <summary>
/// گروه
/// </summary>
public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string UniqueCode { get; set; } = string.Empty; // آی‌دی اختصاصی برای جستجو
    public int OwnerId { get; set; }
    public int TotalScore { get; set; } = 0;
    public int MembersCount { get; set; } = 0;
    public int WinsCount { get; set; } = 0;
    public int LossesCount { get; set; } = 0;
    public bool IsChatEnabled { get; set; } = true;
    public bool IsPublic { get; set; } = true;
    public string InviteLink { get; set; } = string.Empty;
    
    // روابط
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public virtual ICollection<GroupMembershipRequest> MembershipRequests { get; set; } = new List<GroupMembershipRequest>();
    public virtual ICollection<GroupChat> ChatMessages { get; set; } = new List<GroupChat>();
    public virtual ICollection<GroupBattle> BattlesAsGroup1 { get; set; } = new List<GroupBattle>();
    public virtual ICollection<GroupBattle> BattlesAsGroup2 { get; set; } = new List<GroupBattle>();
    
    /// <summary>
    /// ایجاد لینک دعوت
    /// </summary>
    public void GenerateInviteLink()
    {
        InviteLink = $"quizbattle://group/join/{UniqueCode}";
    }
}

/// <summary>
/// عضو گروه
/// </summary>
public class GroupMember : BaseEntity
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public int ContributedScore { get; set; } = 0;
    
    // روابط
    public virtual Group Group { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// درخواست عضویت در گروه
/// </summary>
public class GroupMembershipRequest : BaseEntity
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? Message { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Pending;
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedById { get; set; }
    
    // روابط
    public virtual Group Group { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? ReviewedBy { get; set; }
}

/// <summary>
/// چت گروه
/// </summary>
public class GroupChat : BaseEntity
{
    public int GroupId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    
    // روابط
    public virtual Group Group { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}
