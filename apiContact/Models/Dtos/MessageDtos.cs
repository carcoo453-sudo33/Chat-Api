using System.ComponentModel.DataAnnotations;
using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    public class SendMessageDto
    {
        [Required(ErrorMessage = "RoomId is required")]
        public string RoomId { get; set; } = string.Empty;

        public string SenderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message content is required")]
        [MinLength(1,    ErrorMessage = "Message content cannot be empty")]
        [MaxLength(4000, ErrorMessage = "Message content must be at most 4000 characters")]
        public string Content { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.Text;

        public List<string> Tags { get; set; } = new();
    }

    public class EditMessageDto
    {
        [Required(ErrorMessage = "Content is required")]
        [MinLength(1,    ErrorMessage = "Message content cannot be empty")]
        [MaxLength(4000, ErrorMessage = "Message content must be at most 4000 characters")]
        public string Content { get; set; } = string.Empty;
    }

    public class MarkReadDto
    {
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = string.Empty;
    }

    public class AddReactionDto
    {
        [Required(ErrorMessage = "Emoji is required")]
        [MaxLength(10, ErrorMessage = "Emoji must be at most 10 characters")]
        public string Emoji { get; set; } = string.Empty;
    }
}
