using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Application.DTOs;
using QuizBattle.Application.Interfaces;
using QuizBattle.Domain.Enums;

namespace QuizBattle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupRequest request)
    {
        try
        {
            var group = await _groupService.CreateAsync(request.OwnerId, request.Data);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<GroupDto>> GetById(int id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        return Ok(group);
    }

    [HttpGet("code/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<GroupDto>> GetByCode(string code)
    {
        var group = await _groupService.GetByUniqueCodeAsync(code);
        if (group == null) return NotFound();
        return Ok(group);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedResultDto<GroupDto>>> Search([FromQuery] GroupSearchDto dto)
    {
        var result = await _groupService.SearchAsync(dto);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<GroupDto>>> GetUserGroups(int userId)
    {
        var groups = await _groupService.GetUserGroupsAsync(userId);
        return Ok(groups);
    }

    [HttpGet("owned/{userId}")]
    public async Task<ActionResult<List<GroupDto>>> GetOwnedGroups(int userId)
    {
        var groups = await _groupService.GetOwnedGroupsAsync(userId);
        return Ok(groups);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GroupDto>> Update(int id, [FromBody] UpdateGroupRequest request)
    {
        try
        {
            var group = await _groupService.UpdateAsync(id, request.UserId, request.Data);
            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
    {
        var result = await _groupService.DeleteAsync(id, userId);
        if (!result) return BadRequest("فقط مالک می‌تواند گروه را حذف کند");
        return NoContent();
    }

    // Members
    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<GroupMemberDto>>> GetMembers(int id)
    {
        var members = await _groupService.GetMembersAsync(id);
        return Ok(members);
    }

    [HttpPost("{id}/request-membership")]
    public async Task<IActionResult> RequestMembership(int id, [FromBody] MembershipRequest request)
    {
        try
        {
            await _groupService.RequestMembershipAsync(id, request.UserId, request.Message);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/membership-requests")]
    public async Task<ActionResult<List<MembershipRequestDto>>> GetMembershipRequests(int id, [FromQuery] int userId)
    {
        try
        {
            var requests = await _groupService.GetMembershipRequestsAsync(id, userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("membership-requests/{requestId}/approve")]
    public async Task<IActionResult> ApproveMembership(int requestId, [FromQuery] int approverId)
    {
        var result = await _groupService.ApproveMembershipAsync(requestId, approverId);
        if (!result) return BadRequest("خطا در تایید عضویت");
        return Ok();
    }

    [HttpPost("membership-requests/{requestId}/reject")]
    public async Task<IActionResult> RejectMembership(int requestId, [FromQuery] int approverId)
    {
        var result = await _groupService.RejectMembershipAsync(requestId, approverId);
        if (!result) return BadRequest("خطا در رد عضویت");
        return Ok();
    }

    [HttpPost("{id}/kick/{memberId}")]
    public async Task<IActionResult> KickMember(int id, int memberId, [FromQuery] int kickerId)
    {
        var result = await _groupService.KickMemberAsync(id, memberId, kickerId);
        if (!result) return BadRequest("خطا در اخراج عضو");
        return Ok();
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id, [FromQuery] int userId)
    {
        try
        {
            var result = await _groupService.LeaveGroupAsync(id, userId);
            if (!result) return BadRequest("خطا در خروج از گروه");
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/promote/{memberId}")]
    public async Task<IActionResult> PromoteMember(int id, int memberId, [FromBody] PromoteRequest request)
    {
        var result = await _groupService.PromoteMemberAsync(id, memberId, request.PromoterId, request.NewRole);
        if (!result) return BadRequest("خطا در ارتقا عضو");
        return Ok();
    }

    // Chat
    [HttpGet("{id}/chat")]
    public async Task<ActionResult<List<GroupChatMessageDto>>> GetChatMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _groupService.GetChatMessagesAsync(id, page, pageSize);
        return Ok(messages);
    }

    [HttpPost("chat")]
    public async Task<ActionResult<GroupChatMessageDto>> SendMessage([FromBody] SendGroupChatRequest request)
    {
        try
        {
            var message = await _groupService.SendMessageAsync(request.UserId, request.Data);
            return Ok(message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("chat/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId, [FromQuery] int userId)
    {
        var result = await _groupService.DeleteMessageAsync(messageId, userId);
        if (!result) return BadRequest("خطا در حذف پیام");
        return NoContent();
    }

    [HttpPost("{id}/toggle-chat")]
    public async Task<IActionResult> ToggleChat(int id, [FromBody] ToggleChatRequest request)
    {
        var result = await _groupService.ToggleChatAsync(id, request.UserId, request.Enabled);
        if (!result) return BadRequest("فقط مالک می‌تواند چت را تغییر دهد");
        return Ok();
    }
}

public record CreateGroupRequest(int OwnerId, CreateGroupDto Data);
public record UpdateGroupRequest(int UserId, UpdateGroupDto Data);
public record MembershipRequest(int UserId, string? Message);
public record PromoteRequest(int PromoterId, GroupRole NewRole);
public record SendGroupChatRequest(int UserId, SendGroupChatDto Data);
public record ToggleChatRequest(int UserId, bool Enabled);
