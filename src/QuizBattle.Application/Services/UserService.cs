using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByPhoneAsync(string phone)
    {
        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            AvatarUrl = dto.AvatarUrl,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            IsGuest = string.IsNullOrEmpty(dto.Email) && string.IsNullOrEmpty(dto.PhoneNumber)
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserDto> CreateGuestAsync(string username, string avatarUrl)
    {
        var user = new User
        {
            Username = username,
            AvatarUrl = avatarUrl,
            IsGuest = true
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user == null)
            throw new Exception("کاربر یافت نشد");

        if (!string.IsNullOrEmpty(dto.Username))
            user.Username = dto.Username;
        if (!string.IsNullOrEmpty(dto.AvatarUrl))
            user.AvatarUrl = dto.AvatarUrl;
        if (dto.Email != null)
            user.Email = dto.Email;
        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserDto> ConvertGuestToRegularAsync(Guid id, string? email, string? phone)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user == null)
            throw new Exception("کاربر یافت نشد");

        if (!user.IsGuest)
            throw new Exception("کاربر قبلاً ثبت‌نام کرده است");

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            throw new Exception("ایمیل یا شماره موبایل الزامی است");

        user.Email = email;
        user.PhoneNumber = phone;
        user.IsGuest = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> UpdateCoinsAsync(Guid id, int amount)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user == null) return false;

        user.Coins += amount;
        if (user.Coins < 0) user.Coins = 0;

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddScoreAsync(Guid id, int score)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user == null) return false;

        user.Score += score;
        user.TryLevelUp();
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user == null) return null;

        var totalGames = user.TotalWins + user.TotalLosses + user.TotalDraws;
        var winRate = totalGames > 0 ? (int)((double)user.TotalWins / totalGames * 100) : 0;

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.AvatarUrl,
            user.Score,
            user.Level,
            user.TotalWins,
            user.TotalLosses,
            winRate
        );
    }

    public async Task<List<UserProfileDto>> SearchUsersAsync(string query, int limit = 10)
    {
        var users = await _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Username.Contains(query) && !u.IsDeleted)
            .Take(limit)
            .ToListAsync();

        return users.Select(u =>
        {
            var totalGames = u.TotalWins + u.TotalLosses + u.TotalDraws;
            var winRate = totalGames > 0 ? (int)((double)u.TotalWins / totalGames * 100) : 0;
            return new UserProfileDto(
                u.Id,
                u.Username,
                u.AvatarUrl,
                u.Score,
                u.Level,
                u.TotalWins,
                u.TotalLosses,
                winRate
            );
        }).ToList();
    }

    public async Task<bool> HasCompletedTutorialAsync(Guid id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        return user?.HasCompletedTutorial ?? false;
    }

    public async Task MarkTutorialCompletedAsync(Guid id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user != null)
        {
            user.HasCompletedTutorial = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task UpdateDeviceTokenAsync(Guid id, string deviceToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user != null)
        {
            user.DeviceToken = deviceToken;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.PhoneNumber,
        user.AvatarUrl,
        user.IsGuest,
        user.Coins,
        user.Score,
        user.TotalWins,
        user.TotalLosses,
        user.TotalDraws,
        user.Level,
        user.CurrentStreak,
        user.HasCompletedTutorial,
        user.CreatedAt,
        user.LastLoginAt
    );
}
