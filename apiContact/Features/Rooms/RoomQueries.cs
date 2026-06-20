using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MediatR;

namespace apiContact.Features.Rooms
{
    // ── GetAllRooms ───────────────────────────────────────────
    public record GetAllRoomsQuery : IRequest<List<ChatRoom>>;

    public class GetAllRoomsHandler : IRequestHandler<GetAllRoomsQuery, List<ChatRoom>>
    {
        private readonly IUnitOfWork _uow;
        public GetAllRoomsHandler(IUnitOfWork uow) => _uow = uow;
        public Task<List<ChatRoom>> Handle(GetAllRoomsQuery _, CancellationToken ct)
            => _uow.Rooms.GetAllAsync();
    }

    // ── GetRoomById ───────────────────────────────────────────
    public record GetRoomByIdQuery(string Id) : IRequest<ChatRoom?>;

    public class GetRoomByIdHandler : IRequestHandler<GetRoomByIdQuery, ChatRoom?>
    {
        private readonly IUnitOfWork _uow;
        public GetRoomByIdHandler(IUnitOfWork uow) => _uow = uow;
        public Task<ChatRoom?> Handle(GetRoomByIdQuery q, CancellationToken ct)
            => _uow.Rooms.GetByIdAsync(q.Id);
    }

    // ── GetRoomBySlug ─────────────────────────────────────────
    public record GetRoomBySlugQuery(string Slug) : IRequest<ChatRoom?>;

    public class GetRoomBySlugHandler : IRequestHandler<GetRoomBySlugQuery, ChatRoom?>
    {
        private readonly IUnitOfWork _uow;
        public GetRoomBySlugHandler(IUnitOfWork uow) => _uow = uow;
        public Task<ChatRoom?> Handle(GetRoomBySlugQuery q, CancellationToken ct)
            => _uow.Rooms.GetBySlugAsync(q.Slug);
    }

    // ── GetRoomsByUser ────────────────────────────────────────
    public record GetRoomsByUserQuery(string UserId) : IRequest<List<ChatRoom>>;

    public class GetRoomsByUserHandler : IRequestHandler<GetRoomsByUserQuery, List<ChatRoom>>
    {
        private readonly IUnitOfWork _uow;
        public GetRoomsByUserHandler(IUnitOfWork uow) => _uow = uow;
        public Task<List<ChatRoom>> Handle(GetRoomsByUserQuery q, CancellationToken ct)
            => _uow.Rooms.GetByUserAsync(q.UserId);
    }

    // ── SearchRooms ───────────────────────────────────────────
    public record SearchRoomsQuery(RoomSearchQuery Params) : IRequest<PagedResult<ChatRoom>>;

    public class SearchRoomsHandler : IRequestHandler<SearchRoomsQuery, PagedResult<ChatRoom>>
    {
        private readonly IUnitOfWork _uow;
        public SearchRoomsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<PagedResult<ChatRoom>> Handle(SearchRoomsQuery q, CancellationToken ct)
        {
            q.Params.Clamp();
            var items = await _uow.Rooms.SearchAsync(q.Params);
            var total = await _uow.Rooms.CountSearchAsync(q.Params);
            return PagedResult<ChatRoom>.From(items, total, q.Params.Page, q.Params.PageSize);
        }
    }
}
