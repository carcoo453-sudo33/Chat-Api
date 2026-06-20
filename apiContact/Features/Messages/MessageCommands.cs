using apiContact.Data.Repositories;
using apiContact.Hubs;
using apiContact.Mappings;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace apiContact.Features.Messages
{
    // ── SendMessage ───────────────────────────────────────────
    public record SendMessageCommand(SendMessageDto Dto, string SenderName)
        : IRequest<Message>;

    public class SendMessageHandler : IRequestHandler<SendMessageCommand, Message>
    {
        private readonly IUnitOfWork          _uow;
        private readonly IHubContext<ChatHub> _hub;

        public SendMessageHandler(IUnitOfWork uow, IHubContext<ChatHub> hub)
        {
            _uow = uow;
            _hub = hub;
        }

        public async Task<Message> Handle(SendMessageCommand cmd, CancellationToken ct)
        {
            var msg = MessageMapper.FromSendDto(cmd.Dto, cmd.SenderName);
            await _uow.Messages.AddAsync(msg);

            var preview = msg.Content.Length > 60
                ? msg.Content[..60] + "…" : msg.Content;
            await _uow.Rooms.UpdateLastMessageAsync(msg.RoomId, preview);

            await _hub.Clients.Group(msg.RoomId).SendAsync("ReceiveMessage", new
            {
                msg.Id, msg.RoomId, msg.SenderId, msg.SenderName,
                msg.Content, Type = msg.Type.ToString(),
                msg.Tags, msg.Timestamp
            }, ct);

            return msg;
        }
    }

    // ── EditMessage ───────────────────────────────────────────
    public record EditMessageCommand(string Id, string SenderId, string Content)
        : IRequest<Message?>;

    public class EditMessageHandler : IRequestHandler<EditMessageCommand, Message?>
    {
        private readonly IUnitOfWork          _uow;
        private readonly IHubContext<ChatHub> _hub;

        public EditMessageHandler(IUnitOfWork uow, IHubContext<ChatHub> hub)
        {
            _uow = uow;
            _hub = hub;
        }

        public async Task<Message?> Handle(EditMessageCommand cmd, CancellationToken ct)
        {
            var updated = await _uow.Messages.EditAsync(cmd.Id, cmd.SenderId, cmd.Content);
            if (updated is null) return null;
            await _hub.Clients.Group(updated.RoomId)
                .SendAsync("MessageEdited", new { cmd.Id, content = cmd.Content }, ct);
            return updated;
        }
    }

    // ── DeleteMessage ─────────────────────────────────────────
    public record DeleteMessageCommand(string Id, string RoomId) : IRequest<bool>;

    public class DeleteMessageHandler : IRequestHandler<DeleteMessageCommand, bool>
    {
        private readonly IUnitOfWork          _uow;
        private readonly IHubContext<ChatHub> _hub;

        public DeleteMessageHandler(IUnitOfWork uow, IHubContext<ChatHub> hub)
        {
            _uow = uow;
            _hub = hub;
        }

        public async Task<bool> Handle(DeleteMessageCommand cmd, CancellationToken ct)
        {
            var ok = await _uow.Messages.DeleteAsync(cmd.Id);
            if (!ok) return false;
            await _hub.Clients.Group(cmd.RoomId)
                .SendAsync("MessageDeleted", new { cmd.Id, cmd.RoomId }, ct);
            return true;
        }
    }

    // ── MarkRead ──────────────────────────────────────────────
    public record MarkReadCommand(string MessageId, string UserId) : IRequest;

    public class MarkReadHandler : IRequestHandler<MarkReadCommand>
    {
        private readonly IUnitOfWork _uow;
        public MarkReadHandler(IUnitOfWork uow) => _uow = uow;
        public Task Handle(MarkReadCommand cmd, CancellationToken ct)
            => _uow.Messages.MarkReadAsync(cmd.MessageId, cmd.UserId);
    }

    // ── AddReaction ───────────────────────────────────────────
    public record AddReactionCommand(string MessageId, string Emoji, string UserId)
        : IRequest;

    public class AddReactionHandler : IRequestHandler<AddReactionCommand>
    {
        private readonly IUnitOfWork _uow;
        public AddReactionHandler(IUnitOfWork uow) => _uow = uow;
        public Task Handle(AddReactionCommand cmd, CancellationToken ct)
            => _uow.Messages.AddReactionAsync(cmd.MessageId, cmd.Emoji, cmd.UserId);
    }

    // ── RemoveReaction ────────────────────────────────────────
    public record RemoveReactionCommand(string MessageId, string Emoji, string UserId)
        : IRequest;

    public class RemoveReactionHandler : IRequestHandler<RemoveReactionCommand>
    {
        private readonly IUnitOfWork _uow;
        public RemoveReactionHandler(IUnitOfWork uow) => _uow = uow;
        public Task Handle(RemoveReactionCommand cmd, CancellationToken ct)
            => _uow.Messages.RemoveReactionAsync(cmd.MessageId, cmd.Emoji, cmd.UserId);
    }
}
