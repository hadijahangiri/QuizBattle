using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Services;

public class DailyChallengeService : IDailyChallengeService
{
    private readonly IUnitOfWork _unitOfWork;
    private const int QuestionsPerChallenge = 10;

    public DailyChallengeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DailyChallengeDto?> GetTodayChallengeAsync(Guid? userId = null)
    {
        var today = DateTime.UtcNow.Date;
        var challenge = await _unitOfWork.Repository<DailyChallenge>()
            .Query()
            .Include(c => c.Questions)
            .Include(c => c.Results)
            .FirstOrDefaultAsync(c => c.ChallengeDate.Date == today && c.IsActive);

        if (challenge == null) return null;

        var hasParticipated = false;
        int? userRank = null;
        int? userScore = null;

        if (userId.HasValue)
        {
            var userResult = challenge.Results.FirstOrDefault(r => r.UserId == userId);
            if (userResult != null)
            {
                hasParticipated = true;
                userRank = userResult.Rank;
                userScore = userResult.Score;
            }
        }

        return new DailyChallengeDto(
            challenge.Id,
            challenge.ChallengeDate,
            challenge.Questions.Count,
            challenge.ParticipantsCount,
            hasParticipated,
            userRank,
            userScore
        );
    }

    public async Task<List<DailyChallengeQuestionDto>> GetChallengeQuestionsAsync(Guid challengeId)
    {
        var questions = await _unitOfWork.Repository<DailyChallengeQuestion>()
            .Query()
            .Include(dcq => dcq.Question)
                .ThenInclude(q => q.Answers)
            .Include(dcq => dcq.Question)
                .ThenInclude(q => q.Category)
            .Where(dcq => dcq.DailyChallengeId == challengeId)
            .OrderBy(dcq => dcq.QuestionOrder)
            .ToListAsync();

        return questions.Select(q => new DailyChallengeQuestionDto(
            q.QuestionOrder,
            new QuestionDto(
                q.Question.Id,
                q.Question.Text,
                q.Question.ImageUrl,
                q.Question.CategoryId,
                q.Question.Category.Name,
                q.Question.Difficulty,
                q.Question.Answers.OrderBy(a => a.OrderIndex)
                    .Select(a => new AnswerDto(a.Id, a.Text, a.OrderIndex)).ToList(),
                q.Question.Answers.First(a => a.IsCorrect).Id
            )
        )).ToList();
    }

    public async Task<AnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitDailyChallengeAnswerDto dto)
    {
        var question = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == dto.QuestionId);

        if (question == null)
            throw new Exception("سوال یافت نشد");

        var answer = question.Answers.FirstOrDefault(a => a.Id == dto.AnswerId);
        var isCorrect = answer?.IsCorrect ?? false;
        var correctAnswer = question.Answers.First(a => a.IsCorrect);

        // محاسبه امتیاز
        var score = isCorrect ? CalculateScore(dto.TimeSpent) : 0;

        // ذخیره یا بروزرسانی نتیجه کاربر
        var result = await _unitOfWork.Repository<DailyChallengeResult>()
            .FirstOrDefaultAsync(r => r.DailyChallengeId == dto.DailyChallengeId && r.UserId == userId);

        if (result == null)
        {
            result = new DailyChallengeResult
            {
                DailyChallengeId = dto.DailyChallengeId,
                UserId = userId,
                CompletedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<DailyChallengeResult>().AddAsync(result);

            // افزایش تعداد شرکت‌کنندگان
            var challenge = await _unitOfWork.Repository<DailyChallenge>().GetByIdAsync(dto.DailyChallengeId);
            if (challenge != null)
            {
                challenge.ParticipantsCount++;
                await _unitOfWork.Repository<DailyChallenge>().UpdateAsync(challenge);
            }
        }

        // ذخیره پاسخ
        var answerRecord = new DailyChallengeAnswer
        {
            DailyChallengeResultId = result.Id,
            QuestionId = dto.QuestionId,
            AnswerId = dto.AnswerId,
            TimeSpent = dto.TimeSpent,
            IsCorrect = isCorrect,
            Score = score
        };
        await _unitOfWork.Repository<DailyChallengeAnswer>().AddAsync(answerRecord);

        // بروزرسانی نتیجه کلی
        result.CorrectAnswers += isCorrect ? 1 : 0;
        result.TotalTimeSpent += dto.TimeSpent;
        result.Score += score;
        await _unitOfWork.Repository<DailyChallengeResult>().UpdateAsync(result);

        await _unitOfWork.SaveChangesAsync();

        // بروزرسانی رتبه‌ها
        await UpdateRanksAsync(dto.DailyChallengeId);

        return new AnswerResultDto(isCorrect, score, correctAnswer.Id);
    }

    public async Task<DailyChallengeResultDto?> GetUserResultAsync(Guid challengeId, Guid userId)
    {
        var result = await _unitOfWork.Repository<DailyChallengeResult>()
            .Query()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.DailyChallengeId == challengeId && r.UserId == userId);

        if (result == null) return null;

        return new DailyChallengeResultDto(
            result.UserId,
            result.User.Username,
            result.User.AvatarUrl,
            result.CorrectAnswers,
            result.TotalTimeSpent,
            result.Score,
            result.Rank
        );
    }

    public async Task<DailyChallengeLeaderboardDto> GetLeaderboardAsync(Guid challengeId, int limit = 100)
    {
        var results = await _unitOfWork.Repository<DailyChallengeResult>()
            .Query()
            .Include(r => r.User)
            .Where(r => r.DailyChallengeId == challengeId)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.TotalTimeSpent)
            .Take(limit)
            .ToListAsync();

        var totalParticipants = await _unitOfWork.Repository<DailyChallengeResult>()
            .CountAsync(r => r.DailyChallengeId == challengeId);

        return new DailyChallengeLeaderboardDto(
            results.Select(r => new DailyChallengeResultDto(
                r.UserId,
                r.User.Username,
                r.User.AvatarUrl,
                r.CorrectAnswers,
                r.TotalTimeSpent,
                r.Score,
                r.Rank
            )).ToList(),
            totalParticipants
        );
    }

    public async Task<bool> CreateDailyChallengeAsync()
    {
        var today = DateTime.UtcNow.Date;
        
        // بررسی اینکه چالش امروز وجود ندارد
        var existingChallenge = await _unitOfWork.Repository<DailyChallenge>()
            .AnyAsync(c => c.ChallengeDate.Date == today);

        if (existingChallenge) return false;

        // ایجاد چالش جدید
        var challenge = new DailyChallenge
        {
            ChallengeDate = today,
            IsActive = true
        };
        await _unitOfWork.Repository<DailyChallenge>().AddAsync(challenge);

        // انتخاب 10 سوال تصادفی
        var questions = await _unitOfWork.Repository<Question>()
            .Query()
            .Where(q => q.IsActive && !q.IsDeleted)
            .OrderBy(q => Guid.NewGuid())
            .Take(QuestionsPerChallenge)
            .ToListAsync();

        var order = 1;
        foreach (var question in questions)
        {
            var challengeQuestion = new DailyChallengeQuestion
            {
                DailyChallengeId = challenge.Id,
                QuestionId = question.Id,
                QuestionOrder = order++
            };
            await _unitOfWork.Repository<DailyChallengeQuestion>().AddAsync(challengeQuestion);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task UpdateRanksAsync(Guid challengeId)
    {
        var results = await _unitOfWork.Repository<DailyChallengeResult>()
            .Query()
            .Where(r => r.DailyChallengeId == challengeId)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.TotalTimeSpent)
            .ToListAsync();

        var rank = 1;
        foreach (var result in results)
        {
            result.Rank = rank++;
            await _unitOfWork.Repository<DailyChallengeResult>().UpdateAsync(result);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static int CalculateScore(int timeSpentMs)
    {
        const int maxTime = 15000;
        const int maxScore = 100;

        if (timeSpentMs >= maxTime) return 10;

        var timeRatio = 1 - ((double)timeSpentMs / maxTime);
        return (int)(10 + (maxScore - 10) * timeRatio);
    }
}
