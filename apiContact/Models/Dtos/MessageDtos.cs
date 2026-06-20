using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    public class SendMessageDto
    {
        public string       RoomId   { get; set; } = string.Empty;
        public string       SenderId { get; set; } = string.Empty;
        public string       Content  { get; set; } = string.Empty;
        public MessageType  Type     { get; set; } = MessageType.Text;
        public List<string> Tags     { get; set; } = new();
    }

    public class EditMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class MarkReadDto
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class AddReactionDto
    {
        public string Emoji  { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
