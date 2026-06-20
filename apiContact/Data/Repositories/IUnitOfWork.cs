namespace apiContact.Data.Repositories
{
    /// <summary>
    /// Unit of Work — groups all repositories under one coordinated scope.
    /// MongoDB does not require an explicit SaveChanges() call; each repository
    /// method writes directly. For multi-document atomicity in production, wire
    /// in a MongoDB client session and pass it through the repositories.
    /// </summary>
    public interface IUnitOfWork
    {
        IUserRepository    Users    { get; }
        IRoomRepository    Rooms    { get; }
        IMessageRepository Messages { get; }
    }
}
