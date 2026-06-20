using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MediatR;

namespace apiContact.Features.Users
{
    // ── GetAllUsers ───────────────────────────────────────────
    public record GetAllUsersQuery : IRequest<List<ChatUser>>;

    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<ChatUser>>
    {
        private readonly IUnitOfWork _uow;
        public GetAllUsersHandler(IUnitOfWork uow) => _uow = uow;
        public Task<List<ChatUser>> Handle(GetAllUsersQuery _, CancellationToken ct)
            => _uow.Users.GetAllAsync();
    }

    // ── GetUserById ───────────────────────────────────────────
    public record GetUserByIdQuery(string Id) : IRequest<ChatUser?>;

    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ChatUser?>
    {
        private readonly IUnitOfWork _uow;
        public GetUserByIdHandler(IUnitOfWork uow) => _uow = uow;
        public Task<ChatUser?> Handle(GetUserByIdQuery q, CancellationToken ct)
            => _uow.Users.GetByIdAsync(q.Id);
    }

    // ── GetOnlineUsers ────────────────────────────────────────
    public record GetOnlineUsersQuery : IRequest<List<ChatUser>>;

    public class GetOnlineUsersHandler : IRequestHandler<GetOnlineUsersQuery, List<ChatUser>>
    {
        private readonly IUnitOfWork _uow;
        public GetOnlineUsersHandler(IUnitOfWork uow) => _uow = uow;
        public Task<List<ChatUser>> Handle(GetOnlineUsersQuery _, CancellationToken ct)
            => _uow.Users.GetOnlineUsersAsync();
    }

    // ── GetUsersByRoom ────────────────────────────────────────
    public record GetUsersByRoomQuery(string RoomId) : IRequest<List<ChatUser>>;

    public class GetUsersByRoomHandler : IRequestHandler<GetUsersByRoomQuery, List<ChatUser>>
    {
        private readonly IUnitOfWork _uow;
        public GetUsersByRoomHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<ChatUser>> Handle(GetUsersByRoomQuery q, CancellationToken ct)
        {
            var memberIds = await _uow.Rooms.GetMemberIdsAsync(q.RoomId);
            var users     = new List<ChatUser>(memberIds.Count);
            foreach (var id in memberIds)
            {
                var user = await _uow.Users.GetByIdAsync(id);
                if (user is not null) users.Add(user);
            }
            return users;
        }
    }

    // ── SearchUsers ───────────────────────────────────────────
    public record SearchUsersQuery(UserSearchQuery Params) : IRequest<PagedResult<ChatUser>>;

    public class SearchUsersHandler : IRequestHandler<SearchUsersQuery, PagedResult<ChatUser>>
    {
        private readonly IUnitOfWork _uow;
        public SearchUsersHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<PagedResult<ChatUser>> Handle(SearchUsersQuery q, CancellationToken ct)
        {
            q.Params.Clamp();
            var items = await _uow.Users.SearchAsync(q.Params);
            var total = await _uow.Users.CountSearchAsync(q.Params);
            return PagedResult<ChatUser>.From(items, total, q.Params.Page, q.Params.PageSize);
        }
    }
}
