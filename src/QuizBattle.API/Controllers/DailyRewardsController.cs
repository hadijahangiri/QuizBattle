using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyRewardsController : ControllerBase
{
    private readonly IDailyRewardService _dailyRewardService;

    public DailyRewardsController(IDailyRewardService dailyRewardService)
    {
        _dailyRewardService = dailyRewardService;
    }

    [HttpGet("status/{userId}")]
    public async Task<ActionResult<DailyRewardStatusDto>> GetStatus(Guid userId)
    {
        try
        {
            var status = await _dailyRewardService.GetStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("claim/{userId}")]
    public async Task<ActionResult<ClaimDailyRewardResultDto>> Claim(Guid userId)
    {
        var result = await _dailyRewardService.ClaimRewardAsync(userId);
        return Ok(result);
    }
}
