using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ICategoryService _categoryService;

    public QuestionsController(IQuestionService questionService, ICategoryService categoryService)
    {
        _questionService = questionService;
        _categoryService = categoryService;
    }

    /// <summary>
    /// دریافت همه سوالات با صفحه‌بندی
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<QuestionDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var questions = await _questionService.GetAllAsync(page, pageSize);
        return Ok(questions);
    }

    /// <summary>
    /// دریافت سوال با شناسه
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionDto>> GetById(Guid id)
    {
        var question = await _questionService.GetByIdAsync(id);
        if (question == null) return NotFound();
        return Ok(question);
    }

    /// <summary>
    /// دریافت سوالات یک دسته‌بندی (بدون پاسخ صحیح)
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<List<QuestionDto>>> GetByCategory(Guid categoryId, [FromQuery] int count = 3)
    {
        var questions = await _questionService.GetByCategoryAsync(categoryId, count);
        return Ok(questions);
    }

    /// <summary>
    /// دریافت سوالات یک دسته‌بندی با پاسخ صحیح (برای بازی)
    /// </summary>
    [HttpGet("category/{categoryId}/with-answers")]
    public async Task<ActionResult<List<QuestionWithCorrectAnswerDto>>> GetWithAnswersByCategory(Guid categoryId, [FromQuery] int count = 3)
    {
        var questions = await _questionService.GetWithAnswersByCategoryAsync(categoryId, count);
        return Ok(questions);
    }

    /// <summary>
    /// دریافت همه دسته‌بندی‌ها با سوالات
    /// </summary>
    [HttpGet("categories-with-questions")]
    public async Task<ActionResult<CategoriesWithQuestionsDto>> GetCategoriesWithQuestions([FromQuery] int questionsPerCategory = 10)
    {
        var categories = await _categoryService.GetAllAsync();
        var result = new List<CategoryWithQuestionsDto>();

        foreach (var category in categories)
        {
            var questions = await _questionService.GetWithAnswersByCategoryAsync(category.Id, questionsPerCategory);
            result.Add(new CategoryWithQuestionsDto(
                category.Id,
                category.Name,
                category.Description,
                category.IconUrl,
                questions
            ));
        }

        return Ok(new CategoriesWithQuestionsDto(result));
    }

    /// <summary>
    /// ایجاد سوال جدید
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] CreateQuestionDto dto)
    {
        var question = await _questionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    /// <summary>
    /// ویرایش سوال
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<QuestionDto>> Update(Guid id, [FromBody] CreateQuestionDto dto)
    {
        try
        {
            var question = await _questionService.UpdateAsync(id, dto);
            return Ok(question);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// حذف سوال
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _questionService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// ثبت واکنش به سوال (پسندیدم، نپسندیدم، گزارش)
    /// </summary>
    [HttpPost("react")]
    [Authorize]
    public async Task<IActionResult> React([FromBody] QuestionReactionRequest request)
    {
        var result = await _questionService.ReactToQuestionAsync(request.UserId, request.Reaction);
        if (!result) return BadRequest("خطا در ثبت واکنش");
        return Ok();
    }

    /// <summary>
    /// دریافت سوالات گزارش شده (برای ادمین)
    /// </summary>
    [HttpGet("reported")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResultDto<ReportedQuestionDto>>> GetReported([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _questionService.GetReportedQuestionsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// بررسی گزارش سوال (برای ادمین)
    /// </summary>
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewReport(Guid id, [FromBody] ReviewReportRequest request)
    {
        var result = await _questionService.ReviewReportAsync(id, request.Approve);
        if (!result) return NotFound();
        return Ok();
    }
}

public record QuestionReactionRequest(Guid UserId, QuestionReactionDto Reaction);
public record ReviewReportRequest(bool Approve);
public record CategoryWithQuestionsDto(Guid Id, string Name, string? Description, string? IconUrl, List<QuestionWithCorrectAnswerDto> Questions);
public record CategoriesWithQuestionsDto(List<CategoryWithQuestionsDto> Categories);
