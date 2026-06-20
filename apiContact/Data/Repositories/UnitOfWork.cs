namespace apiContact.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        public IUserRepository    Users    { get; }
        public IRoomRepository    Rooms    { get; }
        public IMessageRepository Messages { get; }

        public UnitOfWork(
            IUserRepository    users,
            IRoomRepository    rooms,
            IMessageRepository messages)
        {
            Users    = users;
            Rooms    = rooms;
            Messages = messages;
        }
    }
}
