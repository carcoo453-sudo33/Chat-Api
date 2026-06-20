using apiContact.Data.Repositories;
using apiContact.Mappings;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MediatR;
using BC = BCrypt.Net.BCrypt;

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
            var hash = BC.HashPassword(cmd.Dto.Password);
            var user = UserMapper.FromCreateDto(cmd.Dto, hash);
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
            UserMapper.ApplyUpdate(user, cmd.Dto);
            return await _uow.Users.UpdateAsync(user);
        }
    }

    // ── SetUserStatus ─────────────────────────────────────────
    /// <summary>Set full presence status (Online, Away, Busy, Offline)</summary>
    public record SetUserStatusCommand(string Id, UserStatus Status) : IRequest<bool>;

    public class SetUserStatusHandler : IRequestHandler<SetUserStatusCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        public SetUserStatusHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<bool> Handle(SetUserStatusCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByIdAsync(cmd.Id);
            if (user is null) return false;
            await _uow.Users.SetStatusAsync(cmd.Id, cmd.Status);
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
