using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Services;

public class StoreService : IStoreService
{
    private readonly IUnitOfWork _unitOfWork;

    public StoreService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<StoreItemDto>> GetAllItemsAsync()
    {
        var items = await _unitOfWork.Repository<StoreItem>().GetAllAsync();
        return items.OrderBy(x => x.OrderIndex).Select(MapToDto).ToList();
    }

    public async Task<List<StoreItemDto>> GetActiveItemsAsync()
    {
        var items = await _unitOfWork.Repository<StoreItem>()
            .FindAsync(x => x.IsActive);
        return items.OrderBy(x => x.OrderIndex).Select(MapToDto).ToList();
    }

    public async Task<StoreItemDto?> GetItemByIdAsync(int id)
    {
        var item = await _unitOfWork.Repository<StoreItem>().GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<StoreItemDto> CreateItemAsync(CreateStoreItemDto dto)
    {
        var item = new StoreItem
        {
            Name = dto.Name,
            Description = dto.Description,
            CoinAmount = dto.CoinAmount,
            PriceInToman = dto.PriceInToman,
            ImageUrl = dto.ImageUrl,
            IsPopular = dto.IsPopular,
            DiscountPercent = dto.DiscountPercent,
            OrderIndex = dto.OrderIndex,
            IsActive = true
        };

        await _unitOfWork.Repository<StoreItem>().AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<StoreItemDto> UpdateItemAsync(int id, CreateStoreItemDto dto)
    {
        var item = await _unitOfWork.Repository<StoreItem>().GetByIdAsync(id);
        if (item == null)
            throw new Exception("آیتم یافت نشد");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.CoinAmount = dto.CoinAmount;
        item.PriceInToman = dto.PriceInToman;
        item.ImageUrl = dto.ImageUrl;
        item.IsPopular = dto.IsPopular;
        item.DiscountPercent = dto.DiscountPercent;
        item.OrderIndex = dto.OrderIndex;
        item.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<StoreItem>().UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _unitOfWork.Repository<StoreItem>().GetByIdAsync(id);
        if (item == null) return false;

        await _unitOfWork.Repository<StoreItem>().DeleteAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var item = await _unitOfWork.Repository<StoreItem>().GetByIdAsync(id);
        if (item == null) return false;

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<StoreItem>().UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<PurchaseResultDto> PurchaseAsync(int userId, PurchaseRequestDto dto)
    {
        // TODO: Implement payment gateway integration
        throw new NotImplementedException("پرداخت هنوز پیاده‌سازی نشده است");
    }

    public Task<bool> ConfirmPurchaseAsync(string transactionId, string paymentReferenceId)
    {
        // TODO: Implement payment confirmation
        throw new NotImplementedException("تایید پرداخت هنوز پیاده‌سازی نشده است");
    }

    public Task<bool> GiftCoinsAsync(int senderId, GiftCoinsDto dto)
    {
        // TODO: Implement coin gifting
        throw new NotImplementedException("هدیه سکه هنوز پیاده‌سازی نشده است");
    }

    public async Task<List<TransactionDto>> GetUserTransactionsAsync(int userId, int page = 1, int pageSize = 20)
    {
        var transactions = await _unitOfWork.Repository<Transaction>()
            .FindAsync(t => t.UserId == userId);

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Type,
                t.CoinAmount,
                t.PriceInToman,
                t.Description,
                t.CreatedAt,
                t.IsSuccessful
            ))
            .ToList();
    }

    private static StoreItemDto MapToDto(StoreItem item)
    {
        decimal? discountedPrice = null;
        if (item.DiscountPercent.HasValue && item.DiscountPercent > 0)
        {
            discountedPrice = item.PriceInToman * (1 - item.DiscountPercent.Value / 100);
        }

        return new StoreItemDto(
            item.Id,
            item.Name,
            item.Description,
            item.CoinAmount,
            item.PriceInToman,
            discountedPrice,
            item.ImageUrl,
            item.IsPopular
        );
    }
}
