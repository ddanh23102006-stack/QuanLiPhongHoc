using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;

namespace QuanLiPhongHoc.Web.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly QuanLiPhongHocContext Db;
    protected readonly DbSet<T> Set;

    public Repository(QuanLiPhongHocContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task<T?> GetByIdAsync(object id) => await Set.FindAsync(id);

    public async Task AddAsync(T entity) => await Set.AddAsync(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync() => Db.SaveChangesAsync();

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Set.AnyAsync(predicate);
}
