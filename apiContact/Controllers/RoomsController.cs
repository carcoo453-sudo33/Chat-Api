using System.Security.Claims;
using apiContact.Features.Rooms;
using apiContact.Features.Users;
using apiContact.Models.Dtos;
using apiContact.Models.Enums;
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

        // ── Discovery ─────────────────────────────────────────────

        /// <summary>Get all rooms (ordered by last activity)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _mediator.Send(new GetAllRoomsQuery());
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Search / filter rooms with full pagination, tags, category and type</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] RoomSearchQuery q)
        {
            var result = await _mediator.Send(new SearchRoomsQuery(q));
            return Ok(PagedApiResponse.From(result));
        }

        /// <summary>List all distinct room categories</summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var cats = await _mediator.Send(new GetRoomCategoriesQuery());
            return Ok(ApiResponse<object>.Ok(cats, total: cats.Count));
        }

        /// <summary>List all distinct room tags</summary>
        [HttpGet("tags")]
        public async Task<IActionResult> GetTags()
        {
            var tags = await _mediator.Send(new GetRoomTagsQuery());
            return Ok(ApiResponse<object>.Ok(tags, total: tags.Count));
        }

        /// <summary>Get rooms the current user belongs to</summary>
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

        // ── Single room ───────────────────────────────────────────

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

        /// <summary>Get member IDs for a room</summary>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(string id)
        {
            var room = await _mediator.Send(new GetRoomByIdQuery(id));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            var memberIds = await _mediator.Send(new GetRoomMembersQuery(id));
            return Ok(ApiResponse<object>.Ok(memberIds, total: memberIds.Count));
        }

        /// <summary>Get full member profiles for a room</summary>
        [HttpGet("{id}/members/profiles")]
        public async Task<IActionResult> GetMemberProfiles(string id)
        {
            var room = await _mediator.Send(new GetRoomByIdQuery(id));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            var users = await _mediator.Send(new GetUsersByRoomQuery(id));
            var profiles = users.Select(u => new
            {
                u.Id, u.Username, u.DisplayName,
                u.AvatarUrl, u.Role,
                Status = u.Status.ToString(),
                u.IsOnline
            });
            return Ok(ApiResponse<object>.Ok(profiles, total: users.Count));
        }

        // ── Mutations ─────────────────────────────────────────────

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

        /// <summary>Update room name, description, category, tags, or privacy</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRoomDto dto)
        {
            var room = await _mediator.Send(new UpdateRoomCommand(id, dto));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(room, "Room updated"));
        }

        /// <summary>Archive or unarchive a room (admin or creator)</summary>
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(string id)
        {
            var room = await _mediator.Send(new ArchiveRoomCommand(id, true));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "Room archived"));
        }

        /// <summary>Unarchive a room (admin or creator)</summary>
        [HttpPost("{id}/unarchive")]
        public async Task<IActionResult> Unarchive(string id)
        {
            var room = await _mediator.Send(new ArchiveRoomCommand(id, false));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "Room unarchived"));
        }

        /// <summary>Current user joins a room</summary>
        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(string id)
        {
            var room = await _mediator.Send(new GetRoomByIdQuery(id));
            if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            if (room.IsPrivate && !CallerRole.Equals(nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var ok = await _mediator.Send(new AddMemberCommand(id, CallerId));
            if (!ok) return Ok(ApiResponse<object>.Ok(new { roomId = id, userId = CallerId }, "Already a member"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId = CallerId }, "Joined room"));
        }

        /// <summary>Current user leaves a room</summary>
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> Leave(string id)
        {
            var ok = await _mediator.Send(new RemoveMemberCommand(id, CallerId));
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId = CallerId }, "Left room"));
        }

        /// <summary>Add a member to a room</summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberDto dto)
        {
            var ok = await _mediator.Send(new AddMemberCommand(id, dto.UserId));
            if (!ok)
                return BadRequest(ApiResponse<object>.Fail("Could not add member — already a member or room not found"));
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
        [Authorize(Roles = "admin,Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteRoomCommand(id));
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "Room deleted"));
        }
    }
}
