using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StoreItemDto>>> GetAll()
    {
        var items = await _storeService.GetAllItemsAsync();
        return Ok(items);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<StoreItemDto>>> GetActive()
    {
        var items = await _storeService.GetActiveItemsAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreItemDto>> GetById(Guid id)
    {
        var item = await _storeService.GetItemByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<StoreItemDto>> Create([FromBody] CreateStoreItemDto dto)
    {
        var item = await _storeService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StoreItemDto>> Update(Guid id, [FromBody] CreateStoreItemDto dto)
    {
        try
        {
            var item = await _storeService.UpdateItemAsync(id, dto);
            return Ok(item);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _storeService.DeleteItemAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _storeService.ToggleActiveAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
