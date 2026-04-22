using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.AdminPanel.Models;
using QuizBattle.AdminPanel.Services;
using QuizBattle.Application.DTOs;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly IApiClient _apiClient;

    public CategoriesController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _apiClient.GetAllCategoriesAsync();

        var viewModel = new CategoryListViewModel
        {
            Categories = categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IconUrl = c.IconUrl,
                QuestionCount = c.QuestionsCount,
                IsActive = true, // API doesn't return this, default to true
                CreatedAt = DateTime.UtcNow
            }).OrderBy(c => c.Name).ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new CreateCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new CreateCategoryDto(model.Name, model.Description, model.IconUrl);
        var result = await _apiClient.CreateCategoryAsync(dto);

        if (result == null)
        {
            TempData["Error"] = "خطا در ایجاد دسته‌بندی";
            return View(model);
        }

        TempData["Success"] = "دسته‌بندی با موفقیت ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _apiClient.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        var viewModel = new EditCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            IconUrl = category.IconUrl,
            Description = category.Description,
            IsActive = true // API doesn't return this
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new CreateCategoryDto(model.Name, model.Description, model.IconUrl);
        var result = await _apiClient.UpdateCategoryAsync(model.Id, dto);

        if (result == null)
        {
            TempData["Error"] = "خطا در ویرایش دسته‌بندی";
            return View(model);
        }

        TempData["Success"] = "دسته‌بندی با موفقیت ویرایش شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _apiClient.DeleteCategoryAsync(id);
        
        if (!result)
        {
            TempData["Error"] = "خطا در حذف دسته‌بندی. ممکن است دارای سوال باشد.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "دسته‌بندی با موفقیت حذف شد";
        return RedirectToAction(nameof(Index));
    }
}
