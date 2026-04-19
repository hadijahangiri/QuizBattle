using QuizBattle.Application.DTOs;

namespace QuizBattle.Application.Interfaces;

public interface IGameSettingsService
{
    Task<GameSettingsDto> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(GameSettingsDto settings);
}
