using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.AdminPanel.Models;
using QuizBattle.AdminPanel.Services;
using QuizBattle.Application.DTOs;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class QuestionsController : Controller
{
    private readonly IApiClient _apiClient;

    public QuestionsController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(int page = 1, Guid? categoryId = null, string? search = null)
    {
        const int pageSize = 20;
        var questions = await _apiClient.GetAllQuestionsAsync(page, 100); // Get more for filtering
        var categories = await _apiClient.GetAllCategoriesAsync();

        var questionsFiltered = questions.AsEnumerable();

        // Apply filters
        if (categoryId.HasValue)
        {
            questionsFiltered = questionsFiltered.Where(q => q.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            questionsFiltered = questionsFiltered.Where(q => q.Text.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = questionsFiltered.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var questionList = questionsFiltered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                ImageUrl = q.ImageUrl,
                CategoryName = q.CategoryName,
                CategoryId = q.CategoryId,
                CorrectAnswer = q.CorrectAnswer,
                Options = new List<string> { q.Option1, q.Option2, q.Option3, q.Option4 },
                TimesUsed = 0,
                CorrectCount = 0,
                WrongCount = 0,
                ReportCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();

        var viewModel = new QuestionListViewModel
        {
            Questions = questionList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            CategoryFilter = categoryId,
            SearchTerm = search
        };

        ViewBag.Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList();

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var categories = await _apiClient.GetAllCategoriesAsync();
        var viewModel = new CreateQuestionViewModel
        {
            Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _apiClient.GetAllCategoriesAsync();
            model.Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList();
            return View(model);
        }

        var options = new[] { model.Option1, model.Option2, model.Option3, model.Option4 };
        var answers = options.Select((opt, i) => new CreateAnswerDto(opt, i == model.CorrectOptionIndex - 1)).ToList();
        
        var dto = new CreateQuestionDto(
            model.Text,
            model.CategoryId,
            1, // Default difficulty
            model.ImageUrl,
            answers
        );

        var result = await _apiClient.CreateQuestionAsync(dto);
        if (result == null)
        {
            TempData["Error"] = "خطا در ایجاد سوال";
            var categories = await _apiClient.GetAllCategoriesAsync();
            model.Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList();
            return View(model);
        }

        TempData["Success"] = "سوال با موفقیت ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var question = await _apiClient.GetQuestionByIdAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        var categories = await _apiClient.GetAllCategoriesAsync();
        var options = new[] { question.Option1, question.Option2, question.Option3, question.Option4 };
        var correctIndex = Array.IndexOf(options, question.CorrectAnswer) + 1;

        var viewModel = new EditQuestionViewModel
        {
            Id = question.Id,
            Text = question.Text,
            ImageUrl = question.ImageUrl,
            CategoryId = question.CategoryId,
            Option1 = question.Option1,
            Option2 = question.Option2,
            Option3 = question.Option3,
            Option4 = question.Option4,
            CorrectOptionIndex = correctIndex > 0 ? correctIndex : 1,
            IsActive = true,
            Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _apiClient.GetAllCategoriesAsync();
            model.Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList();
            return View(model);
        }

        var options = new[] { model.Option1, model.Option2, model.Option3, model.Option4 };
        var answers = options.Select((opt, i) => new CreateAnswerDto(opt, i == model.CorrectOptionIndex - 1)).ToList();
        
        var dto = new CreateQuestionDto(
            model.Text,
            model.CategoryId,
            1, // Default difficulty
            model.ImageUrl,
            answers
        );

        var result = await _apiClient.UpdateQuestionAsync(model.Id, dto);
        if (result == null)
        {
            TempData["Error"] = "خطا در ویرایش سوال";
            var categories = await _apiClient.GetAllCategoriesAsync();
            model.Categories = categories.Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name }).ToList();
            return View(model);
        }

        TempData["Success"] = "سوال با موفقیت ویرایش شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _apiClient.DeleteQuestionAsync(id);
        if (!result)
        {
            TempData["Error"] = "خطا در حذف سوال";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "سوال با موفقیت حذف شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reported(int page = 1)
    {
        // For now, return empty list since API needs to implement this
        var viewModel = new ReportedQuestionListViewModel
        {
            Reports = new List<ReportedQuestionViewModel>(),
            CurrentPage = 1,
            TotalPages = 1
        };

        return View(viewModel);
    }
}
