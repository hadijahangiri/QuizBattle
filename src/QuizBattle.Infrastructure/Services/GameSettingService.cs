using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Infrastructure.Data;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Services;

public class GameSettingService : IGameSettingsService
{
    private readonly ApplicationDbContext _dbContext;

    public GameSettingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GameSettingsDto> GetSettingsAsync()
    {
        var settingsEntity = await _dbContext.GameSettings.FirstOrDefaultAsync();
        if (settingsEntity is null)
        {
            var defaultSettings = CreateDefaultSettings();
            await CreateDefaultSettingsAsync(defaultSettings);
            return defaultSettings;
        }

        return new GameSettingsDto(settingsEntity.OpponentResponseTimeoutHours);
    }

    public async Task<bool> UpdateSettingsAsync(GameSettingsDto settings)
    {
        try
        {
            var existingSettings = await _dbContext.GameSettings.FirstOrDefaultAsync();
            if (existingSettings is null)
            {
                existingSettings = new GameSettings
                {
                    OpponentResponseTimeoutHours = settings.OpponentResponseTimeoutHours
                };

                await _dbContext.GameSettings.AddAsync(existingSettings);
            }
            else
            {
                existingSettings.OpponentResponseTimeoutHours = settings.OpponentResponseTimeoutHours;
                existingSettings.UpdatedAt = DateTime.UtcNow;
                _dbContext.GameSettings.Update(existingSettings);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateDefaultSettingsAsync(GameSettingsDto settings)
    {
        var entity = new GameSettings
        {
            OpponentResponseTimeoutHours = settings.OpponentResponseTimeoutHours
        };

        await _dbContext.GameSettings.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    private static GameSettingsDto CreateDefaultSettings() => new(12);
}
