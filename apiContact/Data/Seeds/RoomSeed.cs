using apiContact.Models.Entities;
using apiContact.Models.Enums;
using apiContact.Utilities;

namespace apiContact.Data.Seeds
{
    public static class RoomSeed
    {
        public static IReadOnlyList<ChatRoom> Generate(IReadOnlyList<ChatUser> users)
        {
            var u1 = users[0];
            var u2 = users[1];
            var u3 = users[2];

            var rooms = new List<ChatRoom>
            {
                new()
                {
                    Id          = "room_001",
                    Name        = "General",
                    Description = "General discussion — open to all",
                    Category    = "community",
                    Tags        = new List<string> { "general", "announcements", "welcome" },
                    Type        = RoomType.Channel,
                    IsPrivate   = false,
                    MemberIds   = new List<string> { u1.Id, u2.Id, u3.Id },
                    CreatedBy   = u1.Id
                },
                new()
                {
                    Id          = "room_002",
                    Name        = "Engineering",
                    Description = "Engineering team chat",
                    Category    = "engineering",
                    Tags        = new List<string> { "dev", "backend", "api" },
                    Type        = RoomType.Group,
                    IsPrivate   = true,
                    MemberIds   = new List<string> { u1.Id, u3.Id },
                    CreatedBy   = u1.Id
                },
                new()
                {
                    Id          = "room_003",
                    Name        = "Alice & Bob",
                    Description = "Direct message",
                    Category    = "direct",
                    Tags        = new List<string>(),
                    Type        = RoomType.Direct,
                    IsPrivate   = true,
                    MemberIds   = new List<string> { u1.Id, u2.Id },
                    CreatedBy   = u1.Id
                }
            };

            // Generate slugs
            var existingSlugs = new List<string>();
            foreach (var room in rooms)
            {
                var slug = SlugHelper.Uniquify(SlugHelper.Generate(room.Name), existingSlugs);
                room.Slug = slug;
                existingSlugs.Add(slug);
            }

            return rooms;
        }
    }
}
