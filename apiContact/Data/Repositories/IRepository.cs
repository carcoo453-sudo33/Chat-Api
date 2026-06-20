using System.Linq.Expressions;

namespace apiContact.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?>             GetByIdAsync(string id);
        Task<List<T>>        GetAllAsync();
        Task<List<T>>        FindAsync(Expression<Func<T, bool>> predicate);
        Task<T>              AddAsync(T entity);
        Task<T?>             UpdateAsync(T entity);
        Task<bool>           DeleteAsync(string id);
        Task<int>            CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<bool>           ExistsAsync(Expression<Func<T, bool>> predicate);
    }
}
