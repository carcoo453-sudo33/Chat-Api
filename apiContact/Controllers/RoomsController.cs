using System.Security.Claims;
using apiContact.Features.Rooms;
using apiContact.Models.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Produces("application/json")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public RoomsController(IMediator mediator) => _mediator = mediator;

        private string CallerId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
        private string CallerRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? "user";

        /// <summary>Get all rooms</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _mediator.Send(new GetAllRoomsQuery());
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Search / filter rooms with pagination</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] RoomSearchQuery q)
        {
            var result = await _mediator.Send(new SearchRoomsQuery(q));
            return Ok(new
            {
                success     = true,
                message     = "Success",
                data        = result.Items,
                total       = result.Total,
                page        = result.Page,
                pageSize    = result.PageSize,
                totalPages  = result.TotalPages,
                hasNext     = result.HasNext,
                hasPrevious = result.HasPrevious
            });
        }

        /// <summary>Get rooms for the current user</summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var list = await _mediator.Send(new GetRoomsByUserQuery(CallerId));
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get rooms for a specific user</summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var list = await _mediator.Send(new GetRoomsByUserQuery(userId));
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get room by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var room = await _mediator.Send(new GetRoomByIdQuery(id));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(room));
        }

        /// <summary>Get room by slug</summary>
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var room = await _mediator.Send(new GetRoomBySlugQuery(slug));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(room));
        }

        /// <summary>Create a new room</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResponse<object>.Fail("Room name is required"));
            var room = await _mediator.Send(new CreateRoomCommand(dto, CallerId));
            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                ApiResponse<object>.Ok(room, "Room created"));
        }

        /// <summary>Update room details</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRoomDto dto)
        {
            var room = await _mediator.Send(new UpdateRoomCommand(id, dto));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(room, "Room updated"));
        }

        /// <summary>Add a member to a room</summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberDto dto)
        {
            var ok = await _mediator.Send(new AddMemberCommand(id, dto.UserId));
            if (!ok) return BadRequest(ApiResponse<object>.Fail("Could not add member — already a member or room not found"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId = dto.UserId }, "Member added"));
        }

        /// <summary>Remove a member from a room</summary>
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(string id, string userId)
        {
            var ok = await _mediator.Send(new RemoveMemberCommand(id, userId));
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId }, "Member removed"));
        }

        /// <summary>Delete a room (admin only)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteRoomCommand(id));
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "Room deleted"));
        }
    }
}
