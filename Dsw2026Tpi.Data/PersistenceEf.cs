using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

public class PersistenceEf: IPersistence
{
    private readonly Dsw2026TpiDbContext _context;

    public PersistenceEf(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<T> Add<T>(T entity) where T : EntityBase
    {
        var now = DateTime.Now;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;

        await _context.AddAsync(entity);
        ApplyUpdatedAt();
        await _context.SaveChangesAsync();
        return entity;
    }
    public async Task AddRange<T>(IEnumerable<T> entities, bool saveChanges = true) where T : EntityBase
    {
        var entityList = entities.ToList();
        var now = DateTime.Now;

        foreach (var entity in entityList)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }


        await _context.Set<T>().AddRangeAsync(entityList);
        if (saveChanges)
        {
            ApplyUpdatedAt();
            await _context.SaveChangesAsync();
        }
    }
    public async Task SaveChanges()
    {
        ApplyUpdatedAt();

        await _context.SaveChangesAsync();
    }

    public async Task<T> Delete<T>(T entity) where T : EntityBase
    {
        var a = entity.Id;
        _context.Remove(entity);
        ApplyUpdatedAt();
        await _context.SaveChangesAsync();
        return entity;
    }
    public async Task ReplaceRange<T>(IEnumerable<T> existingEntities, IEnumerable<T> newEntities) where T : EntityBase
    {
        var existingList = existingEntities.ToList();
        var newList = newEntities.ToList();
        var now = DateTime.Now;

        foreach (var entity in newList)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }

        _context.RemoveRange(existingList);
        await _context.Set<T>().AddRangeAsync(newList);
        ApplyUpdatedAt();
        await _context.SaveChangesAsync();
    }

    public async Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).ToListAsync();
    }

    public async Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).Where(predicate).ToListAsync();
    }

    public async Task<T> Update<T>(T entity) where T : EntityBase
    {
        entity.UpdatedAt = DateTime.Now;

        _context.Update(entity);
        ApplyUpdatedAt();
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Pagination<T>> Paginate<T, TKey>(int pageSize, int pageIndex, Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> sortOrder, params string[] includes) where T : EntityBase
    {
        if (pageSize <= 0 || pageSize > PaginationOptions.MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var filtered = Include(_context.Set<T>(), includes)
                 .Where(predicate)
                 .OrderBy(sortOrder)
                 .ThenBy(entity => entity.Id);

        var total = await filtered.CountAsync();

        var skip = (long)pageIndex * pageSize;
        if (skip >= total)
            return new Pagination<T>(pageSize, pageIndex, total, []);

        var data = await filtered.Skip((int)skip)
            .Take(pageSize)
            .ToListAsync();

        return new Pagination<T>(pageSize, pageIndex, total, data);
    }

    private static IQueryable<T> Include<T>(IQueryable<T> query, string[] includes) where T : EntityBase
    {
        var includedQuery = query;

        foreach (var include in includes)
        {
            includedQuery = includedQuery.Include(include);
        }
        return includedQuery;
    }
    private void ApplyUpdatedAt()
    {
        var now = DateTime.Now;

        foreach (var entry in _context.ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
