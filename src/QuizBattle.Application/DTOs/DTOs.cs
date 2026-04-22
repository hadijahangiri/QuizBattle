using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.DTOs;

#region User DTOs

public record UserDto(
    int Id,
    string Username,
    string? Email,
    string? PhoneNumber,
    string AvatarUrl,
    bool IsGuest,
    int Coins,
    int Score,
    int TotalWins,
    int TotalLosses,
    int TotalDraws,
    UserLevel Level,
    int CurrentStreak,
    bool HasCompletedTutorial,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record CreateUserDto(
    string Username,
    string AvatarUrl,
    string? Email = null,
    string? PhoneNumber = null
);

public record UpdateUserDto(
    string? Username = null,
    string? AvatarUrl = null,
    string? Email = null,
    string? PhoneNumber = null
);

public record UserProfileDto(
    int Id,
    string Username,
    string AvatarUrl,
    int Score,
    UserLevel Level,
    int TotalWins,
    int TotalLosses,
    int WinRate
);

public record LoginDto(
    string? Email = null,
    string? PhoneNumber = null
);

public record RegisterDto(
    string Username,
    string AvatarUrl,
    string? Email = null,
    string? PhoneNumber = null
);

#endregion

#region Category DTOs

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? IconUrl,
    int QuestionsCount
);

public record CreateCategoryDto(
    string Name,
    string? Description = null,
    string? IconUrl = null
);

#endregion

#region Question DTOs

public record QuestionDto(
    int Id,
    string Text,
    string? ImageUrl, int CategoryId,
    string CategoryName,
    int Difficulty,
    List<AnswerDto> Answers, int CorrectAnswerId
);

public record QuestionWithCorrectAnswerDto(
    int Id,
    string Text,
    string? ImageUrl, int CategoryId,
    string CategoryName,
    int Difficulty,
    string Option1,
    string Option2,
    string Option3,
    string Option4,
    string CorrectAnswer
);

public record AnswerDto(
    int Id,
    string Text,
    int OrderIndex
);

public record AnswerWithCorrectDto(
    int Id,
    string Text,
    bool IsCorrect,
    int OrderIndex
);

public record CreateQuestionDto(
    string Text, int CategoryId,
    int Difficulty,
    string? ImageUrl,
    List<CreateAnswerDto> Answers
);

public record CreateAnswerDto(
    string Text,
    bool IsCorrect
);

public record QuestionReactionDto(
    int QuestionId,
    QuestionReaction Reaction,
    string? ReportReason = null
);

public record ReportedQuestionDto(
    int Id,
    string Text,
    string CategoryName,
    int ReportsCount,
    List<ReportDetailDto> Reports
);

public record ReportDetailDto(
    int UserId,
    string Username,
    string? Reason,
    DateTime ReportedAt
);

#endregion

#region Game DTOs

public record GameDto(
    int Id, int Player1Id,
    string Player1Username,
    string Player1AvatarUrl, int Player2Id,
    string Player2Username,
    string Player2AvatarUrl,
    int Player1Score,
    int Player2Score,
    int CurrentRound,
    GameStatus Status, int? WinnerId,
    int TimeoutHours,
    DateTime CreatedAt,
    DateTime LastActivityAt
);

public record CreateGameDto(
    int ChallengerId, int OpponentId
);

public record GameRoundDto(
    int Id,
    int RoundNumber, int CategoryId,
    string CategoryName,
    RoundStatus Status,
    int Player1Score,
    int Player2Score,
    int TotalQuestions,
    List<RoundQuestionDto> Questions
);

public record RoundQuestionDto(
    int Id, int QuestionId,
    QuestionDto Question,
    int QuestionOrder,
    bool? Player1IsCorrect,
    bool? Player2IsCorrect,
    int? Player1Score,
    int? Player2Score,
    int RemainingTimeMs
);

public record SubmitAnswerDto(
    int GameId, int RoundQuestionId, int? AnswerId,
    int TimeSpent,
    HelperType? HelperUsed = null
);

public record AnswerResultDto(
    bool IsCorrect,
    int Score, int CorrectAnswerId
);

public record SelectCategoryDto(
    int GameId,
    int RoundNumber, int CategoryId
);

public record CategorySuggestionsDto(
    List<CategoryDto> Categories,
    int ChangeCost
);

public record MatchInfoDto(
    int GameId, int OpponentId,
    string OpponentUsername,
    string OpponentAvatarUrl
);

public record MatchmakingResultDto(
    bool IsMatched, int? GameId,
    string? OpponentUsername,
    string? OpponentAvatarUrl,
    bool InQueue
);

#endregion

#region Daily Challenge DTOs

public record DailyChallengeDto(
    int Id,
    DateTime ChallengeDate,
    int QuestionsCount,
    int ParticipantsCount,
    bool HasUserParticipated,
    int? UserRank,
    int? UserScore
);

public record DailyChallengeQuestionDto(
    int QuestionOrder,
    QuestionDto Question
);

public record SubmitDailyChallengeAnswerDto(
    int DailyChallengeId, int QuestionId, int AnswerId,
    int TimeSpent
);

public record DailyChallengeResultDto(
    int UserId,
    string Username,
    string AvatarUrl,
    int CorrectAnswers,
    int TotalTimeSpent,
    int Score,
    int Rank
);

public record DailyChallengeLeaderboardDto(
    List<DailyChallengeResultDto> Results,
    int TotalParticipants
);

#endregion

#region Group DTOs

public record GroupDto(
    int Id,
    string Name,
    string? Description,
    string? LogoUrl,
    string UniqueCode, int OwnerId,
    string OwnerUsername,
    int TotalScore,
    int MembersCount,
    int WinsCount,
    int LossesCount,
    bool IsChatEnabled,
    bool IsPublic,
    string InviteLink
);

public record CreateGroupDto(
    string Name,
    string? Description = null,
    string? LogoUrl = null,
    bool IsChatEnabled = true,
    bool IsPublic = true
);

public record UpdateGroupDto(
    string? Name = null,
    string? Description = null,
    string? LogoUrl = null,
    bool? IsChatEnabled = null,
    bool? IsPublic = null
);

public record GroupMemberDto(
    int Id, int UserId,
    string Username,
    string AvatarUrl,
    GroupRole Role,
    int ContributedScore,
    DateTime JoinedAt
);

public record MembershipRequestDto(
    int Id, int UserId,
    string Username,
    string AvatarUrl,
    UserLevel Level,
    string? Message,
    DateTime RequestedAt
);

public record GroupChatMessageDto(
    int Id, int SenderId,
    string SenderUsername,
    string SenderAvatarUrl,
    string Message,
    DateTime SentAt
);

public record SendGroupChatDto(
    int GroupId,
    string Message
);

public record GroupSearchDto(
    string? Query = null,
    string? UniqueCode = null,
    int Page = 1,
    int PageSize = 20
);

#endregion

#region Group Battle DTOs

public record GroupBattleDto(
    int Id, int Group1Id,
    string Group1Name,
    string? Group1LogoUrl, int? Group2Id,
    string? Group2Name,
    string? Group2LogoUrl,
    int Group1Score,
    int Group2Score,
    int PlayersPerTeam,
    GroupBattleStatus Status, int? WinnerGroupId,
    DateTime? ExpiresAt,
    List<GroupBattlePlayerDto> Players,
    List<GroupBattleMatchDto> Matches
);

public record CreateGroupBattleDto(
    int GroupId, int RequesterId,
    List<int> PlayerIds
);

public record GroupBattlePlayerDto(
    int UserId,
    string Username,
    string AvatarUrl,
    UserLevel Level,
    bool IsGroup1,
    bool HasPlayed,
    int Score
);

public record GroupBattleMatchDto(
    int Id,
    int MatchOrder,
    string Player1Username,
    string Player2Username,
    int Player1Score,
    int Player2Score,
    GameStatus Status
);

#endregion

#region Store DTOs

public record StoreItemDto(
    int Id,
    string Name,
    string? Description,
    int CoinAmount,
    decimal PriceInToman,
    decimal? DiscountedPrice,
    string? ImageUrl,
    bool IsPopular
);

public record PurchaseRequestDto(
    int StoreItemId
);

public record PurchaseResultDto(
    bool IsSuccessful,
    string? PaymentUrl,
    string? TransactionId,
    string? ErrorMessage
);

public record GiftCoinsDto(
    int ReceiverId,
    int CoinAmount,
    string? Message = null
);

public record TransactionDto(
    int Id,
    TransactionType Type,
    int CoinAmount,
    decimal? PriceInToman,
    string? Description,
    DateTime CreatedAt,
    bool IsSuccessful
);

public record CreateStoreItemDto(
    string Name,
    string? Description,
    int CoinAmount,
    decimal PriceInToman,
    string? ImageUrl = null,
    bool IsPopular = false,
    decimal? DiscountPercent = null,
    int OrderIndex = 0
);

#endregion

#region Daily Reward DTOs

public record DailyRewardDto(
    int Day,
    int CoinReward,
    string? SpecialReward,
    bool CanClaim,
    bool IsClaimed
);

public record DailyRewardStatusDto(
    int CurrentStreak,
    List<DailyRewardDto> Rewards,
    int NextRewardDay
);

public record ClaimDailyRewardResultDto(
    bool IsSuccessful,
    int CoinsReceived,
    string? SpecialReward,
    int NewStreak
);

#endregion

#region Avatar DTOs

public record AvatarDto(
    int Id,
    string Name,
    string ImageUrl,
    bool IsDefault,
    bool IsPremium,
    int? CoinPrice
);

#endregion

#region Common DTOs

public record PaginatedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record ApiResponseDto<T>(
    bool IsSuccess,
    T? Data,
    string? Message,
    List<string>? Errors
);

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string? ImageUrl,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAt
);

#endregion
