namespace apiContact.Models.Enums
{
    public enum MessageType
    {
        Text     = 0,  // Plain text
        Image    = 1,  // Inline image
        File     = 2,  // Attached file
        System   = 3,  // System / bot notification
        Reaction = 4   // Emoji reaction
    }
}
