using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace QuizBattle.Shared.Components;

public partial class GamePlay
{
    [Parameter] public int GameId { get; set; }
    [Parameter] public EventCallback OnGameEnd { get; set; }

    private GameDto? game;
    private GameRoundDto? currentRound;
    private QuestionDto? currentQuestion;
    private List<CategoryDto> categories = new();
    private int categoriesChangeCost = 10;
    private bool isLoading = true;
    private bool showCategorySelection = false;
    private bool showResult = false;
    
    private int currentQuestionIndex = 1;
    private int totalQuestions = 3;
    private int totalRounds = 6;
    private int timeLeft = 15;
    private int countdownBaseTime = 15;
    private bool countdownExpired = false;
    private Timer? timer;
    private Timer? pollTimer;
    
    private int? selectedAnswer;
    private int? correctAnswerId;
    private List<int> removedAnswers = new();
    private List<int> usedHelpers = new();
    private bool canSelectSecondAnswer = false;
    
    // Post-answer state
    private bool showPostAnswer = false;
    private bool lastAnswerCorrect = false;
    private int lastAnswerScore = 0;
    private int userReaction = 0; // 0=none, 1=like, 2=dislike
    
    // Report modal
    private bool showReportModal = false;
    private string? selectedReportReason;
    private bool reportSubmitted = false;
    private string? reactionFeedback;
    private List<string> reportReasons = new()
    {
        "سوال اشتباه است",
        "جواب صحیح اشتباه است",
        "حاوی الفاظ رکیک",
        "سوال تکراری است",
        "سوال نامفهوم است",
        "سایر موارد"
    };
    
    private int myScore = 0;
    private int opponentScore = 0;
    private string opponentAvatarUrl = "";
    private bool isWinner = false;
    private bool isDraw = false;
    private bool leveledUp = false;
    private int newLevel = 0;
    
    // Quiz of Kings UI
    private string activeTab = "game";
    private List<RoundResultInfo> roundResults = new();
    private bool showingCategoryPicker = false;
    private bool waitingForOpponentTurn = false;

    private class RoundResultInfo
    {
        public int RoundNumber { get; set; }
        public int MyScore { get; set; }
        public int OpponentScore { get; set; }
        public bool IsCompleted { get; set; }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadGame();
    }

    private async Task LoadGame()
    {
        Logger.LogInformation("[GamePlay] LoadGame start - GameId={GameId}", GameId);
        isLoading = true;
        try
        {
            game = await Http.GetFromJsonAsync<GameDto>($"games/{GameId}");
            Logger.LogInformation("[GamePlay] LoadGame response - GameFound={GameFound}", game != null);
            if (game != null)
            {
                Logger.LogInformation("[GamePlay] current game status={Status}, currentRound={CurrentRound}", game.Status, game.CurrentRound);
                opponentAvatarUrl = IsCurrentUserPlayer1()
                    ? game.Player2AvatarUrl 
                    : game.Player1AvatarUrl;

                // بازی تمام شده؟
                if (game.Status == 3) // Completed
                {
                    myScore = IsCurrentUserPlayer1()
                        ? game.Player1Score : game.Player2Score;
                    opponentScore = IsCurrentUserPlayer1()
                        ? game.Player2Score : game.Player1Score;

                    if (game.WinnerId == null)
                    {
                        isDraw = true;
                    }
                    else if (game.WinnerId == AppState.CurrentUserId)
                    {
                        isWinner = true;
                        leveledUp = AppState.AddWin();
                        if (leveledUp) newLevel = AppState.Level;
                    }
                    showResult = true;
                    isLoading = false;
                    return;
                }

                await LoadCurrentRound();
            }
        }
        catch { }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadCurrentRound()
    {
        Logger.LogInformation("[GamePlay] LoadCurrentRound start - GameId={GameId}", GameId);
        try
        {
            var response = await Http.GetAsync($"games/{GameId}/current-round");
            Logger.LogInformation("[GamePlay] LoadCurrentRound status={StatusCode}", response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                var round = await response.Content.ReadFromJsonAsync<GameRoundDto>();
                Logger.LogInformation("[GamePlay] LoadCurrentRound roundFound={RoundFound} status={Status}", round != null, round?.Status);
                if (round != null)
                {
                    currentRound = round;
                    
                    // بررسی آیا نوبت این بازیکن برای پاسخ دادن است
                    if (IsMyTurnToAnswer())
                    {
                        // برگشت به صفحه مسابقه با دکمه انجام بازی - سوال را خودکار نزن
                        StopPolling();
                        currentQuestion = null;
                        showPostAnswer = false;
                        waitingForOpponentTurn = false;
                        showCategorySelection = true;
                        InitWaitingCountdown();
                    }
                    else
                    {
                        // نوبت حریف است - منتظر بمان
                        currentQuestion = null;
                        showPostAnswer = false;
                        waitingForOpponentTurn = true;
                        showCategorySelection = true;
                        InitWaitingCountdown();
                        StartPolling();
                    }
                    return;
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogInformation("[GamePlay] LoadCurrentRound not found - showing categories");
                currentQuestion = null;
                showCategorySelection = true;
                await LoadCategories();
            }
            else
            {
                Logger.LogWarning("[GamePlay] LoadCurrentRound unexpected response {StatusCode} - showing categories", response.StatusCode);
                currentQuestion = null;
                showCategorySelection = true;
                await LoadCategories();
            }

            if (!IsCurrentUserTurn())
            {
                InitWaitingCountdown();
                StartPolling();
            }
            else
            {
                InitWaitingCountdown();
                StopPolling();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[GamePlay] LoadCurrentRound failed");
            currentQuestion = null;
            showCategorySelection = true;
            await LoadCategories();

            if (!IsCurrentUserTurn())
            {
                InitWaitingCountdown();
                StartPolling();
            }
            else
            {
                InitWaitingCountdown();
            }
        }
    }

    private void InitWaitingCountdown()
    {
        if (game == null) return;
        var start = game.LastActivityAt != default ? game.LastActivityAt : game.CreatedAt;
        var startUtc = start.Kind == DateTimeKind.Utc ? start : DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var timeoutHours = game.TimeoutHours > 0 ? game.TimeoutHours : 12;
        countdownBaseTime = timeoutHours * 3600;
        var elapsed = (int)(DateTime.UtcNow - startUtc).TotalSeconds;
        timeLeft = Math.Max(0, countdownBaseTime - elapsed);
        countdownExpired = timeLeft <= 0;
    }

    
    private void StartPolling()
    {
        StopPolling();
        pollTimer = new Timer(3000); // هر ۳ ثانیه چک کن
        pollTimer.Elapsed += async (s, e) => await PollForRound();
        pollTimer.Start();
    }
    
    private void StopPolling()
    {
        pollTimer?.Stop();
        pollTimer?.Dispose();
        pollTimer = null;
    }
    
    private async Task PollForRound()
    {
        Logger.LogInformation("[GamePlay] PollForRound start - GameId={GameId}", GameId);
        try
        {
            var response = await Http.GetAsync($"games/{GameId}/current-round");
            Logger.LogInformation("[GamePlay] PollForRound status={StatusCode}", response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // راند فعلی هنوز ایجاد نشده یا بازی هنوز آغاز نشده؛ poll را ادامه بده
                    return;
                }

                Logger.LogWarning("[GamePlay] PollForRound unexpected status {StatusCode}", response.StatusCode);
                return;
            }

            var round = await response.Content.ReadFromJsonAsync<GameRoundDto>();
            Logger.LogInformation("[GamePlay] PollForRound result roundFound={RoundFound} status={Status}", round != null, round?.Status);
            if (round != null)
            {
                currentRound = round;
                
                // بررسی آیا نوبت این بازیکن برای پاسخ دادن است
                if (IsMyTurnToAnswer())
                {
                    // نوبت پاسخ رسیده - صفحه مسابقه با دکمه انجام بازی نمایش بده
                    StopPolling();
                    currentQuestion = null;
                    showPostAnswer = false;
                    waitingForOpponentTurn = false;
                    showCategorySelection = true;
                    await InvokeAsync(StateHasChanged);
                }
                else if (round.Status == 3) // راند تمام شده - برو به راند بعدی
                {
                    StopPolling();
                    await LoadGame();
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[GamePlay] PollForRound failed");
        }
    }

    private async Task LoadCategories()
    {
        Logger.LogInformation("[GamePlay] LoadCategories start - currentRound={CurrentRound}", game?.CurrentRound);
        try
        {
            var suggestions = await Http.GetFromJsonAsync<CategorySuggestionsDto>($"games/{GameId}/rounds/{game!.CurrentRound}/categories");
            Logger.LogInformation("[GamePlay] LoadCategories suggestionsFetched={HasSuggestions}", suggestions != null);
            if (suggestions != null)
            {
                categories = suggestions.Categories;
                categoriesChangeCost = suggestions.ChangeCost;
                Logger.LogInformation("[GamePlay] LoadCategories categoryCount={Count} changeCost={Cost}", categories.Count, categoriesChangeCost);
            }
            else
            {
                Logger.LogWarning("[GamePlay] LoadCategories no suggestions - loading all categories");
                var allCategories = await Http.GetFromJsonAsync<List<CategoryDto>>("categories");
                categories = allCategories?.Take(4).ToList() ?? new();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[GamePlay] LoadCategories failed - loading all categories fallback");
            try
            {
                var allCategories = await Http.GetFromJsonAsync<List<CategoryDto>>("categories");
                categories = allCategories?.Take(4).ToList() ?? new();
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "[GamePlay] LoadCategories fallback failed");
                categories = new();
            }
        }
    }

    private string? categorySelectionError;

    private async Task SelectCategory(int categoryId)
    {
        Logger.LogInformation("[GamePlay] SelectCategory called - CategoryId={CategoryId}", categoryId);
        categorySelectionError = null;
        try
        {
            var payload = new
            {
                GameId = GameId,
                RoundNumber = game!.CurrentRound,
                CategoryId = categoryId
            };
            Logger.LogInformation("[GamePlay] SelectCategory payload={Payload}", payload);

            var response = await Http.PostAsJsonAsync("games/select-category", payload);
            Logger.LogInformation("[GamePlay] SelectCategory response status={StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                currentRound = await response.Content.ReadFromJsonAsync<GameRoundDto>();
                Logger.LogInformation("[GamePlay] SelectCategory roundLoaded={HasRound} questionsCount={QuestionCount}", currentRound != null, currentRound?.Questions?.Count ?? 0);
                if (currentRound == null || currentRound.Questions == null || !currentRound.Questions.Any())
                {
                    categorySelectionError = "بازی نتوانست سوالی را برای این موضوع بارگذاری کند. دوباره تلاش کنید.";
                }
                else
                {
                    currentQuestionIndex = GetCurrentRoundQuestion()?.QuestionOrder ?? 1;
                    showingCategoryPicker = false;
                    showCategorySelection = false;
                    LoadNextQuestion();
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("[GamePlay] SelectCategory failed response body={Body}", content);
                categorySelectionError = content;
                if (string.IsNullOrWhiteSpace(categorySelectionError))
                {
                    categorySelectionError = "انتخاب موضوع با خطا مواجه شد. لطفاً دوباره تلاش کنید.";
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[GamePlay] SelectCategory exception");
            categorySelectionError = ex.Message;
        }
        StateHasChanged();
    }

    private async Task ChangeCategories()
    {
        if (AppState.Coins < categoriesChangeCost) return;

        var response = await Http.PostAsync($"games/{GameId}/rounds/{game!.CurrentRound}/change-categories", null);
        if (response.IsSuccessStatusCode)
        {
            AppState.Coins -= categoriesChangeCost;
            await LoadCategories();
        }
    }

    private RoundQuestionDto? GetCurrentRoundQuestion()
    {
        if (currentRound?.Questions == null || !currentRound.Questions.Any()) return null;
        return currentRound.Questions.First();
    }

    private void LoadNextQuestion()
    {
        Logger.LogInformation("[GamePlay] LoadNextQuestion start - currentQuestionIndex={Index} questionsCount={Count}", currentQuestionIndex, currentRound?.Questions?.Count ?? 0);
        var rq = GetCurrentRoundQuestion();
        if (rq == null)
        {
            Logger.LogWarning("[GamePlay] LoadNextQuestion no current question available");
            if (currentRound != null && currentRound.Questions != null && !currentRound.Questions.Any())
            {
                categorySelectionError = "در این دسته سوالی برای نمایش وجود ندارد.";
                showCategorySelection = true;
                showingCategoryPicker = false;
            }
            return;
        }

        totalQuestions = currentRound?.TotalQuestions ?? totalQuestions;
        showCategorySelection = false;
        showingCategoryPicker = false;

        Logger.LogInformation("[GamePlay] LoadNextQuestion rq.Question is null: {IsNull}", rq.Question == null);
        currentQuestion = rq.Question;
        if (currentQuestion == null)
        {
            Logger.LogError("[GamePlay] LoadNextQuestion rq.Question is null! Cannot display question.");
            categorySelectionError = "سوال بارگذاری نشد. لطفاً دوباره تلاش کنید.";
            showCategorySelection = true;
            return;
        }

        var remainingMs = rq.RemainingTimeMs;
        timeLeft = remainingMs > 0 ? (int)Math.Ceiling(remainingMs / 1000.0) : 0;
        countdownBaseTime = 15;
        countdownExpired = false;
        Logger.LogInformation("[GamePlay] LoadNextQuestion questionLoaded id={QuestionId} text={QuestionText} remainingMs={RemainingMs}", currentQuestion.Id, currentQuestion.Text, remainingMs);
        selectedAnswer = null;
        correctAnswerId = null;
        removedAnswers.Clear();
        canSelectSecondAnswer = false;
        showPostAnswer = false;
        userReaction = 0;
        reactionFeedback = null;
        showReportModal = false;
        selectedReportReason = null;
        reportSubmitted = false;
        
        if (timeLeft > 0)
        {
            StartTimer();
        }
    }

    private void StartTimer()
    {
        timer?.Dispose();
        timer = new Timer(1000)
        {
            AutoReset = true,
            Enabled = true
        };
        timer.Elapsed += OnTimerTick;
    }

    private int GetCountdownPercent()
    {
        if (countdownBaseTime <= 0) return 0;
        return Math.Max(0, Math.Min(100, (int)Math.Round(timeLeft * 100.0 / countdownBaseTime)));
    }

    private const double ArcRadius = 44.0;
    private static readonly double ArcCircumference = 2 * Math.PI * ArcRadius;

    private string GetArcCircumference() =>
        ArcCircumference.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private string GetArcDashOffset()
    {
        var offset = ArcCircumference * (1.0 - GetCountdownPercent() / 100.0);
        return offset.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string GetArcSparkCx()
    {
        var angle = GetCountdownPercent() / 100.0 * 2 * Math.PI;
        return (50 + ArcRadius * Math.Cos(angle)).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string GetArcSparkCy()
    {
        var angle = GetCountdownPercent() / 100.0 * 2 * Math.PI;
        return (50 + ArcRadius * Math.Sin(angle)).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string GetArcColor()
    {
        var pct = GetCountdownPercent();
        if (pct > 60) return "#00D4FF";
        if (pct > 30) return "#FFA040";
        return "#FF3B30";
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        timeLeft--;
        
        if (timeLeft <= 5)
        {
            // پخش صدای تیک‌تاک
        }

        if (timeLeft <= 0)
        {
            timer?.Stop();
            timeLeft = 0;
            countdownExpired = true;
            InvokeAsync(StateHasChanged);

            _ = Task.Delay(700).ContinueWith(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    if (selectedAnswer == null)
                    {
                        // زمان تمام شد بدون پاسخ
                        await SubmitAnswer(null);
                    }
                });
            });
            return;
        }

        InvokeAsync(StateHasChanged);
    }

    private async Task SubmitAnswer(int? answerId)
    {
        Logger.LogInformation("[GamePlay] SubmitAnswer called - answerId={AnswerId} currentQuestionIndex={Index}", answerId, currentQuestionIndex);
        if (selectedAnswer != null && !canSelectSecondAnswer) return;

        if (canSelectSecondAnswer && selectedAnswer != null)
        {
            // پاسخ دوم
            canSelectSecondAnswer = false;
        }

        selectedAnswer = answerId;
        timer?.Stop();

        var timeSpent = (15 - timeLeft) * 1000;
        lastAnswerCorrect = false;
        lastAnswerScore = 0;

        try
        {
            var roundQuestionId = GetCurrentRoundQuestion()?.Id;
            if (roundQuestionId == null)
            {
                Logger.LogWarning("[GamePlay] SubmitAnswer failed - no current round question found");
                return;
            }

            var response = await Http.PostAsJsonAsync("games/submit-answer", new
            {
                GameId = GameId,
                RoundQuestionId = roundQuestionId,
                AnswerId = answerId,
                TimeSpent = timeSpent
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AnswerResultDto>();
                if (result != null)
                {
                    correctAnswerId = result.CorrectAnswerId;
                    lastAnswerCorrect = result.IsCorrect;
                    lastAnswerScore = result.Score;
                    if (result.IsCorrect)
                    {
                        myScore += result.Score;
                    }
                }
            }
        }
        catch { }

        // نمایش صفحه بعد از پاسخ (1 ثانیه تأخیر برای نمایش رنگ جواب)
        await Task.Delay(1000);
        showPostAnswer = true;
        StateHasChanged();
    }

    private async Task GoToNextQuestion()
    {
        showPostAnswer = false;
        await LoadCurrentRound();
        StateHasChanged();
    }

    // Helper methods for turn-based selection and opponent info
    private bool IsMyTurnToSelectCategory()
    {
        if (game == null) return false;
        
        // راندهای فرد (1، 3، 5): نوبت بازیکن 1
        // راندهای زوج (2، 4، 6): نوبت بازیکن 2
        var isOddRound = game.CurrentRound % 2 == 1;
        var isPlayer1 = IsCurrentUserPlayer1();
        
        return (isOddRound && isPlayer1) || (!isOddRound && !isPlayer1);
    }

    private bool IsMyTurnToAnswer()
    {
        if (game == null || currentRound == null) return false;
        
        var isPlayer1 = IsCurrentUserPlayer1();
        
        // Status: 0=NotStarted, 1=Player1Turn, 2=Player2Turn, 3=Completed
        if (currentRound.Status == 1 && isPlayer1) return true;  // نوبت بازیکن 1
        if (currentRound.Status == 2 && !isPlayer1) return true; // نوبت بازیکن 2
        
        return false;
    }


    private bool IsCurrentUserPlayer1()
    {
        if (game == null) return false;

        if (AppState.CurrentUserId != 0)
        {
            if (game.Player1Id == AppState.CurrentUserId) return true;
            if (game.Player2Id == AppState.CurrentUserId) return false;
        }

        if (!string.IsNullOrWhiteSpace(AppState.Username))
        {
            if (string.Equals(AppState.Username, game.Player1Username, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(AppState.Username, game.Player2Username, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return false;
    }

    private string GetRightPlayerUsername()
    {
        if (game == null) return AppState.Username ?? "شما";
        return IsCurrentUserPlayer1() ? game.Player1Username : game.Player2Username;
    }

    private string GetRightPlayerAvatarUrl()
    {
        if (game == null) return AppState.AvatarUrl ?? "👤";
        return IsCurrentUserPlayer1() ? game.Player1AvatarUrl : game.Player2AvatarUrl;
    }

    private string GetLeftPlayerUsername()
    {
        if (game == null) return "حریف";
        return IsCurrentUserPlayer1() ? game.Player2Username : game.Player1Username;
    }

    private string GetLeftPlayerAvatarUrl()
    {
        if (game == null) return opponentAvatarUrl;
        return IsCurrentUserPlayer1() ? game.Player2AvatarUrl : game.Player1AvatarUrl;
    }

    private string GetMyUsername()
    {
        if (game == null) return AppState.Username ?? "شما";
        return IsCurrentUserPlayer1() ? game.Player1Username : game.Player2Username;
    }

    private string GetMyAvatarUrl()
    {
        if (game == null) return AppState.AvatarUrl ?? "👤";
        return IsCurrentUserPlayer1() ? game.Player1AvatarUrl : game.Player2AvatarUrl;
    }

    private bool IsCurrentUserTurn()
    {
        if(game == null) return false;
        if(currentRound == null){
            return IsMyTurnToSelectCategory();
        }
        if (currentRound.Status == 1) return IsCurrentUserPlayer1();
        if (currentRound.Status == 2) return !IsCurrentUserPlayer1();
        return false;
    }

    private int GetOpponentLevel()
    {
        // فعلاً سطح پیش‌فرض برمی‌گردانیم - بعداً از API می‌گیریم
        return 1;
    }

    private bool IsIconUrl(string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(iconUrl)) return false;
        iconUrl = iconUrl.Trim();
        return iconUrl.StartsWith("http://") 
            || iconUrl.StartsWith("https://") 
            || iconUrl.StartsWith("/") 
            || iconUrl.StartsWith("data:")
            || iconUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || iconUrl.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || iconUrl.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || iconUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private string GetAnswerClass(int answerId)
    {
        if (selectedAnswer == null) return "";
        
        if (answerId == correctAnswerId) return "correct";
        if (answerId == selectedAnswer && answerId != correctAnswerId) return "wrong";
        
        return "";
    }

    private void UseRemoveTwoOptions()
    {
        if (AppState.Coins < 20 || usedHelpers.Contains(1) || currentQuestion == null) return;
        
        AppState.Coins -= 20;
        usedHelpers.Add(1);

        var wrongAnswers = currentQuestion.Answers
            .Where(a => a.Id != currentQuestion.CorrectAnswerId)
            .OrderBy(_ => new Random().Next(1, int.MaxValue))
            .Take(2)
            .Select(a => a.Id)
            .ToList();

        removedAnswers.AddRange(wrongAnswers);
    }

    private void UseDoubleAnswer()
    {
        if (AppState.Coins < 15 || usedHelpers.Contains(2)) return;
        
        AppState.Coins -= 15;
        usedHelpers.Add(2);
        canSelectSecondAnswer = true;
    }

    private void UseAddTime()
    {
        if (AppState.Coins < 10 || usedHelpers.Contains(3)) return;
        
        AppState.Coins -= 10;
        usedHelpers.Add(3);
        timeLeft += 15;
    }

    private async Task LikeQuestion()
    {
        if (userReaction != 0) return; // already reacted
        userReaction = 1;
        await ReactToQuestion(1, null);
        reactionFeedback = "✅ لایک شما ثبت شد!";
        StateHasChanged();
    }

    private async Task DislikeQuestion()
    {
        if (userReaction != 0) return; // already reacted
        userReaction = 2;
        await ReactToQuestion(2, null);
        reactionFeedback = "✅ دیسلایک شما ثبت شد!";
        StateHasChanged();
    }

    private void OpenReportModal()
    {
        selectedReportReason = null;
        reportSubmitted = false;
        showReportModal = true;
    }

    private void CloseReportModal()
    {
        showReportModal = false;
    }

    private async Task SubmitReport()
    {
        if (string.IsNullOrEmpty(selectedReportReason)) return;
        await ReactToQuestion(3, selectedReportReason);
        reportSubmitted = true;
        StateHasChanged();
        await Task.Delay(1500);
        showReportModal = false;
        StateHasChanged();
    }

    private async Task ReactToQuestion(int reaction, string? reportReason)
    {
        try
        {
            await Http.PostAsJsonAsync("questions/react", new
            {
                UserId = AppState.CurrentUserId,
                QuestionId = currentQuestion!.Id,
                Reaction = reaction,
                ReportReason = reportReason
            });
        }
        catch { }
    }

    private string GetPostAnswerClass(int answerId)
    {
        if (answerId == correctAnswerId) return "correct";
        if (answerId == selectedAnswer && answerId != correctAnswerId) return "wrong";
        return "";
    }

    private bool? GetOpponentAnswerStatus(RoundQuestionDto? rq)
    {
        if (rq == null || game == null) return null;
        var isPlayer1 = IsCurrentUserPlayer1();
        if (isPlayer1)
            return rq.Player2IsCorrect;
        else
            return rq.Player1IsCorrect;
    }

    private bool? GetOpponentAnswerForOption(int answerId)
    {
        var rq = GetCurrentRoundQuestion();
        if (rq == null || game == null) return null;

        var isPlayer1 = IsCurrentUserPlayer1();
        var opponentIsCorrect = isPlayer1 ? rq.Player2IsCorrect : rq.Player1IsCorrect;
        if (opponentIsCorrect == null) return null;

        if (answerId == correctAnswerId && opponentIsCorrect == true) return true;
        return null;
    }

    private async Task GoBack()
    {
        StopPolling();
        await OnGameEnd.InvokeAsync();
    }

    private int GetPlayerScore(bool isMe)
    {
        if (game == null) return 0;
        if (isMe)
        {
            return IsCurrentUserPlayer1()
                ? game.Player1Score 
                : game.Player2Score;
        }
        else
        {
            return IsCurrentUserPlayer1()
                ? game.Player2Score 
                : game.Player1Score;
        }
    }

    private async Task ShowCategoryPicker()
    {
        Logger.LogInformation("[GamePlay] ShowCategoryPicker called");
        showingCategoryPicker = true;
        categorySelectionError = null;

        if (!categories.Any())
        {
            await LoadCategories();
        }

        StateHasChanged();
    }

    private void CloseCategoryPicker()
    {
        showingCategoryPicker = false;
        StateHasChanged();
    }

    private bool IsMyTurnToPlay()
    {
        return IsMyTurnToSelectCategory() || IsMyTurnToAnswer();
    }

    private bool CanPlay()
    {
        if (game == null) return false;
        return IsMyTurnToSelectCategory();
    }

    private async Task StartCurrentPlay()
    {
        if (IsMyTurnToSelectCategory())
        {
            await ShowCategoryPicker();
            return;
        }

        if (IsMyTurnToAnswer())
        {
            if (currentRound == null || currentRound.Questions == null || !currentRound.Questions.Any())
            {
                await LoadCurrentRound();
                StateHasChanged();
                return;
            }
            currentQuestionIndex = GetCurrentRoundQuestion()?.QuestionOrder ?? 1;
            showCategorySelection = false;
            waitingForOpponentTurn = false;
            LoadNextQuestion();
            StateHasChanged();
        }
    }

    private async Task PlayNextQuestion()
    {
        if (!CanPlay()) return;

        if (currentRound != null && currentRound.Questions.Any())
        {
            LoadNextQuestion();
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        timer?.Dispose();
        pollTimer?.Dispose();
    }

    private string GetGameTimeoutLabel()
    {
        if (game == null) return "—";

        var start = game.LastActivityAt != default ? game.LastActivityAt : game.CreatedAt;
        if (start == default)
        {
            return "—";
        }

        var startUtc = start.Kind == DateTimeKind.Utc ? start : DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var elapsed = DateTime.UtcNow - startUtc;
        var timeoutHours = game.TimeoutHours > 0 ? game.TimeoutHours : 12;
        var remaining = TimeSpan.FromHours(timeoutHours) - elapsed;

        if (remaining <= TimeSpan.Zero)
        {
            return "زمان تمام شده";
        }

        return remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}"
            : $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private string GetTimeoutWarningClass()
    {
        if (game == null) return string.Empty;

        var start = game.LastActivityAt != default ? game.LastActivityAt : game.CreatedAt;
        if (start == default) return string.Empty;

        var startUtc = start.Kind == DateTimeKind.Utc ? start : DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var elapsed = DateTime.UtcNow - startUtc;
        var timeoutHours = game.TimeoutHours > 0 ? game.TimeoutHours : 12;
        var remaining = TimeSpan.FromHours(timeoutHours) - elapsed;

        return remaining <= TimeSpan.FromMinutes(30) ? "warning" : string.Empty;
    }

    private string GetTurnText()
    {
        if (game == null) return "";
        
        if (IsMyTurnToSelectCategory())
        {
            return "نوبت توئه";
        }
        return "نوبت حریف";
    }

    private string GetRoundClass(int roundNum)
    {
        if (game == null) return "";
        
        if (roundNum < game.CurrentRound) return "completed";
        if (roundNum == game.CurrentRound) return "current";
        return "upcoming";
    }

    private string GetRoundResultIcon(int roundNum, bool isMe)
    {
        if (game == null || roundNum >= game.CurrentRound) return "";
        return "•";
    }

    private string GetRoundScore(int roundNum, bool isMe)
    {
        return "";
    }

    public record GameDto(int Id, int Player1Id, string Player1Username, string Player1AvatarUrl, int Player2Id, string Player2Username, string Player2AvatarUrl,
        int Player1Score, int Player2Score, int CurrentRound, int Status, int? WinnerId,
        int TimeoutHours, DateTime CreatedAt, DateTime LastActivityAt
    );

    public record GameRoundDto(int Id, int RoundNumber, int CategoryId, string CategoryName, string? CategoryIconUrl,
        int Status, int Player1Score, int Player2Score, int TotalQuestions, List<RoundQuestionDto> Questions
    );

    public record RoundQuestionDto(int Id, int QuestionId, QuestionDto Question, int QuestionOrder,
        bool? Player1IsCorrect, bool? Player2IsCorrect, int? Player1Score, int? Player2Score,
        int RemainingTimeMs
    );

    public record QuestionDto(int Id, string Text, string? ImageUrl, int CategoryId, string CategoryName,
        int Difficulty, List<AnswerDto> Answers, int CorrectAnswerId
    );

    public record AnswerDto(int Id, string Text, int OrderIndex);

    public record CategoryDto(int Id, string Name, string? Description, string? IconUrl, int QuestionsCount);

    public record CategorySuggestionsDto(List<CategoryDto> Categories, int ChangeCost);

    public record AnswerResultDto(bool IsCorrect, int Score, int CorrectAnswerId);
}
