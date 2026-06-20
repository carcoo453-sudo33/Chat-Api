using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MediatR;

namespace apiContact.Features.Messages
{
    // ── GetMessagesByRoom ─────────────────────────────────────
    public record GetMessagesByRoomQuery(string RoomId, int Limit = 50, int Skip = 0)
        : IRequest<List<Message>>;

    public class GetMessagesByRoomHandler
        : IRequestHandler<GetMessagesByRoomQuery, List<Message>>
    {
        private readonly IUnitOfWork _uow;
        public GetMessagesByRoomHandler(IUnitOfWork uow) => _uow = uow;
        public Task<List<Message>> Handle(GetMessagesByRoomQuery q, CancellationToken ct)
            => _uow.Messages.GetByRoomAsync(q.RoomId, q.Limit, q.Skip);
    }

    // ── GetMessageById ────────────────────────────────────────
    public record GetMessageByIdQuery(string Id) : IRequest<Message?>;

    public class GetMessageByIdHandler : IRequestHandler<GetMessageByIdQuery, Message?>
    {
        private readonly IUnitOfWork _uow;
        public GetMessageByIdHandler(IUnitOfWork uow) => _uow = uow;
        public Task<Message?> Handle(GetMessageByIdQuery q, CancellationToken ct)
            => _uow.Messages.GetByIdAsync(q.Id);
    }

    // ── SearchMessages ────────────────────────────────────────
    public record SearchMessagesQuery(string RoomId, MessageSearchQuery Params)
        : IRequest<PagedResult<Message>>;

    public class SearchMessagesHandler
        : IRequestHandler<SearchMessagesQuery, PagedResult<Message>>
    {
        private readonly IUnitOfWork _uow;
        public SearchMessagesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<PagedResult<Message>> Handle(SearchMessagesQuery q, CancellationToken ct)
        {
            q.Params.Clamp();
            var items = await _uow.Messages.SearchAsync(q.RoomId, q.Params);
            var total = await _uow.Messages.CountSearchAsync(q.RoomId, q.Params);
            return PagedResult<Message>.From(items, total, q.Params.Page, q.Params.PageSize);
        }
    }

    // ── GetUnreadCount ────────────────────────────────────────
    public record GetUnreadCountQuery(string RoomId, string UserId) : IRequest<int>;

    public class GetUnreadCountHandler : IRequestHandler<GetUnreadCountQuery, int>
    {
        private readonly IUnitOfWork _uow;
        public GetUnreadCountHandler(IUnitOfWork uow) => _uow = uow;
        public Task<int> Handle(GetUnreadCountQuery q, CancellationToken ct)
            => _uow.Messages.GetUnreadCountAsync(q.RoomId, q.UserId);
    }
}
