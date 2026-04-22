using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.API.Extensions;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<ActionResult<GameDto>> Create([FromBody] CreateGameDto dto)
    {
        var userId = User.GetCurrentUserId();
        // فقط کاربر جاری می‌تواند بازی ایجاد کند
        if (dto.ChallengerId != userId)
            return Forbid();

        try
        {
            var game = await _gameService.CreateGameAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = game.Id }, game);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameDto>> GetById(int id)
    {
        var userId = User.GetCurrentUserId();
        var game = await _gameService.GetByIdAsync(id);
        if (game == null) return NotFound();
        
        // فقط بازیکنان بازی می‌توانند اطلاعات آن را ببینند
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
            
        return Ok(game);
    }

    [HttpGet("my/active")]
    public async Task<ActionResult<List<GameDto>>> GetMyActiveGames()
    {
        var userId = User.GetCurrentUserId();
        var games = await _gameService.GetUserActiveGamesAsync(userId);
        return Ok(games);
    }

    [HttpGet("my/history")]
    public async Task<ActionResult<List<GameDto>>> GetMyGameHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetCurrentUserId();
        var games = await _gameService.GetUserGameHistoryAsync(userId, page, pageSize);
        return Ok(games);
    }

    [HttpGet("{id}/current-round")]
    public async Task<ActionResult<GameRoundDto>> GetCurrentRound(int id)
    {
        var userId = User.GetCurrentUserId();
        
        // بررسی دسترسی به بازی
        var game = await _gameService.GetByIdAsync(id);
        if (game == null) return NotFound();
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
        
        var round = await _gameService.GetCurrentRoundAsync(id, userId);
        if (round == null) return NotFound();
        return Ok(round);
    }

    [HttpGet("{id}/rounds/{roundNumber}/categories")]
    public async Task<ActionResult<CategorySuggestionsDto>> GetCategorySuggestions(int id, int roundNumber)
    {
        var userId = User.GetCurrentUserId();
        
        // بررسی دسترسی به بازی
        var game = await _gameService.GetByIdAsync(id);
        if (game == null) return NotFound();
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
            
        var suggestions = await _gameService.GetCategorySuggestionsAsync(id, roundNumber);
        return Ok(suggestions);
    }

    [HttpPost("{id}/rounds/{roundNumber}/change-categories")]
    public async Task<ActionResult> ChangeCategorySuggestions(int id, int roundNumber)
    {
        var userId = User.GetCurrentUserId();
        
        // بررسی دسترسی به بازی
        var game = await _gameService.GetByIdAsync(id);
        if (game == null) return NotFound();
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
            
        var result = await _gameService.ChangeCategorySuggestionsAsync(id, roundNumber);
        if (!result) return BadRequest("سکه کافی ندارید");
        return Ok();
    }

    [HttpPost("select-category")]
    public async Task<ActionResult<GameRoundDto>> SelectCategory([FromBody] SelectCategoryDto dto)
    {
        var userId = User.GetCurrentUserId();
        
        // بررسی دسترسی به بازی
        var game = await _gameService.GetByIdAsync(dto.GameId);
        if (game == null) return NotFound();
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
            
        try
        {
            var round = await _gameService.SelectCategoryAsync(dto);
            return Ok(round);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("submit-answer")]
    public async Task<ActionResult<AnswerResultDto>> SubmitAnswer([FromBody] SubmitAnswerDto answer)
    {
        var userId = User.GetCurrentUserId();
        
        // بررسی دسترسی به بازی
        var game = await _gameService.GetByIdAsync(answer.GameId);
        if (game == null) return NotFound();
        if (game.Player1Id != userId && game.Player2Id != userId)
            return Forbid();
            
        try
        {
            var result = await _gameService.SubmitAnswerAsync(userId, answer);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("my/active-count")]
    public async Task<ActionResult<int>> GetMyActiveGamesCount()
    {
        var userId = User.GetCurrentUserId();
        var count = await _gameService.GetUserActiveGamesCountAsync(userId);
        return Ok(count);
    }

    #region Matchmaking

    [HttpPost("matchmaking/join")]
    public async Task<ActionResult<MatchmakingResultDto>> JoinMatchmaking()
    {
        var userId = User.GetCurrentUserId();
        var result = await _gameService.JoinMatchmakingQueueAsync(userId);
        return Ok(result);
    }

    [HttpGet("matchmaking/status")]
    public async Task<ActionResult<MatchmakingResultDto>> GetMatchmakingStatus()
    {
        var userId = User.GetCurrentUserId();
        var result = await _gameService.GetMatchmakingStatusAsync(userId);
        return Ok(result);
    }

    [HttpPost("matchmaking/leave")]
    public async Task<IActionResult> LeaveMatchmaking()
    {
        var userId = User.GetCurrentUserId();
        await _gameService.LeaveMatchmakingQueueAsync(userId);
        return Ok();
    }

    #endregion
}
