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

    // ── SearchUsers ───────────────────────────────────────────
    public record SearchUsersQuery(string Q, int Page = 1, int PageSize = 20)
        : IRequest<PagedResult<ChatUser>>;

    public class SearchUsersHandler : IRequestHandler<SearchUsersQuery, PagedResult<ChatUser>>
    {
        private readonly IUnitOfWork _uow;
        public SearchUsersHandler(IUnitOfWork uow) => _uow = uow;
        public async Task<PagedResult<ChatUser>> Handle(SearchUsersQuery q, CancellationToken ct)
        {
            var pq = new PagedQuery { Page = q.Page, PageSize = q.PageSize };
            pq.Clamp();
            var items = await _uow.Users.SearchAsync(q.Q, pq.Skip, pq.PageSize);
            // total count for pagination
            var total = (await _uow.Users.SearchAsync(q.Q, 0, int.MaxValue)).Count;
            return PagedResult<ChatUser>.From(items, total, pq.Page, pq.PageSize);
        }
    }
}
