using apiContact.Models.Entities;
using apiContact.Models.Enums;

namespace apiContact.Data.Seeds
{
    public static class MessageSeed
    {
        public static IReadOnlyList<Message> Generate(
            IReadOnlyList<ChatUser> users,
            IReadOnlyList<ChatRoom> rooms)
        {
            var u1 = users[0]; // alice
            var u2 = users[1]; // bob
            var u3 = users[2]; // carla
            var r1 = rooms[0]; // General
            var r2 = rooms[1]; // Engineering
            var r3 = rooms[2]; // DM

            var now = DateTime.UtcNow;

            return new List<Message>
            {
                // ── General ──────────────────────────────────────────
                new()
                {
                    Id = "msg_001", RoomId = r1.Id,
                    SenderId = u1.Id, SenderName = u1.DisplayName,
                    Content = "Welcome to Chat API! 👋", Type = MessageType.Text,
                    Tags = new() { "welcome" }, Timestamp = now.AddMinutes(-30)
                },
                new()
                {
                    Id = "msg_002", RoomId = r1.Id,
                    SenderId = u2.Id, SenderName = u2.DisplayName,
                    Content = "Hey everyone!", Type = MessageType.Text,
                    Tags = new(), Timestamp = now.AddMinutes(-28)
                },
                new()
                {
                    Id = "msg_003", RoomId = r1.Id,
                    SenderId = u3.Id, SenderName = u3.DisplayName,
                    Content = "This API is looking great. 🚀", Type = MessageType.Text,
                    Tags = new() { "feedback" }, Timestamp = now.AddMinutes(-25)
                },

                // ── Engineering ───────────────────────────────────────
                new()
                {
                    Id = "msg_004", RoomId = r2.Id,
                    SenderId = u1.Id, SenderName = u1.DisplayName,
                    Content = "SignalR integration is live.", Type = MessageType.Text,
                    Tags = new() { "signalr", "release" }, Timestamp = now.AddMinutes(-15)
                },
                new()
                {
                    Id = "msg_005", RoomId = r2.Id,
                    SenderId = u3.Id, SenderName = u3.DisplayName,
                    Content = "MongoDB in-memory fallback works perfectly.", Type = MessageType.Text,
                    Tags = new() { "mongodb", "testing" }, Timestamp = now.AddMinutes(-12)
                },

                // ── DM ────────────────────────────────────────────────
                new()
                {
                    Id = "msg_006", RoomId = r3.Id,
                    SenderId = u1.Id, SenderName = u1.DisplayName,
                    Content = "Hey Bob, can you review the PR?", Type = MessageType.Text,
                    Tags = new() { "pr", "review" }, Timestamp = now.AddMinutes(-5)
                },
                new()
                {
                    Id = "msg_007", RoomId = r3.Id,
                    SenderId = u2.Id, SenderName = u2.DisplayName,
                    Content = "Sure, on it now!", Type = MessageType.Text,
                    Tags = new(), Timestamp = now.AddMinutes(-3)
                }
            };
        }
    }
}
