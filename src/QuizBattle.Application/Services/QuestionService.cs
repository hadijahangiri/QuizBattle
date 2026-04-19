using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Services;

public class QuestionService : IQuestionService
{
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<QuestionDto?> GetByIdAsync(Guid id)
    {
        var question = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Category)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

        return question == null ? null : MapToDto(question);
    }

    public async Task<List<QuestionDto>> GetByCategoryAsync(Guid categoryId, int count = 3)
    {
        var questions = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Category)
            .Include(q => q.Answers)
            .Where(q => q.CategoryId == categoryId && q.IsActive && !q.IsDeleted)
            .OrderBy(q => Guid.NewGuid())
            .Take(count)
            .ToListAsync();

        return questions.Select(MapToDto).ToList();
    }

    public async Task<List<QuestionDto>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        var questions = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Category)
            .Include(q => q.Answers)
            .Where(q => q.IsActive && !q.IsDeleted)
            .OrderBy(q => q.Category.Name)
            .ThenBy(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return questions.Select(MapToDto).ToList();
    }

    public async Task<List<QuestionWithCorrectAnswerDto>> GetWithAnswersByCategoryAsync(Guid categoryId, int count = 3)
    {
        var questions = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Category)
            .Where(q => q.CategoryId == categoryId && q.IsActive && !q.IsDeleted)
            .OrderBy(q => Guid.NewGuid())
            .Take(count)
            .ToListAsync();

        return questions.Select(q => new QuestionWithCorrectAnswerDto(
            q.Id,
            q.Text,
            q.ImageUrl,
            q.CategoryId,
            q.Category.Name,
            q.Difficulty,
            q.Option1,
            q.Option2,
            q.Option3,
            q.Option4,
            q.CorrectAnswer
        )).ToList();
    }

    public async Task<QuestionDto> CreateAsync(CreateQuestionDto dto)
    {
        var question = new Question
        {
            Text = dto.Text,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            Difficulty = dto.Difficulty,
            Option1 = dto.Answers.Count > 0 ? dto.Answers[0].Text : "",
            Option2 = dto.Answers.Count > 1 ? dto.Answers[1].Text : "",
            Option3 = dto.Answers.Count > 2 ? dto.Answers[2].Text : "",
            Option4 = dto.Answers.Count > 3 ? dto.Answers[3].Text : "",
            CorrectAnswer = dto.Answers.FirstOrDefault(a => a.IsCorrect)?.Text ?? ""
        };

        // Add Answer entities for compatibility
        for (int i = 0; i < dto.Answers.Count; i++)
        {
            question.Answers.Add(new Answer
            {
                Text = dto.Answers[i].Text,
                IsCorrect = dto.Answers[i].IsCorrect,
                OrderIndex = i
            });
        }

        await _unitOfWork.Repository<Question>().AddAsync(question);
        await _unitOfWork.SaveChangesAsync();

        // Reload to include Category
        var created = await GetByIdAsync(question.Id);
        return created!;
    }

    public async Task<QuestionDto> UpdateAsync(Guid id, CreateQuestionDto dto)
    {
        var question = await _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            throw new Exception("سوال یافت نشد");

        question.Text = dto.Text;
        question.ImageUrl = dto.ImageUrl;
        question.CategoryId = dto.CategoryId;
        question.Difficulty = dto.Difficulty;
        question.Option1 = dto.Answers.Count > 0 ? dto.Answers[0].Text : "";
        question.Option2 = dto.Answers.Count > 1 ? dto.Answers[1].Text : "";
        question.Option3 = dto.Answers.Count > 2 ? dto.Answers[2].Text : "";
        question.Option4 = dto.Answers.Count > 3 ? dto.Answers[3].Text : "";
        question.CorrectAnswer = dto.Answers.FirstOrDefault(a => a.IsCorrect)?.Text ?? "";
        question.UpdatedAt = DateTime.UtcNow;

        // Clear and re-add answers
        question.Answers.Clear();
        for (int i = 0; i < dto.Answers.Count; i++)
        {
            question.Answers.Add(new Answer
            {
                Text = dto.Answers[i].Text,
                IsCorrect = dto.Answers[i].IsCorrect,
                OrderIndex = i
            });
        }

        await _unitOfWork.Repository<Question>().UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync();

        var updated = await GetByIdAsync(id);
        return updated!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var question = await _unitOfWork.Repository<Question>().GetByIdAsync(id);
        if (question == null) return false;

        question.IsDeleted = true;
        question.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Question>().UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReactToQuestionAsync(Guid userId, QuestionReactionDto dto)
    {
        var question = await _unitOfWork.Repository<Question>().GetByIdAsync(dto.QuestionId);
        if (question == null) return false;

        // Check if user already reacted
        var existingReaction = await _unitOfWork.Repository<QuestionReactionEntity>()
            .Query()
            .FirstOrDefaultAsync(r => r.QuestionId == dto.QuestionId && r.UserId == userId);

        if (existingReaction != null)
        {
            // Update existing reaction
            existingReaction.Reaction = dto.Reaction;
            existingReaction.ReportReason = dto.ReportReason;
            existingReaction.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<QuestionReactionEntity>().UpdateAsync(existingReaction);
        }
        else
        {
            // Add new reaction
            var reaction = new QuestionReactionEntity
            {
                QuestionId = dto.QuestionId,
                UserId = userId,
                Reaction = dto.Reaction,
                ReportReason = dto.ReportReason
            };
            await _unitOfWork.Repository<QuestionReactionEntity>().AddAsync(reaction);
        }

        // Update question stats
        switch (dto.Reaction)
        {
            case QuestionReaction.Like:
                question.LikesCount++;
                break;
            case QuestionReaction.Dislike:
                question.DislikesCount++;
                break;
            case QuestionReaction.Report:
                question.ReportCount++;
                break;
        }

        await _unitOfWork.Repository<Question>().UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<PaginatedResultDto<ReportedQuestionDto>> GetReportedQuestionsAsync(int page = 1, int pageSize = 20)
    {
        var query = _unitOfWork.Repository<Question>()
            .Query()
            .Include(q => q.Category)
            .Include(q => q.Reactions)
                .ThenInclude(r => r.User)
            .Where(q => q.ReportCount > 0 && !q.IsDeleted);

        var totalCount = await query.CountAsync();

        var questions = await query
            .OrderByDescending(q => q.ReportCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = questions.Select(q => new ReportedQuestionDto(
            q.Id,
            q.Text,
            q.Category.Name,
            q.ReportCount,
            q.Reactions
                .Where(r => r.Reaction == QuestionReaction.Report)
                .Select(r => new ReportDetailDto(
                    r.UserId,
                    r.User?.Username ?? "ناشناس",
                    r.ReportReason,
                    r.CreatedAt
                ))
                .ToList()
        )).ToList();

        return new PaginatedResultDto<ReportedQuestionDto>(items, totalCount, page, pageSize, (int)Math.Ceiling((double)totalCount / pageSize));
    }

    public async Task<bool> ReviewReportAsync(Guid questionId, bool approve)
    {
        var question = await _unitOfWork.Repository<Question>().GetByIdAsync(questionId);
        if (question == null) return false;

        if (approve)
        {
            // Deactivate the question
            question.IsActive = false;
        }
        
        question.IsReviewed = true;
        question.ReportCount = 0; // Reset report count
        question.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Question>().UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static QuestionDto MapToDto(Question question) => new(
        question.Id,
        question.Text,
        question.ImageUrl,
        question.CategoryId,
        question.Category?.Name ?? "",
        question.Difficulty,
        question.Answers.OrderBy(a => a.OrderIndex).Select(a => new AnswerDto(a.Id, a.Text, a.OrderIndex)).ToList(),
        question.Answers.First(a => a.IsCorrect).Id
    );
}
