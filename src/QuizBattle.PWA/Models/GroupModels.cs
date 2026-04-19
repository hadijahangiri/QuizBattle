namespace QuizBattle.PWA.Models;

public enum GroupMemberRole
{
    Member = 0,
    Officer = 1,
    Leader = 2,
}

public record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    string? LogoUrl,
    string UniqueCode,
    int MembersCount,
    int TotalScore,
    bool IsOwner,
    bool IsChatEnabled,
    string InviteLink
);

public record GroupMemberDto(
    Guid Id,
    Guid UserId,
    string Username,
    string AvatarUrl,
    GroupMemberRole Role,
    int ContributedScore,
    DateTime JoinedAt
);

public record GroupChatMessageDto(
    Guid Id,
    Guid SenderId,
    string SenderUsername,
    string SenderAvatarUrl,
    string Message,
    DateTime SentAt
);

public record GroupBattleMemberDto(
    Guid Id,
    Guid UserId,
    string Username,
    string Avatar,
    string RoleLabel,
    bool IsReady = false
);

public record GroupBattleInfo(
    Guid BattleId,
    string Status,
    int OurScore,
    int OpponentScore,
    List<GroupBattleMemberDto> OurMembers,
    List<GroupBattleMemberDto> OpponentMembers,
    string OpponentName,
    string OpponentLogo,
    int MemberCount
);
