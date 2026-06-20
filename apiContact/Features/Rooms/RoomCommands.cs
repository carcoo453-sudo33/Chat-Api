using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Utilities;
using MediatR;
using MongoDB.Bson;

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
            // Auto-include caller
            var dto = cmd.Dto;
            dto.CreatedBy = cmd.CallerId;
            if (!dto.MemberIds.Contains(cmd.CallerId))
                dto.MemberIds.Add(cmd.CallerId);

            // Generate unique slug
            var baseSlug = SlugHelper.Generate(dto.Name);
            var allSlugs = (await _uow.Rooms.GetAllAsync()).Select(r => r.Slug);
            var slug     = SlugHelper.Uniquify(baseSlug, allSlugs);

            var room = new ChatRoom
            {
                Id          = ObjectId.GenerateNewId().ToString(),
                Name        = dto.Name,
                Slug        = slug,
                Description = dto.Description,
                Category    = dto.Category,
                Tags        = dto.Tags,
                Type        = dto.Type,
                IsPrivate   = dto.IsPrivate,
                MemberIds   = dto.MemberIds,
                CreatedBy   = dto.CreatedBy,
                CreatedAt   = DateTime.UtcNow
            };
            return await _uow.Rooms.AddAsync(room);
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
            var dto = cmd.Dto;
            if (dto.Name        is not null) { room.Name = dto.Name; room.Slug = SlugHelper.Generate(dto.Name); }
            if (dto.Description is not null) room.Description = dto.Description;
            if (dto.Category    is not null) room.Category    = dto.Category;
            if (dto.Tags        is not null) room.Tags        = dto.Tags;
            if (dto.IsArchived.HasValue)     room.IsArchived  = dto.IsArchived.Value;
            if (dto.IsPrivate.HasValue)      room.IsPrivate   = dto.IsPrivate.Value;
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
