using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupBattlesController : ControllerBase
{
    private readonly IGroupBattleService _groupBattleService;

    public GroupBattlesController(IGroupBattleService groupBattleService)
    {
        _groupBattleService = groupBattleService;
    }

    [HttpPost("request")]
    public async Task<ActionResult<GroupBattleDto>> CreateBattleRequest([FromBody] CreateGroupBattleDto dto)
    {
        try
        {
            var battle = await _groupBattleService.CreateBattleRequestAsync(dto);
            return Ok(battle);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("group/{groupId}/active")]
    public async Task<ActionResult<GroupBattleDto>> GetActiveBattle(Guid groupId)
    {
        var battle = await _groupBattleService.GetActiveBattleAsync(groupId);
        if (battle == null) return NotFound();
        return Ok(battle);
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<GroupBattleDto>>> GetGroupBattles(Guid groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var battles = await _groupBattleService.GetGroupBattlesAsync(groupId, page, pageSize);
        return Ok(battles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GroupBattleDto>> GetById(Guid id)
    {
        var battle = await _groupBattleService.GetByIdAsync(id);
        if (battle == null) return NotFound();
        return Ok(battle);
    }
}
