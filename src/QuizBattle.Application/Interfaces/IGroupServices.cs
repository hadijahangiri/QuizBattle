using QuizBattle.Application.DTOs;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Interfaces;

/// <summary>
/// سرویس مدیریت گروه‌ها
/// </summary>
public interface IGroupService
{
    Task<GroupDto> CreateAsync(Guid ownerId, CreateGroupDto dto);
    Task<GroupDto?> GetByIdAsync(Guid id);
    Task<GroupDto?> GetByUniqueCodeAsync(string uniqueCode);
    Task<GroupDto> UpdateAsync(Guid id, Guid userId, UpdateGroupDto dto);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<PaginatedResultDto<GroupDto>> SearchAsync(GroupSearchDto dto);
    Task<List<GroupDto>> GetUserGroupsAsync(Guid userId);
    Task<List<GroupDto>> GetOwnedGroupsAsync(Guid userId);
    
    // اعضا
    Task<List<GroupMemberDto>> GetMembersAsync(Guid groupId);
    Task<bool> RequestMembershipAsync(Guid groupId, Guid userId, string? message = null);
    Task<List<MembershipRequestDto>> GetMembershipRequestsAsync(Guid groupId, Guid userId);
    Task<bool> ApproveMembershipAsync(Guid requestId, Guid approverId);
    Task<bool> RejectMembershipAsync(Guid requestId, Guid approverId);
    Task<bool> KickMemberAsync(Guid groupId, Guid memberId, Guid kickerId);
    Task<bool> LeaveGroupAsync(Guid groupId, Guid userId);
    Task<bool> PromoteMemberAsync(Guid groupId, Guid memberId, Guid promoterId, GroupRole newRole);
    
    // چت
    Task<List<GroupChatMessageDto>> GetChatMessagesAsync(Guid groupId, int page = 1, int pageSize = 50);
    Task<GroupChatMessageDto> SendMessageAsync(Guid userId, SendGroupChatDto dto);
    Task<bool> DeleteMessageAsync(Guid messageId, Guid userId);
    Task<bool> ToggleChatAsync(Guid groupId, Guid userId, bool enabled);
    
    // آمار
    Task<int> GetUserGroupsCountAsync(Guid userId);
    Task<int> GetOwnedGroupsCountAsync(Guid userId);
}

/// <summary>
/// سرویس مبارزات گروهی
/// </summary>
public interface IGroupBattleService
{
    Task<GroupBattleDto> CreateBattleRequestAsync(CreateGroupBattleDto dto);
    Task<GroupBattleDto?> GetByIdAsync(Guid id);
    Task<List<GroupBattleDto>> GetGroupBattlesAsync(Guid groupId, int page = 1, int pageSize = 20);
    Task<bool> MatchGroupsAsync(); // برای پیدا کردن گروه‌های هم‌تراز
    Task<GroupBattleMatchDto?> GetCurrentMatchAsync(Guid battleId, Guid userId);
    Task<AnswerResultDto> SubmitAnswerAsync(Guid userId, Guid matchId, SubmitAnswerDto dto);
    Task<bool> CheckAndExpireBattlesAsync(); // برای پایان بعد از 24 ساعت
    Task<GroupBattleDto?> GetActiveBattleAsync(Guid groupId);
}
