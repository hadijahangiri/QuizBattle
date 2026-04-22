using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _unitOfWork.Repository<Category>()
            .Query()
            .Include(c => c.Questions)
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        return category == null ? null : MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            IconUrl = dto.IconUrl
        };

        await _unitOfWork.Repository<Category>().AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CreateCategoryDto dto)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null)
            throw new Exception("دسته‌بندی یافت نشد");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IconUrl = dto.IconUrl;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Category>().UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null) return false;

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Category>().UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<CategoryDto>> GetRandomCategoriesAsync(int count = 4)
    {
        var categories = await _unitOfWork.Repository<Category>()
            .Query()
            .Include(c => c.Questions)
            .Where(c => c.IsActive && !c.IsDeleted && c.Questions.Any(q => q.IsActive))
            .OrderBy(c => Guid.NewGuid())
            .Take(count)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    private static CategoryDto MapToDto(Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.IconUrl,
        category.Questions?.Count(q => q.IsActive && !q.IsDeleted) ?? 0
    );
}
