using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.AdminPanel.Models;
using QuizBattle.AdminPanel.Services;
using QuizBattle.Application.DTOs;

namespace QuizBattle.AdminPanel.Controllers;

[Authorize]
public class ShopController : Controller
{
    private readonly IApiClient _apiClient;

    public ShopController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _apiClient.GetAllStoreItemsAsync();
        
        var viewModel = new StoreListViewModel
        {
            Items = items.Select(i => new StoreItemViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                CoinAmount = i.CoinAmount,
                PriceInToman = i.PriceInToman,
                DiscountedPrice = i.DiscountedPrice,
                ImageUrl = i.ImageUrl,
                IsPopular = i.IsPopular,
                IsActive = true // فعلاً همه فعال در نظر گرفته می‌شوند
            }).ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new CreateStoreItemViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStoreItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new CreateStoreItemDto(
            model.Name,
            model.Description,
            model.CoinAmount,
            model.PriceInToman,
            model.ImageUrl,
            model.IsPopular,
            model.DiscountPercent,
            model.OrderIndex
        );

        var result = await _apiClient.CreateStoreItemAsync(dto);
        if (result == null)
        {
            ModelState.AddModelError("", "خطا در ایجاد بسته سکه");
            return View(model);
        }

        TempData["Success"] = "بسته سکه با موفقیت ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _apiClient.GetStoreItemByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var viewModel = new EditStoreItemViewModel
        {
            Id = id,
            Name = item.Name,
            Description = item.Description,
            CoinAmount = item.CoinAmount,
            PriceInToman = item.PriceInToman,
            ImageUrl = item.ImageUrl,
            IsPopular = item.IsPopular
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditStoreItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new CreateStoreItemDto(
            model.Name,
            model.Description,
            model.CoinAmount,
            model.PriceInToman,
            model.ImageUrl,
            model.IsPopular,
            model.DiscountPercent,
            model.OrderIndex
        );

        var result = await _apiClient.UpdateStoreItemAsync(model.Id, dto);
        if (result == null)
        {
            ModelState.AddModelError("", "خطا در ویرایش بسته سکه");
            return View(model);
        }

        TempData["Success"] = "بسته سکه با موفقیت ویرایش شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _apiClient.DeleteStoreItemAsync(id);
        if (result)
        {
            TempData["Success"] = "بسته سکه با موفقیت حذف شد";
        }
        else
        {
            TempData["Error"] = "خطا در حذف بسته سکه";
        }

        return RedirectToAction(nameof(Index));
    }
}
