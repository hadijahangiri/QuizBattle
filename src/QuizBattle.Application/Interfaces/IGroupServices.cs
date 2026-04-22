using QuizBattle.Application.DTOs;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Interfaces;

/// <summary>
/// سرویس مدیریت گروه‌ها
/// </summary>
public interface IGroupService
{
    Task<GroupDto> CreateAsync(int ownerId, CreateGroupDto dto);
    Task<GroupDto?> GetByIdAsync(int id);
    Task<GroupDto?> GetByUniqueCodeAsync(string uniqueCode);
    Task<GroupDto> UpdateAsync(int id, int userId, UpdateGroupDto dto);
    Task<bool> DeleteAsync(int id, int userId);
    Task<PaginatedResultDto<GroupDto>> SearchAsync(GroupSearchDto dto);
    Task<List<GroupDto>> GetUserGroupsAsync(int userId);
    Task<List<GroupDto>> GetOwnedGroupsAsync(int userId);
    
    // اعضا
    Task<List<GroupMemberDto>> GetMembersAsync(int groupId);
    Task<bool> RequestMembershipAsync(int groupId, int userId, string? message = null);
    Task<List<MembershipRequestDto>> GetMembershipRequestsAsync(int groupId, int userId);
    Task<bool> ApproveMembershipAsync(int requestId, int approverId);
    Task<bool> RejectMembershipAsync(int requestId, int approverId);
    Task<bool> KickMemberAsync(int groupId, int memberId, int kickerId);
    Task<bool> LeaveGroupAsync(int groupId, int userId);
    Task<bool> PromoteMemberAsync(int groupId, int memberId, int promoterId, GroupRole newRole);
    
    // چت
    Task<List<GroupChatMessageDto>> GetChatMessagesAsync(int groupId, int page = 1, int pageSize = 50);
    Task<GroupChatMessageDto> SendMessageAsync(int userId, SendGroupChatDto dto);
    Task<bool> DeleteMessageAsync(int messageId, int userId);
    Task<bool> ToggleChatAsync(int groupId, int userId, bool enabled);
    
    // آمار
    Task<int> GetUserGroupsCountAsync(int userId);
    Task<int> GetOwnedGroupsCountAsync(int userId);
}

/// <summary>
/// سرویس مبارزات گروهی
/// </summary>
public interface IGroupBattleService
{
    Task<GroupBattleDto> CreateBattleRequestAsync(CreateGroupBattleDto dto);
    Task<GroupBattleDto?> GetByIdAsync(int id);
    Task<List<GroupBattleDto>> GetGroupBattlesAsync(int groupId, int page = 1, int pageSize = 20);
    Task<bool> MatchGroupsAsync(); // برای پیدا کردن گروه‌های هم‌تراز
    Task<GroupBattleMatchDto?> GetCurrentMatchAsync(int battleId, int userId);
    Task<AnswerResultDto> SubmitAnswerAsync(int userId, int matchId, SubmitAnswerDto dto);
    Task<bool> CheckAndExpireBattlesAsync(); // برای پایان بعد از 24 ساعت
    Task<GroupBattleDto?> GetActiveBattleAsync(int groupId);
}
