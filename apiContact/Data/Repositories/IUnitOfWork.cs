namespace apiContact.Data.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository    Users    { get; }
        IRoomRepository    Rooms    { get; }
        IMessageRepository Messages { get; }
    }
}
