using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Services;

public class GameService : IGameService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryService _categoryService;
    private readonly IGameSettingsService _gameSettingsService;
    private const int MaxActiveGames = 10;
    private const int RoundsPerGame = 6;
    private const int QuestionsPerRound = 3;
    private const int CategoryChangeCost = 10;

    public GameService(IUnitOfWork unitOfWork, ICategoryService categoryService, IGameSettingsService gameSettingsService)
    {
        _unitOfWork = unitOfWork;
        _categoryService = categoryService;
        _gameSettingsService = gameSettingsService;
    }

    public async Task<GameDto> CreateGameAsync(CreateGameDto dto)
    {
        // بررسی حداکثر بازی‌های همزمان
        var activeGamesCount = await GetUserActiveGamesCountAsync(dto.ChallengerId);
        if (activeGamesCount >= MaxActiveGames)
            throw new Exception("شما نمی‌توانید بیش از 10 بازی همزمان داشته باشید");

        var game = new Game
        {
            Player1Id = dto.ChallengerId,
            Player2Id = dto.OpponentId,
            Status = GameStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Game>().AddAsync(game);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(game.Id) ?? throw new Exception("خطا در ایجاد بازی");
    }

    public async Task<GameDto?> GetByIdAsync(Guid gameId)
    {
        var game = await _unitOfWork.Repository<Game>()
            .Query()
            .Include(g => g.Player1)
            .Include(g => g.Player2)
            .Include(g => g.Winner)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        return game == null ? null : MapToDto(game);
    }

    public async Task<List<GameDto>> GetUserActiveGamesAsync(Guid userId)
    {
        var games = await _unitOfWork.Repository<Game>()
            .Query()
            .Include(g => g.Player1)
            .Include(g => g.Player2)
            .Where(g => (g.Player1Id == userId || g.Player2Id == userId) &&
                        g.Status != GameStatus.Completed && g.Status != GameStatus.Cancelled)
            .OrderByDescending(g => g.LastActivityAt)
            .ToListAsync();

        return games.Select(MapToDto).ToList();
    }

    public async Task<List<GameDto>> GetUserGameHistoryAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var games = await _unitOfWork.Repository<Game>()
            .Query()
            .Include(g => g.Player1)
            .Include(g => g.Player2)
            .Where(g => (g.Player1Id == userId || g.Player2Id == userId) &&
                        g.Status == GameStatus.Completed)
            .OrderByDescending(g => g.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return games.Select(MapToDto).ToList();
    }

    public async Task<GameRoundDto?> GetCurrentRoundAsync(Guid gameId, Guid? userId = null)
    {
        var game = await _unitOfWork.Repository<Game>().GetByIdAsync(gameId);
        if (game == null) return null;

        var round = await _unitOfWork.Repository<GameRound>()
            .Query()
            .Include(r => r.Category)
            .Include(r => r.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(r => r.GameId == gameId && r.RoundNumber == game.CurrentRound);

        if (round == null) return null;

        bool? currentPlayerIsPlayer1 = null;
        if (userId.HasValue)
        {
            round = await EnsureCurrentQuestionStartedAndNotExpiredAsync(game, round, userId.Value);
            currentPlayerIsPlayer1 = game.Player1Id == userId.Value;
        }

        return MapToCurrentQuestionRoundDto(round, currentPlayerIsPlayer1);
    }

    public async Task<CategorySuggestionsDto> GetCategorySuggestionsAsync(Guid gameId, int roundNumber)
    {
        var categories = await _categoryService.GetRandomCategoriesAsync(4);
        return new CategorySuggestionsDto(categories, CategoryChangeCost);
    }

    public async Task<bool> ChangeCategorySuggestionsAsync(Guid gameId, int roundNumber)
    {
        var game = await _unitOfWork.Repository<Game>()
            .Query()
            .Include(g => g.Rounds)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return false;

        // تعیین بازیکنی که نوبت انتخاب موضوع اوست
        var isPlayer1Turn = roundNumber % 2 == 1;
        var currentPlayerId = isPlayer1Turn ? game.Player1Id : game.Player2Id;

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(currentPlayerId);
        if (user == null || user.Coins < CategoryChangeCost) return false;

        // کم کردن سکه
        user.Coins -= CategoryChangeCost;
        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<GameRoundDto> SelectCategoryAsync(SelectCategoryDto dto)
    {
        var game = await _unitOfWork.Repository<Game>().GetByIdAsync(dto.GameId);
        if (game == null)
            throw new Exception("بازی یافت نشد");

        // تعیین بازیکنی که نوبت انتخاب موضوع اوست
        var isPlayer1Turn = dto.RoundNumber % 2 == 1;
        var categorySelectorId = isPlayer1Turn ? game.Player1Id : game.Player2Id;

        // ایجاد راند جدید
        var round = new GameRound
        {
            GameId = dto.GameId,
            RoundNumber = dto.RoundNumber,
            CategoryId = dto.CategoryId,
            CategorySelectorId = categorySelectorId,
            Status = isPlayer1Turn ? RoundStatus.Player1Turn : RoundStatus.Player2Turn
        };

        await _unitOfWork.Repository<GameRound>().AddAsync(round);

        // انتخاب 3 سوال تصادفی از این دسته
        var questions = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Answers)
            .Where(q => q.CategoryId == dto.CategoryId && q.IsActive)
            .OrderBy(q => Guid.NewGuid())
            .Take(QuestionsPerRound)
            .ToListAsync();

        var order = 1;
        foreach (var question in questions)
        {
            var roundQuestion = new RoundQuestion
            {
                GameRoundId = round.Id,
                QuestionId = question.Id,
                QuestionOrder = order++
            };
            await _unitOfWork.Repository<RoundQuestion>().AddAsync(roundQuestion);
        }

        game.Status = GameStatus.InProgress;
        game.LastActivityAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Game>().UpdateAsync(game);
        await _unitOfWork.SaveChangesAsync();

        // بارگیری مجدد راند با روابط
        var createdRound = await _unitOfWork.Repository<GameRound>()
            .Query()
            .Include(r => r.Category)
            .Include(r => r.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(r => r.Id == round.Id);

        var currentTurnUserId = GetCurrentTurnUserId(game, round);
        bool? currentPlayerIsPlayer1 = null;
        if (currentTurnUserId.HasValue)
        {
            currentPlayerIsPlayer1 = game.Player1Id == currentTurnUserId.Value;
            createdRound = await EnsureCurrentQuestionStartedAndNotExpiredAsync(game, createdRound!, currentTurnUserId.Value);
        }

        return MapToCurrentQuestionRoundDto(createdRound!, currentPlayerIsPlayer1);
    }

    public async Task<AnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitAnswerDto dto)
    {
        var game = await _unitOfWork.Repository<Game>()
            .Query()
            .Include(g => g.Rounds)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId);

        if (game == null)
            throw new Exception("بازی یافت نشد");

        var roundQuestion = await _unitOfWork.Repository<RoundQuestion>()
            .Query()
            .Include(rq => rq.Question)
                .ThenInclude(q => q.Answers)
            .Include(rq => rq.GameRound)
            .FirstOrDefaultAsync(rq => rq.Id == dto.RoundQuestionId);

        if (roundQuestion == null)
            throw new Exception("سوال یافت نشد");

        var isPlayer1 = game.Player1Id == userId;
        var round = roundQuestion.GameRound;

        // بررسی نوبت بازیکن
        if (isPlayer1 && round.Status != RoundStatus.Player1Turn)
            throw new Exception("الان نوبت شما نیست");
        if (!isPlayer1 && round.Status != RoundStatus.Player2Turn)
            throw new Exception("الان نوبت شما نیست");

        // بررسی اینکه قبلاً پاسخ نداده باشد
        if (isPlayer1 && roundQuestion.Player1AnswerId.HasValue)
            throw new Exception("شما قبلاً به این سوال پاسخ داده‌اید");
        if (!isPlayer1 && roundQuestion.Player2AnswerId.HasValue)
            throw new Exception("شما قبلاً به این سوال پاسخ داده‌اید");

        var answer = dto.AnswerId.HasValue
            ? roundQuestion.Question.Answers.FirstOrDefault(a => a.Id == dto.AnswerId.Value)
            : null;
        var isCorrect = answer?.IsCorrect ?? false;
        var correctAnswer = roundQuestion.Question.Answers.First(a => a.IsCorrect);

        // محاسبه امتیاز (بر اساس زمان پاسخگویی)
        var score = isCorrect ? CalculateScore(dto.TimeSpent) : 0;

        // ذخیره پاسخ
        if (isPlayer1)
        {
            roundQuestion.Player1AnswerId = dto.AnswerId;
            roundQuestion.Player1TimeSpent = dto.TimeSpent;
            roundQuestion.Player1IsCorrect = isCorrect;
            roundQuestion.Player1Score = score;
        }
        else
        {
            roundQuestion.Player2AnswerId = dto.AnswerId;
            roundQuestion.Player2TimeSpent = dto.TimeSpent;
            roundQuestion.Player2IsCorrect = isCorrect;
            roundQuestion.Player2Score = score;
        }

        // اگر از کمکی استفاده شده، ثبت کنید
        if (dto.HelperUsed.HasValue)
        {
            await UseHelper(game.Id, userId, dto.HelperUsed.Value, dto.RoundQuestionId);
        }

        await _unitOfWork.Repository<RoundQuestion>().UpdateAsync(roundQuestion);
        
        // بررسی آیا همه سوالات این راند توسط این بازیکن پاسخ داده شده
        await CheckAndUpdateRoundStatusAsync(round.Id, isPlayer1);
        
        // بروزرسانی امتیاز راند و بازی
        await UpdateGameScoresAsync(game, roundQuestion.GameRoundId);
        
        game.LastActivityAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Game>().UpdateAsync(game);
        await _unitOfWork.SaveChangesAsync();

        return new AnswerResultDto(isCorrect, score, correctAnswer.Id);
    }

    private async Task CheckAndUpdateRoundStatusAsync(Guid roundId, bool isPlayer1)
    {
        var allQuestions = await _unitOfWork.Repository<RoundQuestion>()
            .Query()
            .Where(rq => rq.GameRoundId == roundId)
            .ToListAsync();

        var round = await _unitOfWork.Repository<GameRound>().GetByIdAsync(roundId);
        if (round == null) return;

        if (isPlayer1)
        {
            // بررسی آیا Player1 همه سوالات را پاسخ داده
            var allAnsweredByPlayer1 = allQuestions.All(rq => rq.Player1AnswerId.HasValue);
            if (allAnsweredByPlayer1 && round.Status == RoundStatus.Player1Turn)
            {
                round.Status = RoundStatus.Player2Turn;
                await _unitOfWork.Repository<GameRound>().UpdateAsync(round);
            }
        }
        else
        {
            // بررسی آیا Player2 همه سوالات را پاسخ داده
            var allAnsweredByPlayer2 = allQuestions.All(rq => rq.Player2AnswerId.HasValue);
            if (allAnsweredByPlayer2 && round.Status == RoundStatus.Player2Turn)
            {
                round.Status = RoundStatus.Completed;
                await _unitOfWork.Repository<GameRound>().UpdateAsync(round);
                
                // بروزرسانی شماره راند فعلی بازی
                var game = await _unitOfWork.Repository<Game>().GetByIdAsync(round.GameId);
                if (game != null && game.CurrentRound == round.RoundNumber && game.CurrentRound < RoundsPerGame)
                {
                    game.CurrentRound++;
                    await _unitOfWork.Repository<Game>().UpdateAsync(game);
                }
            }
        }
    }

    public async Task<bool> CheckAndExpireGamesAsync()
    {
        var settings = await _gameSettingsService.GetSettingsAsync();
        var timeoutThreshold = DateTime.UtcNow.AddHours(-Math.Max(1, settings.OpponentResponseTimeoutHours));

        var expiredGames = await _unitOfWork.Repository<Game>()
            .Query()
            .Where(g => g.Status == GameStatus.WaitingForOpponent &&
                        g.LastActivityAt < timeoutThreshold)
            .ToListAsync();

        foreach (var game in expiredGames)
        {
            game.Status = GameStatus.Timeout;
            // برنده کسی است که منتظر بود
            game.DetermineWinner();
            await _unitOfWork.Repository<Game>().UpdateAsync(game);
        }

        await _unitOfWork.SaveChangesAsync();
        return expiredGames.Any();
    }

    public async Task<int> GetUserActiveGamesCountAsync(Guid userId)
    {
        return await _unitOfWork.Repository<Game>()
            .CountAsync(g => (g.Player1Id == userId || g.Player2Id == userId) &&
                            g.Status != GameStatus.Completed && g.Status != GameStatus.Cancelled);
    }

    private async Task UseHelper(Guid gameId, Guid userId, HelperType helperType, Guid roundQuestionId)
    {
        var coinCost = helperType switch
        {
            HelperType.RemoveTwoOptions => 20,
            HelperType.DoubleAnswer => 15,
            HelperType.AddTime => 10,
            _ => 0
        };

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user != null && user.Coins >= coinCost)
        {
            user.Coins -= coinCost;
            await _unitOfWork.Repository<User>().UpdateAsync(user);

            var usage = new GameHelperUsage
            {
                GameId = gameId,
                UserId = userId,
                HelperType = helperType,
                CoinsCost = coinCost,
                RoundQuestionId = roundQuestionId
            };
            await _unitOfWork.Repository<GameHelperUsage>().AddAsync(usage);
        }
    }

    private async Task UpdateGameScoresAsync(Game game, Guid roundId)
    {
        var roundQuestions = await _unitOfWork.Repository<RoundQuestion>()
            .Query()
            .Where(rq => rq.GameRoundId == roundId)
            .ToListAsync();

        var round = await _unitOfWork.Repository<GameRound>().GetByIdAsync(roundId);
        if (round != null)
        {
            round.Player1Score = roundQuestions.Sum(rq => rq.Player1Score);
            round.Player2Score = roundQuestions.Sum(rq => rq.Player2Score);
            await _unitOfWork.Repository<GameRound>().UpdateAsync(round);
        }

        // بروزرسانی امتیاز کل بازی
        var allRounds = await _unitOfWork.Repository<GameRound>()
            .Query()
            .Where(r => r.GameId == game.Id)
            .ToListAsync();

        game.Player1Score = allRounds.Sum(r => r.Player1Score);
        game.Player2Score = allRounds.Sum(r => r.Player2Score);

        // اگر همه راندها تمام شد، بازی را به اتمام برسانید
        if (allRounds.Count == RoundsPerGame && allRounds.All(r => r.Status == RoundStatus.Completed))
        {
            game.DetermineWinner();
        }
    }

    private static int CalculateScore(int timeSpentMs)
    {
        // حداکثر 15000 میلی‌ثانیه
        const int maxTime = 15000;
        const int maxScore = 100;

        if (timeSpentMs >= maxTime) return 10;

        var timeRatio = 1 - ((double)timeSpentMs / maxTime);
        return (int)(10 + (maxScore - 10) * timeRatio);
    }

    private static GameDto MapToDto(Game game) => new(
        game.Id,
        game.Player1Id,
        game.Player1.Username,
        game.Player1.AvatarUrl,
        game.Player2Id,
        game.Player2.Username,
        game.Player2.AvatarUrl,
        game.Player1Score,
        game.Player2Score,
        game.CurrentRound,
        game.Status,
        game.WinnerId,
        game.CreatedAt,
        game.LastActivityAt
    );

    private static GameRoundDto MapToRoundDto(GameRound round) => new(
        round.Id,
        round.RoundNumber,
        round.CategoryId,
        round.Category.Name,
        round.Status,
        round.Player1Score,
        round.Player2Score,
        round.Questions.Count,
        round.Questions.OrderBy(q => q.QuestionOrder).Select(q => new RoundQuestionDto(
            q.Id,
            q.QuestionId,
            new QuestionDto(
                q.Question.Id,
                q.Question.Text,
                q.Question.ImageUrl,
                q.Question.CategoryId,
                round.Category.Name,
                q.Question.Difficulty,
                q.Question.Answers.OrderBy(a => a.OrderIndex).Select(a => new AnswerDto(a.Id, a.Text, a.OrderIndex)).ToList(),
                q.Question.Answers.First(a => a.IsCorrect).Id
            ),
            q.QuestionOrder,
            q.Player1AnswerId.HasValue ? q.Player1IsCorrect : null,
            q.Player2AnswerId.HasValue ? q.Player2IsCorrect : null,
            q.Player1AnswerId.HasValue ? q.Player1Score : null,
            q.Player2AnswerId.HasValue ? q.Player2Score : null,
            0
        )).ToList()
    );

    private static GameRoundDto MapToCurrentQuestionRoundDto(GameRound round, bool? currentPlayerIsPlayer1)
    {
        var orderedQuestions = round.Questions.OrderBy(q => q.QuestionOrder).ToList();
        var currentQuestion = round.Status switch
        {
            RoundStatus.Player1Turn => orderedQuestions.FirstOrDefault(q => !q.Player1AnswerId.HasValue),
            RoundStatus.Player2Turn => orderedQuestions.FirstOrDefault(q => !q.Player2AnswerId.HasValue),
            _ => orderedQuestions.FirstOrDefault()
        } ?? orderedQuestions.FirstOrDefault();

        if (currentQuestion == null)
            return MapToRoundDto(round);

        var remainingTime = 15000;
        if (currentPlayerIsPlayer1.HasValue && currentQuestion != null)
        {
            if (currentPlayerIsPlayer1.Value)
                remainingTime = GetRemainingTimeMs(currentQuestion, true);
            else
                remainingTime = GetRemainingTimeMs(currentQuestion, false);
        }

        return new GameRoundDto(
            round.Id,
            round.RoundNumber,
            round.CategoryId,
            round.Category.Name,
            round.Status,
            round.Player1Score,
            round.Player2Score,
            orderedQuestions.Count,
            new List<RoundQuestionDto>
            {
                new RoundQuestionDto(
                    currentQuestion.Id,
                    currentQuestion.QuestionId,
                    new QuestionDto(
                        currentQuestion.Question.Id,
                        currentQuestion.Question.Text,
                        currentQuestion.Question.ImageUrl,
                        currentQuestion.Question.CategoryId,
                        round.Category.Name,
                        currentQuestion.Question.Difficulty,
                        currentQuestion.Question.Answers.OrderBy(a => a.OrderIndex).Select(a => new AnswerDto(a.Id, a.Text, a.OrderIndex)).ToList(),
                        currentQuestion.Question.Answers.First(a => a.IsCorrect).Id
                    ),
                    currentQuestion.QuestionOrder,
                    currentQuestion.Player1AnswerId.HasValue ? currentQuestion.Player1IsCorrect : null,
                    currentQuestion.Player2AnswerId.HasValue ? currentQuestion.Player2IsCorrect : null,
                    currentQuestion.Player1AnswerId.HasValue ? currentQuestion.Player1Score : null,
                    currentQuestion.Player2AnswerId.HasValue ? currentQuestion.Player2Score : null,
                    remainingTime
                )
            }
        );
    }

    private static int GetRemainingTimeMs(RoundQuestion question, bool isPlayer1)
    {
        var startedAt = isPlayer1 ? question.Player1StartedAt : question.Player2StartedAt;
        if (!startedAt.HasValue) return 15000;
        var elapsed = (int)(DateTime.UtcNow - startedAt.Value).TotalMilliseconds;
        return Math.Max(0, 15000 - elapsed);
    }

    private async Task<GameRound> EnsureCurrentQuestionStartedAndNotExpiredAsync(Game game, GameRound round, Guid userId)
    {
        var isPlayer1 = game.Player1Id == userId;
        var orderedQuestions = round.Questions.OrderBy(q => q.QuestionOrder).ToList();

        while (true)
        {
            var currentQuestion = round.Status switch
            {
                RoundStatus.Player1Turn => orderedQuestions.FirstOrDefault(q => !q.Player1AnswerId.HasValue),
                RoundStatus.Player2Turn => orderedQuestions.FirstOrDefault(q => !q.Player2AnswerId.HasValue),
                _ => null
            };

            if (currentQuestion == null)
                return round;

var isCurrentTurnUser = (round.Status == RoundStatus.Player1Turn && isPlayer1)
            || (round.Status == RoundStatus.Player2Turn && !isPlayer1);

        if (!isCurrentTurnUser)
        {
            return round;
        }

        var startedAt = isPlayer1 ? currentQuestion.Player1StartedAt : currentQuestion.Player2StartedAt;
            if (!startedAt.HasValue)
            {
                if (isPlayer1)
                    currentQuestion.Player1StartedAt = DateTime.UtcNow;
                else
                    currentQuestion.Player2StartedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<RoundQuestion>().UpdateAsync(currentQuestion);
                await _unitOfWork.SaveChangesAsync();
                return round;
            }

            var elapsed = (DateTime.UtcNow - startedAt.Value).TotalMilliseconds;
            if (elapsed < 15000)
                return round;

            // زمان سوال تمام شده، ذخیره به عنوان رد شده
            if (isPlayer1)
            {
                currentQuestion.Player1AnswerId = null;
                currentQuestion.Player1TimeSpent = 15000;
                currentQuestion.Player1IsCorrect = false;
                currentQuestion.Player1Score = 0;
            }
            else
            {
                currentQuestion.Player2AnswerId = null;
                currentQuestion.Player2TimeSpent = 15000;
                currentQuestion.Player2IsCorrect = false;
                currentQuestion.Player2Score = 0;
            }

            await _unitOfWork.Repository<RoundQuestion>().UpdateAsync(currentQuestion);
            await _unitOfWork.SaveChangesAsync();

            await CheckAndUpdateRoundStatusAsync(round.Id, isPlayer1);

            var gameReloaded = await _unitOfWork.Repository<Game>().GetByIdAsync(game.Id);
            if (gameReloaded != null)
            {
                await UpdateGameScoresAsync(gameReloaded, round.Id);
                await _unitOfWork.Repository<Game>().UpdateAsync(gameReloaded);
                await _unitOfWork.SaveChangesAsync();
            }

            round = await _unitOfWork.Repository<GameRound>()
                .Query()
                .Include(r => r.Category)
                .Include(r => r.Questions)
                    .ThenInclude(q => q.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(r => r.Id == round.Id) ?? round;

            orderedQuestions = round.Questions.OrderBy(q => q.QuestionOrder).ToList();
        }
    }

    private Guid? GetCurrentTurnUserId(Game game, GameRound round)
    {
        return round.Status switch
        {
            RoundStatus.Player1Turn => game.Player1Id,
            RoundStatus.Player2Turn => game.Player2Id,
            _ => null
        };
    }

    #region Matchmaking

    // In-memory matchmaking queue with matched game tracking
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime> _matchmakingQueue = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, (Guid GameId, string OpponentUsername, string OpponentAvatarUrl)> _matchedPlayers = new();

    public async Task<MatchmakingResultDto> JoinMatchmakingQueueAsync(Guid userId)
    {
        // Check if already matched
        if (_matchedPlayers.TryGetValue(userId, out var matchInfo))
        {
            return new MatchmakingResultDto(true, matchInfo.GameId, matchInfo.OpponentUsername, matchInfo.OpponentAvatarUrl, false);
        }

        // Check if already in queue
        if (_matchmakingQueue.ContainsKey(userId))
        {
            return new MatchmakingResultDto(false, null, null, null, true);
        }

        // Ensure user exists in database (create if not)
        var currentUser = await GetOrCreateUserAsync(userId);

        // Try to find an opponent from the queue
        var potentialOpponents = _matchmakingQueue.Keys.Where(id => id != userId).ToList();
        
        if (potentialOpponents.Any())
        {
            var opponentId = potentialOpponents.First();
            
            // Remove opponent from queue
            _matchmakingQueue.TryRemove(opponentId, out _);

            // Get opponent user info (create if not exists)
            var opponent = await GetOrCreateUserAsync(opponentId);

            // Create the game
            var game = new Game
            {
                Player1Id = opponentId, // The one who was waiting first
                Player2Id = userId,
                Status = GameStatus.Pending,
                StartedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Game>().AddAsync(game);
            await _unitOfWork.SaveChangesAsync();

            // Store match info for both players
            _matchedPlayers[userId] = (game.Id, opponent.Username, opponent.AvatarUrl);
            _matchedPlayers[opponentId] = (game.Id, currentUser.Username, currentUser.AvatarUrl);

            return new MatchmakingResultDto(true, game.Id, opponent.Username, opponent.AvatarUrl, false);
        }

        // No opponent found, add to queue
        _matchmakingQueue[userId] = DateTime.UtcNow;
        return new MatchmakingResultDto(false, null, null, null, true);
    }

    private async Task<User> GetOrCreateUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            // Create a placeholder user
            var shortId = userId.ToString()[..4];
            user = new User
            {
                Id = userId,
                Username = $"بازیکن {shortId}",
                Email = $"player{shortId}@quizbattle.local",
                AvatarUrl = "🎮",
                Coins = 100,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
        return user;
    }

    public Task<MatchmakingResultDto> GetMatchmakingStatusAsync(Guid userId)
    {
        // Check if matched
        if (_matchedPlayers.TryRemove(userId, out var matchInfo))
        {
            return Task.FromResult(new MatchmakingResultDto(true, matchInfo.GameId, matchInfo.OpponentUsername, matchInfo.OpponentAvatarUrl, false));
        }

        // Check if still in queue
        var inQueue = _matchmakingQueue.ContainsKey(userId);
        return Task.FromResult(new MatchmakingResultDto(false, null, null, null, inQueue));
    }

    public Task LeaveMatchmakingQueueAsync(Guid userId)
    {
        _matchmakingQueue.TryRemove(userId, out _);
        _matchedPlayers.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    #endregion
}
