using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyChallengesController : ControllerBase
{
    private readonly IDailyChallengeService _dailyChallengeService;

    public DailyChallengesController(IDailyChallengeService dailyChallengeService)
    {
        _dailyChallengeService = dailyChallengeService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<DailyChallengeDto>> GetTodayChallenge([FromQuery] int? userId = null)
    {
        var challenge = await _dailyChallengeService.GetTodayChallengeAsync(userId);
        if (challenge == null) return NotFound("چالش روزانه هنوز ایجاد نشده است");
        return Ok(challenge);
    }

    [HttpGet("{id}/questions")]
    [Authorize]
    public async Task<ActionResult<List<DailyChallengeQuestionDto>>> GetQuestions(int id)
    {
        var questions = await _dailyChallengeService.GetChallengeQuestionsAsync(id);
        return Ok(questions);
    }

    [HttpPost("submit-answer")]
    [Authorize]
    public async Task<ActionResult<AnswerResultDto>> SubmitAnswer([FromBody] SubmitDailyChallengeRequest request)
    {
        try
        {
            var result = await _dailyChallengeService.SubmitAnswerAsync(request.UserId, request.Answer);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/result/{userId}")]
    public async Task<ActionResult<DailyChallengeResultDto>> GetUserResult(int id, int userId)
    {
        var result = await _dailyChallengeService.GetUserResultAsync(id, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/leaderboard")]
    public async Task<ActionResult<DailyChallengeLeaderboardDto>> GetLeaderboard(int id, [FromQuery] int limit = 100)
    {
        var leaderboard = await _dailyChallengeService.GetLeaderboardAsync(id, limit);
        return Ok(leaderboard);
    }
}

public record SubmitDailyChallengeRequest(int UserId, SubmitDailyChallengeAnswerDto Answer);
