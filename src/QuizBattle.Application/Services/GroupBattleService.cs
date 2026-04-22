using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Services;

public class GroupBattleService : IGroupBattleService
{
    private readonly IUnitOfWork _unitOfWork;

    public GroupBattleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupBattleDto> CreateBattleRequestAsync(CreateGroupBattleDto dto)
    {
        var group = await _unitOfWork.Repository<Group>()
            .Query()
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == dto.GroupId && !g.IsDeleted);

        if (group == null)
            throw new Exception("گروه یافت نشد");

        if (dto.RequesterId == 0)
            throw new Exception("شناسه سازنده مبارزه مشخص نیست");

        var ownerMember = await _unitOfWork.Repository<GroupMember>()
            .FirstOrDefaultAsync(m => m.GroupId == dto.GroupId && m.UserId == dto.RequesterId);

        if (ownerMember == null || ownerMember.Role != GroupRole.Owner)
            throw new Exception("فقط مالک گروه می‌تواند درخواست مبارزه ارسال کند");

        if (dto.PlayerIds == null || !dto.PlayerIds.Any())
            throw new Exception("حداقل یک شرکت‌کننده باید انتخاب شود");

        var selectedMembers = await _unitOfWork.Repository<GroupMember>()
            .Query()
            .Include(m => m.User)
            .Where(m => m.GroupId == dto.GroupId && dto.PlayerIds.Contains(m.UserId))
            .ToListAsync();

        if (selectedMembers.Count != dto.PlayerIds.Count)
            throw new Exception("برخی از کاربران انتخاب‌شده عضو گروه نیستند");

        var existingBattle = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Group1)
            .Include(b => b.Group2)
            .Where(b => b.Status == GroupBattleStatus.WaitingForMatch && b.Group2Id == null && b.Group1Id != dto.GroupId && b.PlayersPerTeam == selectedMembers.Count)
            .OrderBy(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        if (existingBattle != null)
        {
            existingBattle.Group2Id = dto.GroupId;
            existingBattle.Status = GroupBattleStatus.InProgress;
            existingBattle.MatchedAt = DateTime.UtcNow;
            existingBattle.StartedAt = DateTime.UtcNow;
            existingBattle.ExpiresAt = DateTime.UtcNow.AddHours(24);

            var group2Players = selectedMembers.Select((item, index) => new GroupBattlePlayer
            {
                GroupBattleId = existingBattle.Id,
                GroupId = dto.GroupId,
                UserId = item.UserId,
                PlayerOrder = index + 1,
                HasPlayed = false,
                Score = 0
            }).ToList();

            await _unitOfWork.Repository<GroupBattlePlayer>().AddRangeAsync(group2Players);
            await _unitOfWork.Repository<GroupBattle>().UpdateAsync(existingBattle);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(existingBattle.Id) ?? throw new Exception("خطا در بارگذاری مبارزه");
        }

        var waitingBattle = new GroupBattle
        {
            Group1Id = dto.GroupId,
            PlayersPerTeam = selectedMembers.Count,
            Status = GroupBattleStatus.WaitingForMatch,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<GroupBattle>().AddAsync(waitingBattle);

        var battlePlayers = selectedMembers.Select((item, index) => new GroupBattlePlayer
        {
            GroupBattleId = waitingBattle.Id,
            GroupId = dto.GroupId,
            UserId = item.UserId,
            PlayerOrder = index + 1,
            HasPlayed = false,
            Score = 0
        }).ToList();

        await _unitOfWork.Repository<GroupBattlePlayer>().AddRangeAsync(battlePlayers);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(waitingBattle.Id) ?? throw new Exception("خطا در بارگذاری مبارزه");
    }

    public async Task<GroupBattleDto?> GetByIdAsync(int id)
    {
        var battle = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Group1)
            .Include(b => b.Group2)
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group1Player)
                    .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group2Player)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        return battle == null ? null : MapToDto(battle);
    }

    public async Task<List<GroupBattleDto>> GetGroupBattlesAsync(int groupId, int page = 1, int pageSize = 20)
    {
        var query = _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Group1)
            .Include(b => b.Group2)
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group1Player)
                    .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group2Player)
                    .ThenInclude(p => p.User)
            .Where(b => b.Group1Id == groupId || b.Group2Id == groupId)
            .OrderByDescending(b => b.CreatedAt);

        var battles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return battles.Select(MapToDto).ToList();
    }

    public async Task<bool> MatchGroupsAsync()
    {
        var waitingBattles = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Group1)
            .Where(b => b.Status == GroupBattleStatus.WaitingForMatch && b.Group2Id == null)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();

        var grouped = waitingBattles
            .GroupBy(b => b.PlayersPerTeam)
            .Where(g => g.Count() >= 2)
            .ToList();

        var matchedAny = false;

        foreach (var group in grouped)
        {
            var battles = group.ToList();
            while (battles.Count >= 2)
            {
                var first = battles[0];
                var second = battles[1];

                first.Group2Id = second.Group1Id;
                first.Status = GroupBattleStatus.InProgress;
                first.MatchedAt = DateTime.UtcNow;
                first.StartedAt = DateTime.UtcNow;
                first.ExpiresAt = DateTime.UtcNow.AddHours(24);

                var secondPlayers = await _unitOfWork.Repository<GroupBattlePlayer>()
                    .Query()
                    .Where(p => p.GroupBattleId == second.Id)
                    .ToListAsync();

                foreach (var player in secondPlayers)
                {
                    player.GroupBattleId = first.Id;
                    player.GroupId = second.Group1Id;
                    await _unitOfWork.Repository<GroupBattlePlayer>().UpdateAsync(player);
                }

                await _unitOfWork.Repository<GroupBattle>().UpdateAsync(first);
                await _unitOfWork.Repository<GroupBattle>().DeleteAsync(second);
                await _unitOfWork.SaveChangesAsync();

                battles.RemoveRange(0, 2);
                matchedAny = true;
            }
        }

        return matchedAny;
    }

    public async Task<GroupBattleMatchDto?> GetCurrentMatchAsync(int battleId, int userId)
    {
        var battle = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group1Player)
                    .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group2Player)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == battleId);

        if (battle == null)
            return null;

        var currentMatch = battle.Matches
            .FirstOrDefault(m => m.Group1Player.UserId == userId || m.Group2Player.UserId == userId);

        if (currentMatch == null)
            return null;

        return new GroupBattleMatchDto(
            currentMatch.Id,
            currentMatch.MatchOrder,
            currentMatch.Group1Player.User.Username,
            currentMatch.Group2Player.User.Username,
            currentMatch.Player1Score,
            currentMatch.Player2Score,
            currentMatch.Status
        );
    }

    public async Task<GroupBattleDto?> GetActiveBattleAsync(int groupId)
    {
        var battle = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Include(b => b.Group1)
            .Include(b => b.Group2)
            .Include(b => b.Players)
                .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group1Player)
                    .ThenInclude(p => p.User)
            .Include(b => b.Matches)
                .ThenInclude(m => m.Group2Player)
                    .ThenInclude(p => p.User)
            .Where(b => (b.Group1Id == groupId || b.Group2Id == groupId) && b.Status != GroupBattleStatus.Completed && b.Status != GroupBattleStatus.Cancelled)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        return battle == null ? null : MapToDto(battle);
    }

    public async Task<AnswerResultDto> SubmitAnswerAsync(int userId, int matchId, SubmitAnswerDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> CheckAndExpireBattlesAsync()
    {
        var expiredBattles = await _unitOfWork.Repository<GroupBattle>()
            .Query()
            .Where(b => b.Status == GroupBattleStatus.InProgress && b.ExpiresAt.HasValue && b.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (!expiredBattles.Any())
            return false;

        foreach (var battle in expiredBattles)
        {
            battle.Status = GroupBattleStatus.Completed;
            battle.CompletedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<GroupBattle>().UpdateAsync(battle);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static GroupBattleDto MapToDto(GroupBattle battle)
    {
        return new GroupBattleDto(
            battle.Id,
            battle.Group1Id,
            battle.Group1.Name,
            battle.Group1.LogoUrl,
            battle.Group2Id ?? 0,
            battle.Group2?.Name,
            battle.Group2?.LogoUrl,
            battle.Group1Score,
            battle.Group2Score,
            battle.PlayersPerTeam,
            battle.Status,
            battle.WinnerGroupId,
            battle.ExpiresAt,
            battle.Players.Select(p => new GroupBattlePlayerDto(
                p.UserId,
                p.User.Username,
                p.User.AvatarUrl,
                p.User.Level,
                p.GroupId == battle.Group1Id,
                p.HasPlayed,
                p.Score
            )).ToList(),
            battle.Matches.Select(m => new GroupBattleMatchDto(
                m.Id,
                m.MatchOrder,
                m.Group1Player.User.Username,
                m.Group2Player.User.Username,
                m.Player1Score,
                m.Player2Score,
                m.Status
            )).ToList()
        );
    }
}
