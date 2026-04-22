using QuizBattle.Application.DTOs;

namespace QuizBattle.Application.Interfaces;

/// <summary>
/// سرویس مدیریت کاربران
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<UserDto?> GetByPhoneAsync(string phone);
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> CreateGuestAsync(string username, string avatarUrl);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);
    Task<UserDto> ConvertGuestToRegularAsync(int id, string? email, string? phone);
    Task<bool> UpdateCoinsAsync(int id, int amount);
    Task<bool> AddScoreAsync(int id, int score);
    Task<UserProfileDto?> GetProfileAsync(int id);
    Task<List<UserProfileDto>> SearchUsersAsync(string query, int limit = 10);
    Task<bool> HasCompletedTutorialAsync(int id);
    Task MarkTutorialCompletedAsync(int id);
    Task UpdateDeviceTokenAsync(int id, string deviceToken);
}

/// <summary>
/// سرویس مدیریت دسته‌بندی‌ها
/// </summary>
public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(int id, CreateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<CategoryDto>> GetRandomCategoriesAsync(int count = 4);
}

/// <summary>
/// سرویس مدیریت سوالات
/// </summary>
public interface IQuestionService
{
    Task<QuestionDto?> GetByIdAsync(int id);
    Task<List<QuestionDto>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<List<QuestionDto>> GetByCategoryAsync(int categoryId, int count = 3);
    Task<List<QuestionWithCorrectAnswerDto>> GetWithAnswersByCategoryAsync(int categoryId, int count = 3);
    Task<QuestionDto> CreateAsync(CreateQuestionDto dto);
    Task<QuestionDto> UpdateAsync(int id, CreateQuestionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReactToQuestionAsync(int userId, QuestionReactionDto dto);
    Task<PaginatedResultDto<ReportedQuestionDto>> GetReportedQuestionsAsync(int page = 1, int pageSize = 20);
    Task<bool> ReviewReportAsync(int questionId, bool approve);
}

/// <summary>
/// سرویس مدیریت بازی تک به تک
/// </summary>
public interface IGameService
{
    Task<GameDto> CreateGameAsync(CreateGameDto dto);
    Task<GameDto?> GetByIdAsync(int gameId);
    Task<List<GameDto>> GetUserActiveGamesAsync(int userId);
    Task<List<GameDto>> GetUserGameHistoryAsync(int userId, int page = 1, int pageSize = 20);
    Task<GameRoundDto?> GetCurrentRoundAsync(int gameId, int? userId = null);
    Task<CategorySuggestionsDto> GetCategorySuggestionsAsync(int gameId, int roundNumber);
    Task<bool> ChangeCategorySuggestionsAsync(int gameId, int roundNumber);
    Task<GameRoundDto> SelectCategoryAsync(SelectCategoryDto dto);
    Task<AnswerResultDto> SubmitAnswerAsync(int userId, SubmitAnswerDto dto);
    Task<bool> CheckAndExpireGamesAsync(); // برای timeout بعد از 5 دقیقه
    Task<int> GetUserActiveGamesCountAsync(int userId);
    
    // Matchmaking
    Task<MatchmakingResultDto> JoinMatchmakingQueueAsync(int userId);
    Task<MatchmakingResultDto> GetMatchmakingStatusAsync(int userId);
    Task LeaveMatchmakingQueueAsync(int userId);
}

/// <summary>
/// سرویس چالش روزانه
/// </summary>
public interface IDailyChallengeService
{
    Task<DailyChallengeDto?> GetTodayChallengeAsync(int? userId = null);
    Task<List<DailyChallengeQuestionDto>> GetChallengeQuestionsAsync(int challengeId);
    Task<AnswerResultDto> SubmitAnswerAsync(int userId, SubmitDailyChallengeAnswerDto dto);
    Task<DailyChallengeResultDto?> GetUserResultAsync(int challengeId, int userId);
    Task<DailyChallengeLeaderboardDto> GetLeaderboardAsync(int challengeId, int limit = 100);
    Task<bool> CreateDailyChallengeAsync(); // برای ایجاد چالش روزانه جدید
}

/// <summary>
/// سرویس جایزه روزانه
/// </summary>
public interface IDailyRewardService
{
    Task<DailyRewardStatusDto> GetStatusAsync(int userId);
    Task<ClaimDailyRewardResultDto> ClaimRewardAsync(int userId);
}

/// <summary>
/// سرویس فروشگاه
/// </summary>
public interface IStoreService
{
    Task<List<StoreItemDto>> GetAllItemsAsync();
    Task<List<StoreItemDto>> GetActiveItemsAsync();
    Task<StoreItemDto?> GetItemByIdAsync(int id);
    Task<StoreItemDto> CreateItemAsync(CreateStoreItemDto dto);
    Task<StoreItemDto> UpdateItemAsync(int id, CreateStoreItemDto dto);
    Task<bool> DeleteItemAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<PurchaseResultDto> PurchaseAsync(int userId, PurchaseRequestDto dto);
    Task<bool> ConfirmPurchaseAsync(string transactionId, string paymentReferenceId);
    Task<bool> GiftCoinsAsync(int senderId, GiftCoinsDto dto);
    Task<List<TransactionDto>> GetUserTransactionsAsync(int userId, int page = 1, int pageSize = 20);
}

/// <summary>
/// سرویس آواتار
/// </summary>
public interface IAvatarService
{
    Task<List<AvatarDto>> GetAllAsync();
    Task<List<AvatarDto>> GetDefaultAvatarsAsync();
    Task<AvatarDto?> GetByIdAsync(int id);
    Task<bool> PurchaseAvatarAsync(int userId, int avatarId);
}

/// <summary>
/// سرویس نوتیفیکیشن
/// </summary>
public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task SendNotificationAsync(int userId, string title, string message, string? actionUrl = null);
    Task SendPushNotificationAsync(int userId, string title, string message);
}
