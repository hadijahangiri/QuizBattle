using System.ComponentModel.DataAnnotations;

namespace QuizBattle.Application.DTOs;

public record GameSettingsDto(
    [property: Range(1, 168, ErrorMessage = "زمان باید حداقل 1 ساعت و حداکثر 168 ساعت باشد.")]
    int OpponentResponseTimeoutHours
);
