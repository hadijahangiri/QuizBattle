using QuizBattle.Application.DTOs;

namespace QuizBattle.Application.Interfaces;

public interface IMatchmakingStore
{
    Task<bool> IsUserInQueueAsync(int userId);
    Task AddToQueueAsync(int userId, DateTime queuedAt);
    Task RemoveFromQueueAsync(int userId);
    Task<int?> GetEarliestOpponentAsync(int excludingUserId);
    Task SetMatchedPlayerAsync(int userId, MatchInfoDto matchInfo);
    Task<MatchInfoDto?> GetMatchedPlayerAsync(int userId);
    Task<MatchInfoDto?> TakeMatchedPlayerAsync(int userId);
    Task<bool> TryRemoveMatchedPlayerAsync(int userId);
    Task RemoveExpiredQueueEntriesAsync(TimeSpan maxAge);
}
