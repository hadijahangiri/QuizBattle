using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Services;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private const int MaxOwnedGroups = 10;
    private const int MaxMemberGroups = 10;

    public GroupService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupDto> CreateAsync(Guid ownerId, CreateGroupDto dto)
    {
        // بررسی حداکثر تعداد گروه‌های ساخته شده
        var ownedCount = await GetOwnedGroupsCountAsync(ownerId);
        if (ownedCount >= MaxOwnedGroups)
            throw new Exception("شما نمی‌توانید بیش از 10 گروه بسازید");

        var uniqueCode = GenerateUniqueCode();

        var group = new Group
        {
            Name = dto.Name,
            Description = dto.Description,
            LogoUrl = dto.LogoUrl,
            OwnerId = ownerId,
            UniqueCode = uniqueCode,
            IsChatEnabled = dto.IsChatEnabled,
            IsPublic = dto.IsPublic,
            MembersCount = 1
        };
        group.GenerateInviteLink();

        await _unitOfWork.Repository<Group>().AddAsync(group);

        // اضافه کردن مالک به عنوان عضو
        var ownerMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = ownerId,
            Role = GroupRole.Owner
        };
        await _unitOfWork.Repository<GroupMember>().AddAsync(ownerMember);

        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(group.Id) ?? throw new Exception("خطا در ایجاد گروه");
    }

    public async Task<GroupDto?> GetByIdAsync(Guid id)
    {
        var group = await _unitOfWork.Repository<Group>()
            .Query()
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto?> GetByUniqueCodeAsync(string uniqueCode)
    {
        var group = await _unitOfWork.Repository<Group>()
            .Query()
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.UniqueCode == uniqueCode && !g.IsDeleted);

        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto> UpdateAsync(Guid id, Guid userId, UpdateGroupDto dto)
    {
        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(id);
        if (group == null)
            throw new Exception("گروه یافت نشد");

        if (group.OwnerId != userId)
            throw new Exception("فقط مالک گروه می‌تواند اطلاعات را ویرایش کند");

        if (dto.Name != null) group.Name = dto.Name;
        if (dto.Description != null) group.Description = dto.Description;
        if (dto.LogoUrl != null) group.LogoUrl = dto.LogoUrl;
        if (dto.IsChatEnabled.HasValue) group.IsChatEnabled = dto.IsChatEnabled.Value;
        if (dto.IsPublic.HasValue) group.IsPublic = dto.IsPublic.Value;

        group.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Group>().UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id) ?? throw new Exception("خطا در بروزرسانی گروه");
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(id);
        if (group == null) return false;
        if (group.OwnerId != userId) return false;

        group.IsDeleted = true;
        group.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Group>().UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<PaginatedResultDto<GroupDto>> SearchAsync(GroupSearchDto dto)
    {
        var query = _unitOfWork.Repository<Group>()
            .Query()
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .Where(g => !g.IsDeleted && g.IsPublic);

        if (!string.IsNullOrEmpty(dto.UniqueCode))
        {
            query = query.Where(g => g.UniqueCode == dto.UniqueCode);
        }
        else if (!string.IsNullOrEmpty(dto.Query))
        {
            query = query.Where(g => g.Name.Contains(dto.Query));
        }

        // مرتب‌سازی بر اساس امتیاز
        query = query.OrderByDescending(g => g.TotalScore);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / dto.PageSize);

        var groups = await query
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToListAsync();

        return new PaginatedResultDto<GroupDto>(
            groups.Select(MapToDto).ToList(),
            totalCount,
            dto.Page,
            dto.PageSize,
            totalPages
        );
    }

    public async Task<List<GroupDto>> GetUserGroupsAsync(Guid userId)
    {
        var memberships = await _unitOfWork.Repository<GroupMember>()
            .Query()
            .Include(gm => gm.Group)
                .ThenInclude(g => g.Owner)
            .Where(gm => gm.UserId == userId && !gm.Group.IsDeleted)
            .ToListAsync();

        return memberships.Select(m => MapToDto(m.Group)).ToList();
    }

    public async Task<List<GroupDto>> GetOwnedGroupsAsync(Guid userId)
    {
        var groups = await _unitOfWork.Repository<Group>()
            .Query()
            .Include(g => g.Owner)
            .Where(g => g.OwnerId == userId && !g.IsDeleted)
            .ToListAsync();

        return groups.Select(MapToDto).ToList();
    }

    public async Task<List<GroupMemberDto>> GetMembersAsync(Guid groupId)
    {
        var members = await _unitOfWork.Repository<GroupMember>()
            .Query()
            .Include(gm => gm.User)
            .Where(gm => gm.GroupId == groupId)
            .OrderByDescending(gm => gm.Role)
            .ThenByDescending(gm => gm.ContributedScore)
            .ToListAsync();

        return members.Select(m => new GroupMemberDto(
            m.Id,
            m.UserId,
            m.User.Username,
            m.User.AvatarUrl,
            m.Role,
            m.ContributedScore,
            m.JoinedAt
        )).ToList();
    }

    public async Task<bool> RequestMembershipAsync(Guid groupId, Guid userId, string? message = null)
    {
        // بررسی حداکثر تعداد گروه‌ها
        var memberCount = await GetUserGroupsCountAsync(userId);
        if (memberCount >= MaxMemberGroups)
            throw new Exception("شما عضو حداکثر تعداد گروه هستید");

        // بررسی اینکه قبلاً عضو نیست
        var isAlreadyMember = await _unitOfWork.Repository<GroupMember>()
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (isAlreadyMember)
            throw new Exception("شما قبلاً عضو این گروه هستید");

        // بررسی اینکه درخواست قبلی ندارد
        var hasPendingRequest = await _unitOfWork.Repository<GroupMembershipRequest>()
            .AnyAsync(r => r.GroupId == groupId && r.UserId == userId && r.Status == MembershipStatus.Pending);

        if (hasPendingRequest)
            throw new Exception("شما قبلاً درخواست داده‌اید");

        var request = new GroupMembershipRequest
        {
            GroupId = groupId,
            UserId = userId,
            Message = message
        };

        await _unitOfWork.Repository<GroupMembershipRequest>().AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<MembershipRequestDto>> GetMembershipRequestsAsync(Guid groupId, Guid userId)
    {
        // فقط مالک و ادمین می‌توانند درخواست‌ها را ببینند
        var member = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (member == null || member.Role == GroupRole.Member)
            throw new Exception("دسترسی ندارید");

        var requests = await _unitOfWork.Repository<GroupMembershipRequest>()
            .Query()
            .Include(r => r.User)
            .Where(r => r.GroupId == groupId && r.Status == MembershipStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => new MembershipRequestDto(
            r.Id,
            r.UserId,
            r.User.Username,
            r.User.AvatarUrl,
            r.User.Level,
            r.Message,
            r.CreatedAt
        )).ToList();
    }

    public async Task<bool> ApproveMembershipAsync(Guid requestId, Guid approverId)
    {
        var request = await _unitOfWork.Repository<GroupMembershipRequest>()
            .Query()
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return false;

        // بررسی دسترسی
        var approver = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == approverId);

        if (approver == null || approver.Role == GroupRole.Member)
            return false;

        // تایید درخواست
        request.Status = MembershipStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedById = approverId;
        await _unitOfWork.Repository<GroupMembershipRequest>().UpdateAsync(request);

        // اضافه کردن به گروه
        var newMember = new GroupMember
        {
            GroupId = request.GroupId,
            UserId = request.UserId,
            Role = GroupRole.Member
        };
        await _unitOfWork.Repository<GroupMember>().AddAsync(newMember);

        // افزایش تعداد اعضا
        request.Group.MembersCount++;
        await _unitOfWork.Repository<Group>().UpdateAsync(request.Group);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectMembershipAsync(Guid requestId, Guid approverId)
    {
        var request = await _unitOfWork.Repository<GroupMembershipRequest>().GetByIdAsync(requestId);
        if (request == null) return false;

        // بررسی دسترسی
        var approver = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == approverId);

        if (approver == null || approver.Role == GroupRole.Member)
            return false;

        request.Status = MembershipStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedById = approverId;

        await _unitOfWork.Repository<GroupMembershipRequest>().UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> KickMemberAsync(Guid groupId, Guid memberId, Guid kickerId)
    {
        var kicker = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == kickerId);

        var memberToKick = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == memberId);

        if (kicker == null || memberToKick == null) return false;

        // فقط مالک و ادمین می‌توانند اخراج کنند
        if (kicker.Role == GroupRole.Member) return false;

        // ادمین نمی‌تواند مالک یا ادمین دیگر را اخراج کند
        if (kicker.Role == GroupRole.Admin && memberToKick.Role != GroupRole.Member)
            return false;

        await _unitOfWork.Repository<GroupMember>().DeleteAsync(memberToKick);

        // کاهش تعداد اعضا
        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(groupId);
        if (group != null)
        {
            group.MembersCount--;
            await _unitOfWork.Repository<Group>().UpdateAsync(group);
        }

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LeaveGroupAsync(Guid groupId, Guid userId)
    {
        var member = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (member == null) return false;

        // مالک نمی‌تواند گروه را ترک کند (باید حذف کند)
        if (member.Role == GroupRole.Owner)
            throw new Exception("مالک نمی‌تواند گروه را ترک کند. لطفاً گروه را حذف کنید");

        await _unitOfWork.Repository<GroupMember>().DeleteAsync(member);

        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(groupId);
        if (group != null)
        {
            group.MembersCount--;
            await _unitOfWork.Repository<Group>().UpdateAsync(group);
        }

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PromoteMemberAsync(Guid groupId, Guid memberId, Guid promoterId, GroupRole newRole)
    {
        var promoter = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == promoterId);

        if (promoter == null || promoter.Role != GroupRole.Owner)
            return false;

        var member = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == memberId);

        if (member == null) return false;

        member.Role = newRole;
        await _unitOfWork.Repository<GroupMember>().UpdateAsync(member);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<GroupChatMessageDto>> GetChatMessagesAsync(Guid groupId, int page = 1, int pageSize = 50)
    {
        var messages = await _unitOfWork.Repository<GroupChat>()
            .Query()
            .Include(gc => gc.Sender)
            .Where(gc => gc.GroupId == groupId && !gc.IsDeleted)
            .OrderByDescending(gc => gc.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages.Select(m => new GroupChatMessageDto(
            m.Id,
            m.SenderId,
            m.Sender.Username,
            m.Sender.AvatarUrl,
            m.Message,
            m.SentAt
        )).ToList();
    }

    public async Task<GroupChatMessageDto> SendMessageAsync(Guid userId, SendGroupChatDto dto)
    {
        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(dto.GroupId);
        if (group == null)
            throw new Exception("گروه یافت نشد");

        if (!group.IsChatEnabled)
            throw new Exception("چت در این گروه غیرفعال است");

        var isMember = await _unitOfWork.Repository<GroupMember>()
            .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId);

        if (!isMember)
            throw new Exception("شما عضو این گروه نیستید");

        var message = new GroupChat
        {
            GroupId = dto.GroupId,
            SenderId = userId,
            Message = dto.Message
        };

        await _unitOfWork.Repository<GroupChat>().AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        return new GroupChatMessageDto(
            message.Id,
            userId,
            user!.Username,
            user.AvatarUrl,
            message.Message,
            message.SentAt
        );
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _unitOfWork.Repository<GroupChat>().GetByIdAsync(messageId);
        if (message == null) return false;

        var member = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == message.GroupId && gm.UserId == userId);

        // فقط فرستنده، ادمین یا مالک می‌تواند پیام را حذف کند
        if (message.SenderId != userId && (member == null || member.Role == GroupRole.Member))
            return false;

        message.IsDeleted = true;
        await _unitOfWork.Repository<GroupChat>().UpdateAsync(message);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleChatAsync(Guid groupId, Guid userId, bool enabled)
    {
        var group = await _unitOfWork.Repository<Group>().GetByIdAsync(groupId);
        if (group == null) return false;

        if (group.OwnerId != userId) return false;

        group.IsChatEnabled = enabled;
        group.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Group>().UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUserGroupsCountAsync(Guid userId)
    {
        return await _unitOfWork.Repository<GroupMember>()
            .Query()
            .Include(gm => gm.Group)
            .CountAsync(gm => gm.UserId == userId && !gm.Group.IsDeleted);
    }

    public async Task<int> GetOwnedGroupsCountAsync(Guid userId)
    {
        return await _unitOfWork.Repository<Group>()
            .CountAsync(g => g.OwnerId == userId && !g.IsDeleted);
    }

    private static string GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static GroupDto MapToDto(Group group) => new(
        group.Id,
        group.Name,
        group.Description,
        group.LogoUrl,
        group.UniqueCode,
        group.OwnerId,
        group.Owner?.Username ?? "",
        group.TotalScore,
        group.MembersCount,
        group.WinsCount,
        group.LossesCount,
        group.IsChatEnabled,
        group.IsPublic,
        group.InviteLink
    );
}
