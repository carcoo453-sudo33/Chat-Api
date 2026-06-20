using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    public class CreateRoomDto
    {
        public string       Name        { get; set; } = string.Empty;
        public string       Description { get; set; } = string.Empty;
        public string       Category    { get; set; } = string.Empty;
        public List<string> Tags        { get; set; } = new();
        public RoomType     Type        { get; set; } = RoomType.Group;
        public bool         IsPrivate   { get; set; } = false;
        public List<string> MemberIds   { get; set; } = new();
        public string       CreatedBy   { get; set; } = string.Empty;
    }

    public class UpdateRoomDto
    {
        public string?      Name        { get; set; }
        public string?      Description { get; set; }
        public string?      Category    { get; set; }
        public List<string>? Tags       { get; set; }
        public bool?        IsArchived  { get; set; }
        public bool?        IsPrivate   { get; set; }
    }

    public class AddMemberDto
    {
        public string UserId { get; set; } = string.Empty;
    }
}
