using System.ComponentModel.DataAnnotations;
using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    public class CreateRoomDto
    {
        [Required(ErrorMessage = "Room name is required")]
        [MinLength(1,   ErrorMessage = "Room name must be at least 1 character")]
        [MaxLength(100, ErrorMessage = "Room name must be at most 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Category must be at most 50 characters")]
        public string Category { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public RoomType Type { get; set; } = RoomType.Group;

        public bool IsPrivate { get; set; } = false;

        public List<string> MemberIds { get; set; } = new();
    }

    public class UpdateRoomDto
    {
        [MinLength(1,   ErrorMessage = "Room name must be at least 1 character")]
        [MaxLength(100, ErrorMessage = "Room name must be at most 100 characters")]
        public string? Name { get; set; }

        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters")]
        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "Category must be at most 50 characters")]
        public string? Category { get; set; }

        public List<string>? Tags { get; set; }

        public bool? IsArchived { get; set; }

        public bool? IsPrivate  { get; set; }
    }

    public class AddMemberDto
    {
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = string.Empty;
    }
}
