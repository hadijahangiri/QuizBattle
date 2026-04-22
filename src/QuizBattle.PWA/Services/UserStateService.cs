using QuizBattle.Shared;

namespace QuizBattle.PWA.Services;

public record UserStateData
{
    public int CurrentUserId { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public bool IsGuest { get; set; }
    public int Coins { get; set; }
    public int UserCoins { get; set; }
    public int Tickets { get; set; }
    public int TotalWins { get; set; }
    public int Level { get; set; }
    public int WinsTowardsNextLevel { get; set; }
    public int WinsNeededForNextLevel { get; set; }
    public bool HasCompletedTutorial { get; set; }
    public DateTime? LastDailyLoginRewardDate { get; set; }
    public int DailyLoginStreak { get; set; }
    public string? AuthToken { get; set; }
}

public class UserStateService
{
    private const string UserStateKey = "quizbattle_user_state";
    
    private readonly IBrowserStorageService _storage;
    private readonly AppState _appState;

    public UserStateService(IBrowserStorageService storage, AppState appState)
    {
        _storage = storage;
        _appState = appState;
    }

    public async Task LoadUserStateAsync()
    {
        var savedState = await _storage.GetAsync<UserStateData>(UserStateKey);
        
        if (savedState != null && savedState.CurrentUserId != 0)
        {
            _appState.CurrentUserId = savedState.CurrentUserId;
            _appState.Username = savedState.Username;
            _appState.AvatarUrl = savedState.AvatarUrl;
            _appState.UserAvatar = savedState.AvatarUrl;
            _appState.IsGuest = savedState.IsGuest;
            _appState.Coins = savedState.Coins;
            _appState.UserCoins = savedState.UserCoins;
            _appState.Tickets = savedState.Tickets;
            _appState.TotalWins = savedState.TotalWins;
            _appState.Level = savedState.Level;
            _appState.WinsTowardsNextLevel = savedState.WinsTowardsNextLevel;
            _appState.WinsNeededForNextLevel = savedState.WinsNeededForNextLevel;
            _appState.HasCompletedTutorial = savedState.HasCompletedTutorial;
            _appState.LastDailyLoginRewardDate = savedState.LastDailyLoginRewardDate;
            _appState.DailyLoginStreak = savedState.DailyLoginStreak;
            _appState.AuthToken = savedState.AuthToken;
            
            _appState.NotifyStateChanged();
        }
    }

    public async Task SaveUserStateAsync()
    {
        var stateData = new UserStateData
        {
            CurrentUserId = _appState.CurrentUserId,
            Username = _appState.Username,
            AvatarUrl = _appState.AvatarUrl,
            IsGuest = _appState.IsGuest,
            Coins = _appState.Coins,
            UserCoins = _appState.UserCoins,
            Tickets = _appState.Tickets,
            TotalWins = _appState.TotalWins,
            Level = _appState.Level,
            WinsTowardsNextLevel = _appState.WinsTowardsNextLevel,
            WinsNeededForNextLevel = _appState.WinsNeededForNextLevel,
            HasCompletedTutorial = _appState.HasCompletedTutorial,
            LastDailyLoginRewardDate = _appState.LastDailyLoginRewardDate,
            DailyLoginStreak = _appState.DailyLoginStreak,
            AuthToken = _appState.AuthToken
        };
        
        await _storage.SetAsync(UserStateKey, stateData);
    }

    public async Task ClearUserStateAsync()
    {
        await _storage.RemoveAsync(UserStateKey);
    }
}
