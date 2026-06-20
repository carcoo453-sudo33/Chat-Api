using System.Security.Claims;
using apiContact.Features.Users;
using apiContact.Models.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator) => _mediator = mediator;

        private string CallerRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? "user";
        private string CallerId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

        /// <summary>List all users</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _mediator.Send(new GetAllUsersQuery());
            var profiles = list.Select(u => new
            {
                u.Id, u.Username, u.DisplayName,
                u.Email, u.AvatarUrl, u.Role,
                Status = u.Status.ToString(),
                u.IsOnline, u.LastSeen, u.CreatedAt
            });
            return Ok(ApiResponse<object>.Ok(profiles, total: list.Count));
        }

        /// <summary>Search users by name or username</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string q         = "",
            [FromQuery] int    page      = 1,
            [FromQuery] int    pageSize  = 20)
        {
            var result = await _mediator.Send(new SearchUsersQuery(q, page, pageSize));
            return Ok(new
            {
                success     = true,
                message     = "Success",
                data        = result.Items.Select(u => new
                {
                    u.Id, u.Username, u.DisplayName,
                    u.AvatarUrl, u.Role,
                    Status = u.Status.ToString(),
                    u.IsOnline
                }),
                total       = result.Total,
                page        = result.Page,
                pageSize    = result.PageSize,
                totalPages  = result.TotalPages,
                hasNext     = result.HasNext,
                hasPrevious = result.HasPrevious
            });
        }

        /// <summary>Get user by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.Email, user.AvatarUrl, user.Role,
                Status = user.Status.ToString(),
                user.IsOnline, user.LastSeen, user.CreatedAt
            }));
        }

        /// <summary>Update a user profile (owner or admin)</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
        {
            if (CallerId != id && !CallerRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var user = await _mediator.Send(new UpdateUserCommand(id, dto));
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.AvatarUrl, user.IsOnline
            }, "Profile updated"));
        }

        /// <summary>Set online / offline status</summary>
        [HttpPost("{id}/status")]
        public async Task<IActionResult> SetStatus(string id, [FromBody] UpdateUserDto dto)
        {
            if (CallerId != id && !CallerRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var ok = await _mediator.Send(new SetUserStatusCommand(id, dto.IsOnline ?? false));
            if (!ok) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new { id, isOnline = dto.IsOnline }, "Status updated"));
        }

        /// <summary>Delete a user (admin only)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteUserCommand(id));
            if (!ok) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "User deleted"));
        }
    }
}
