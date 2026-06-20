namespace apiContact.Models.Enums
{
    public enum RoomType
    {
        Direct  = 0,   // 1-to-1 conversation
        Group   = 1,   // Named group chat
        Channel = 2    // Broadcast channel (one-to-many)
    }
}
