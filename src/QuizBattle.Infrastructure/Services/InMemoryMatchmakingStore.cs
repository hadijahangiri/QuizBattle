using System.Collections.Concurrent;
using System.Linq;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.Infrastructure.Services;

public class InMemoryMatchmakingStore : IMatchmakingStore
{
    private readonly ConcurrentDictionary<int, DateTime> _queue = new();
    private readonly ConcurrentDictionary<int, MatchInfoDto> _matchedPlayers = new();

    public Task<bool> IsUserInQueueAsync(int userId)
        => Task.FromResult(_queue.ContainsKey(userId));

    public Task AddToQueueAsync(int userId, DateTime queuedAt)
    {
        _queue[userId] = queuedAt;
        return Task.CompletedTask;
    }

    public Task RemoveFromQueueAsync(int userId)
    {
        _queue.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task<int?> GetEarliestOpponentAsync(int excludingUserId)
    {
        var opponent = _queue
            .Where(kvp => kvp.Key != excludingUserId)
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (opponent != default)
        {
            _queue.TryRemove(opponent, out _);
            return Task.FromResult<int?>(opponent);
        }

        return Task.FromResult<int?>(null);
    }

    public Task RemoveExpiredQueueEntriesAsync(TimeSpan maxAge)
    {
        var threshold = DateTime.UtcNow - maxAge;
        foreach (var expired in _queue.Where(kvp => kvp.Value < threshold).Select(kvp => kvp.Key).ToList())
        {
            _queue.TryRemove(expired, out _);
        }

        return Task.CompletedTask;
    }

    public Task SetMatchedPlayerAsync(int userId, MatchInfoDto matchInfo)
    {
        _matchedPlayers[userId] = matchInfo;
        return Task.CompletedTask;
    }

    public Task<MatchInfoDto?> GetMatchedPlayerAsync(int userId)
    {
        _matchedPlayers.TryGetValue(userId, out var matchInfo);
        return Task.FromResult(matchInfo);
    }

    public Task<MatchInfoDto?> TakeMatchedPlayerAsync(int userId)
    {
        _matchedPlayers.TryRemove(userId, out var matchInfo);
        return Task.FromResult(matchInfo);
    }

    public Task<bool> TryRemoveMatchedPlayerAsync(int userId)
        => Task.FromResult(_matchedPlayers.TryRemove(userId, out _));
}
