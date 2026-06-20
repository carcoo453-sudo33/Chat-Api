using apiContact.Data.Repositories;
using apiContact.Mappings;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Utilities;
using MediatR;

namespace apiContact.Features.Rooms
{
    // ── CreateRoom ────────────────────────────────────────────
    public record CreateRoomCommand(CreateRoomDto Dto, string CallerId) : IRequest<ChatRoom>;

    public class CreateRoomHandler : IRequestHandler<CreateRoomCommand, ChatRoom>
    {
        private readonly IUnitOfWork _uow;
        public CreateRoomHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatRoom> Handle(CreateRoomCommand cmd, CancellationToken ct)
        {
            var slug = await ResolveUniqueSlugAsync(SlugHelper.Generate(cmd.Dto.Name));
            var room = RoomMapper.FromCreateDto(cmd.Dto, cmd.CallerId, slug);
            return await _uow.Rooms.AddAsync(room);
        }

        /// <summary>
        /// Generates a slug from the base, appending an incrementing suffix until it is unique.
        /// Uses a single DB call per attempt — no full-table load.
        /// </summary>
        private async Task<string> ResolveUniqueSlugAsync(string baseSlug)
        {
            var slug = baseSlug;
            if (!await _uow.Rooms.SlugExistsAsync(slug)) return slug;

            int counter = 2;
            do { slug = $"{baseSlug}-{counter++}"; }
            while (await _uow.Rooms.SlugExistsAsync(slug));

            return slug;
        }
    }

    // ── UpdateRoom ────────────────────────────────────────────
    public record UpdateRoomCommand(string Id, UpdateRoomDto Dto) : IRequest<ChatRoom?>;

    public class UpdateRoomHandler : IRequestHandler<UpdateRoomCommand, ChatRoom?>
    {
        private readonly IUnitOfWork _uow;
        public UpdateRoomHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatRoom?> Handle(UpdateRoomCommand cmd, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(cmd.Id);
            if (room is null) return null;

            string? newSlug = null;
            if (cmd.Dto.Name is not null)
            {
                var baseSlug = SlugHelper.Generate(cmd.Dto.Name);
                newSlug = baseSlug;
                if (newSlug != room.Slug && await _uow.Rooms.SlugExistsAsync(newSlug))
                {
                    int counter = 2;
                    do { newSlug = $"{baseSlug}-{counter++}"; }
                    while (await _uow.Rooms.SlugExistsAsync(newSlug));
                }
            }

            RoomMapper.ApplyUpdate(room, cmd.Dto, newSlug);
            return await _uow.Rooms.UpdateAsync(room);
        }
    }

    // ── ArchiveRoom ───────────────────────────────────────────
    public record ArchiveRoomCommand(string Id, bool Archive = true) : IRequest<ChatRoom?>;

    public class ArchiveRoomHandler : IRequestHandler<ArchiveRoomCommand, ChatRoom?>
    {
        private readonly IUnitOfWork _uow;
        public ArchiveRoomHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatRoom?> Handle(ArchiveRoomCommand cmd, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(cmd.Id);
            if (room is null) return null;
            room.IsArchived = cmd.Archive;
            room.UpdatedAt  = DateTime.UtcNow;
            return await _uow.Rooms.UpdateAsync(room);
        }
    }

    // ── DeleteRoom ────────────────────────────────────────────
    public record DeleteRoomCommand(string Id) : IRequest<bool>;

    public class DeleteRoomHandler : IRequestHandler<DeleteRoomCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public DeleteRoomHandler(IUnitOfWork uow) => _uow = uow;
        public Task<bool> Handle(DeleteRoomCommand cmd, CancellationToken ct)
            => _uow.Rooms.DeleteAsync(cmd.Id);
    }

    // ── AddMember ─────────────────────────────────────────────
    public record AddMemberCommand(string RoomId, string UserId) : IRequest<bool>;

    public class AddMemberHandler : IRequestHandler<AddMemberCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public AddMemberHandler(IUnitOfWork uow) => _uow = uow;
        public Task<bool> Handle(AddMemberCommand cmd, CancellationToken ct)
            => _uow.Rooms.AddMemberAsync(cmd.RoomId, cmd.UserId);
    }

    // ── RemoveMember ──────────────────────────────────────────
    public record RemoveMemberCommand(string RoomId, string UserId) : IRequest<bool>;

    public class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public RemoveMemberHandler(IUnitOfWork uow) => _uow = uow;
        public Task<bool> Handle(RemoveMemberCommand cmd, CancellationToken ct)
            => _uow.Rooms.RemoveMemberAsync(cmd.RoomId, cmd.UserId);
    }
}
