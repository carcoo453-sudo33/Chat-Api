using System.Security.Claims;
using apiContact.Features.Messages;
using apiContact.Models.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Produces("application/json")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public MessagesController(IMediator mediator) => _mediator = mediator;

        private string CallerId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
        private string CallerName =>
            User.FindFirstValue("displayName") ?? User.FindFirstValue(ClaimTypes.Name) ?? "";
        private string CallerRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? "user";

        /// <summary>Get paginated message history for a room</summary>
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetByRoom(
            string roomId,
            [FromQuery] int limit = 50,
            [FromQuery] int skip  = 0)
        {
            var list = await _mediator.Send(new GetMessagesByRoomQuery(roomId, limit, skip));
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Search messages in a room</summary>
        [HttpGet("room/{roomId}/search")]
        public async Task<IActionResult> Search(string roomId, [FromQuery] MessageSearchQuery q)
        {
            var result = await _mediator.Send(new SearchMessagesQuery(roomId, q));
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

        /// <summary>Get a single message by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var msg = await _mediator.Send(new GetMessageByIdQuery(id));
            if (msg is null) return NotFound(ApiResponse<object>.Fail("Message not found"));
            return Ok(ApiResponse<object>.Ok(msg));
        }

        /// <summary>Send a message — broadcasts via WebSocket to all room subscribers</summary>
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(ApiResponse<object>.Fail("Message content is required"));

            dto.SenderId = CallerId;
            var msg = await _mediator.Send(new SendMessageCommand(dto, CallerName));
            return CreatedAtAction(nameof(GetById), new { id = msg.Id },
                ApiResponse<object>.Ok(msg, "Message sent"));
        }

        /// <summary>Edit a message (sender only)</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Edit(string id, [FromBody] EditMessageDto dto)
        {
            var existing = await _mediator.Send(new GetMessageByIdQuery(id));
            if (existing is null) return NotFound(ApiResponse<object>.Fail("Message not found"));
            if (existing.SenderId != CallerId) return Forbid();

            var updated = await _mediator.Send(new EditMessageCommand(id, CallerId, dto.Content));
            return Ok(ApiResponse<object>.Ok(updated, "Message edited"));
        }

        /// <summary>Delete a message (sender or admin)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _mediator.Send(new GetMessageByIdQuery(id));
            if (existing is null) return NotFound(ApiResponse<object>.Fail("Message not found"));

            if (existing.SenderId != CallerId && !CallerRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            await _mediator.Send(new DeleteMessageCommand(id, existing.RoomId));
            return Ok(ApiResponse<object>.Ok(new { id }, "Message deleted"));
        }

        /// <summary>Mark a message as read by the current user</summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            await _mediator.Send(new MarkReadCommand(id, CallerId));
            return Ok(ApiResponse<object>.Ok(new { id, userId = CallerId }, "Marked as read"));
        }

        /// <summary>Get unread message count in a room for the current user</summary>
        [HttpGet("room/{roomId}/unread")]
        public async Task<IActionResult> UnreadCount(string roomId)
        {
            var count = await _mediator.Send(new GetUnreadCountQuery(roomId, CallerId));
            return Ok(ApiResponse<object>.Ok(new { roomId, userId = CallerId, unreadCount = count }));
        }

        /// <summary>Add an emoji reaction to a message</summary>
        [HttpPost("{id}/reactions")]
        public async Task<IActionResult> AddReaction(string id, [FromBody] AddReactionDto dto)
        {
            await _mediator.Send(new AddReactionCommand(id, dto.Emoji, CallerId));
            return Ok(ApiResponse<object>.Ok(new { id, dto.Emoji }, "Reaction added"));
        }

        /// <summary>Remove an emoji reaction from a message</summary>
        [HttpDelete("{id}/reactions/{emoji}")]
        public async Task<IActionResult> RemoveReaction(string id, string emoji)
        {
            await _mediator.Send(new RemoveReactionCommand(id, emoji, CallerId));
            return Ok(ApiResponse<object>.Ok(new { id, emoji }, "Reaction removed"));
        }
    }
}
