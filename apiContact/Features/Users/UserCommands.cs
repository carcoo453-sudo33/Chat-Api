using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MediatR;
using MongoDB.Bson;

namespace apiContact.Features.Users
{
    // ── CreateUser ────────────────────────────────────────────
    public record CreateUserCommand(CreateUserDto Dto) : IRequest<ChatUser>;

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, ChatUser>
    {
        private readonly IUnitOfWork _uow;
        public CreateUserHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatUser> Handle(CreateUserCommand cmd, CancellationToken ct)
        {
            var user = new ChatUser
            {
                Id           = ObjectId.GenerateNewId().ToString(),
                Username     = cmd.Dto.Username.Trim().ToLower(),
                DisplayName  = string.IsNullOrWhiteSpace(cmd.Dto.DisplayName)
                               ? cmd.Dto.Username : cmd.Dto.DisplayName,
                Email        = cmd.Dto.Email.Trim().ToLower(),
                AvatarUrl    = cmd.Dto.AvatarUrl,
                Role         = cmd.Dto.Role,
                PasswordHash = cmd.Dto.Password,  // caller passes pre-hashed value
                CreatedAt    = DateTime.UtcNow
            };
            return await _uow.Users.AddAsync(user);
        }
    }

    // ── UpdateUser ────────────────────────────────────────────
    public record UpdateUserCommand(string Id, UpdateUserDto Dto) : IRequest<ChatUser?>;

    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, ChatUser?>
    {
        private readonly IUnitOfWork _uow;
        public UpdateUserHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatUser?> Handle(UpdateUserCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByIdAsync(cmd.Id);
            if (user is null) return null;
            if (cmd.Dto.DisplayName is not null) user.DisplayName = cmd.Dto.DisplayName;
            if (cmd.Dto.AvatarUrl   is not null) user.AvatarUrl   = cmd.Dto.AvatarUrl;
            if (cmd.Dto.IsOnline.HasValue)
            {
                user.IsOnline = cmd.Dto.IsOnline.Value;
                user.LastSeen = DateTime.UtcNow;
            }
            return await _uow.Users.UpdateAsync(user);
        }
    }

    // ── SetUserStatus ─────────────────────────────────────────
    public record SetUserStatusCommand(string Id, bool IsOnline) : IRequest<bool>;

    public class SetUserStatusHandler : IRequestHandler<SetUserStatusCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public SetUserStatusHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(SetUserStatusCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByIdAsync(cmd.Id);
            if (user is null) return false;
            await _uow.Users.SetStatusAsync(cmd.Id, cmd.IsOnline);
            return true;
        }
    }

    // ── DeleteUser ────────────────────────────────────────────
    public record DeleteUserCommand(string Id) : IRequest<bool>;

    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public DeleteUserHandler(IUnitOfWork uow) => _uow = uow;
        public Task<bool> Handle(DeleteUserCommand cmd, CancellationToken ct)
            => _uow.Users.DeleteAsync(cmd.Id);
    }
}
