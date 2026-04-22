using System.Text.Json;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using StackExchange.Redis;

namespace QuizBattle.Infrastructure.Services;

public class RedisMatchmakingStore : IMatchmakingStore
{
    private const string QueueKey = "matchmaking:queue";
    private const string MatchKeyPrefix = "matchmaking:player:";
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RedisMatchmakingStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<bool> IsUserInQueueAsync(int userId)
        => await _db.SortedSetScoreAsync(QueueKey, userId.ToString()) != null;

    public Task AddToQueueAsync(int userId, DateTime queuedAt)
        => _db.SortedSetAddAsync(QueueKey, userId.ToString(), new DateTimeOffset(queuedAt.ToUniversalTime()).ToUnixTimeMilliseconds());

    public Task RemoveFromQueueAsync(int userId)
        => _db.SortedSetRemoveAsync(QueueKey, userId.ToString());

    public async Task<int?> GetEarliestOpponentAsync(int excludingUserId)
    {
        var members = await _db.SortedSetRangeByRankAsync(QueueKey, 0, -1, Order.Ascending);
        foreach (var member in members)
        {
            if (member.IsNullOrEmpty) continue;
            if (!int.TryParse(member.ToString(), out var userId) || userId == excludingUserId)
            {
                continue;
            }

            var removed = await _db.SortedSetRemoveAsync(QueueKey, member);
            if (removed)
            {
                return userId;
            }
        }

        return null;
    }

    public Task RemoveExpiredQueueEntriesAsync(TimeSpan maxAge)
    {
        var cutoff = new DateTimeOffset(DateTime.UtcNow - maxAge).ToUnixTimeMilliseconds();
        return _db.SortedSetRemoveRangeByScoreAsync(QueueKey, double.NegativeInfinity, cutoff);
    }

    public Task SetMatchedPlayerAsync(int userId, MatchInfoDto matchInfo)
        => _db.StringSetAsync(GetMatchKey(userId), JsonSerializer.Serialize(matchInfo, _jsonOptions), when: When.Always, expiry: TimeSpan.FromMinutes(10));

    public async Task<MatchInfoDto?> GetMatchedPlayerAsync(int userId)
    {
        var value = await _db.StringGetAsync(GetMatchKey(userId));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<MatchInfoDto>(value.ToString()!, _jsonOptions);
    }

    public async Task<MatchInfoDto?> TakeMatchedPlayerAsync(int userId)
    {
        var key = GetMatchKey(userId);
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;

        await _db.KeyDeleteAsync(key);
        return JsonSerializer.Deserialize<MatchInfoDto>(value.ToString()!, _jsonOptions);
    }

    public Task<bool> TryRemoveMatchedPlayerAsync(int userId)
        => _db.KeyDeleteAsync(GetMatchKey(userId));

    private static string GetMatchKey(int userId) => $"{MatchKeyPrefix}{userId:N}";
}
