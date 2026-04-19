using QuizBattle.Application.DTOs;

namespace QuizBattle.Application.Interfaces;

/// <summary>
/// سرویس مدیریت کاربران
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<UserDto?> GetByPhoneAsync(string phone);
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> CreateGuestAsync(string username, string avatarUrl);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<UserDto> ConvertGuestToRegularAsync(Guid id, string? email, string? phone);
    Task<bool> UpdateCoinsAsync(Guid id, int amount);
    Task<bool> AddScoreAsync(Guid id, int score);
    Task<UserProfileDto?> GetProfileAsync(Guid id);
    Task<List<UserProfileDto>> SearchUsersAsync(string query, int limit = 10);
    Task<bool> HasCompletedTutorialAsync(Guid id);
    Task MarkTutorialCompletedAsync(Guid id);
    Task UpdateDeviceTokenAsync(Guid id, string deviceToken);
}

/// <summary>
/// سرویس مدیریت دسته‌بندی‌ها
/// </summary>
public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(Guid id, CreateCategoryDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<List<CategoryDto>> GetRandomCategoriesAsync(int count = 4);
}

/// <summary>
/// سرویس مدیریت سوالات
/// </summary>
public interface IQuestionService
{
    Task<QuestionDto?> GetByIdAsync(Guid id);
    Task<List<QuestionDto>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<List<QuestionDto>> GetByCategoryAsync(Guid categoryId, int count = 3);
    Task<List<QuestionWithCorrectAnswerDto>> GetWithAnswersByCategoryAsync(Guid categoryId, int count = 3);
    Task<QuestionDto> CreateAsync(CreateQuestionDto dto);
    Task<QuestionDto> UpdateAsync(Guid id, CreateQuestionDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ReactToQuestionAsync(Guid userId, QuestionReactionDto dto);
    Task<PaginatedResultDto<ReportedQuestionDto>> GetReportedQuestionsAsync(int page = 1, int pageSize = 20);
    Task<bool> ReviewReportAsync(Guid questionId, bool approve);
}

/// <summary>
/// سرویس مدیریت بازی تک به تک
/// </summary>
public interface IGameService
{
    Task<GameDto> CreateGameAsync(CreateGameDto dto);
    Task<GameDto?> GetByIdAsync(Guid gameId);
    Task<List<GameDto>> GetUserActiveGamesAsync(Guid userId);
    Task<List<GameDto>> GetUserGameHistoryAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<GameRoundDto?> GetCurrentRoundAsync(Guid gameId, Guid? userId = null);
    Task<CategorySuggestionsDto> GetCategorySuggestionsAsync(Guid gameId, int roundNumber);
    Task<bool> ChangeCategorySuggestionsAsync(Guid gameId, int roundNumber);
    Task<GameRoundDto> SelectCategoryAsync(SelectCategoryDto dto);
    Task<AnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitAnswerDto dto);
    Task<bool> CheckAndExpireGamesAsync(); // برای timeout بعد از 5 دقیقه
    Task<int> GetUserActiveGamesCountAsync(Guid userId);
    
    // Matchmaking
    Task<MatchmakingResultDto> JoinMatchmakingQueueAsync(Guid userId);
    Task<MatchmakingResultDto> GetMatchmakingStatusAsync(Guid userId);
    Task LeaveMatchmakingQueueAsync(Guid userId);
}

/// <summary>
/// سرویس چالش روزانه
/// </summary>
public interface IDailyChallengeService
{
    Task<DailyChallengeDto?> GetTodayChallengeAsync(Guid? userId = null);
    Task<List<DailyChallengeQuestionDto>> GetChallengeQuestionsAsync(Guid challengeId);
    Task<AnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitDailyChallengeAnswerDto dto);
    Task<DailyChallengeResultDto?> GetUserResultAsync(Guid challengeId, Guid userId);
    Task<DailyChallengeLeaderboardDto> GetLeaderboardAsync(Guid challengeId, int limit = 100);
    Task<bool> CreateDailyChallengeAsync(); // برای ایجاد چالش روزانه جدید
}

/// <summary>
/// سرویس جایزه روزانه
/// </summary>
public interface IDailyRewardService
{
    Task<DailyRewardStatusDto> GetStatusAsync(Guid userId);
    Task<ClaimDailyRewardResultDto> ClaimRewardAsync(Guid userId);
}

/// <summary>
/// سرویس فروشگاه
/// </summary>
public interface IStoreService
{
    Task<List<StoreItemDto>> GetAllItemsAsync();
    Task<List<StoreItemDto>> GetActiveItemsAsync();
    Task<StoreItemDto?> GetItemByIdAsync(Guid id);
    Task<StoreItemDto> CreateItemAsync(CreateStoreItemDto dto);
    Task<StoreItemDto> UpdateItemAsync(Guid id, CreateStoreItemDto dto);
    Task<bool> DeleteItemAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
    Task<PurchaseResultDto> PurchaseAsync(Guid userId, PurchaseRequestDto dto);
    Task<bool> ConfirmPurchaseAsync(string transactionId, string paymentReferenceId);
    Task<bool> GiftCoinsAsync(Guid senderId, GiftCoinsDto dto);
    Task<List<TransactionDto>> GetUserTransactionsAsync(Guid userId, int page = 1, int pageSize = 20);
}

/// <summary>
/// سرویس آواتار
/// </summary>
public interface IAvatarService
{
    Task<List<AvatarDto>> GetAllAsync();
    Task<List<AvatarDto>> GetDefaultAvatarsAsync();
    Task<AvatarDto?> GetByIdAsync(Guid id);
    Task<bool> PurchaseAvatarAsync(Guid userId, Guid avatarId);
}

/// <summary>
/// سرویس نوتیفیکیشن
/// </summary>
public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task SendNotificationAsync(Guid userId, string title, string message, string? actionUrl = null);
    Task SendPushNotificationAsync(Guid userId, string title, string message);
}
