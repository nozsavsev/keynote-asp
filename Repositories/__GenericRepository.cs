using Microsoft.EntityFrameworkCore;
using keynote_asp.DbContexts;
using keynote_asp.Exceptions;

namespace keynote_asp.Repositories
{

    public delegate IQueryable<T> QueryCallback<T>(IQueryable<T> query);

    public class GenericRepository<T> where T : class
    {
        protected readonly KeynoteDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(KeynoteDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        protected virtual IQueryable<T> BuildQuery(QueryCallback<T>? queryCallback = null, bool loadDependencies = true, bool tracking = false)
        {
            IQueryable<T> baseQuery = _dbSet;
            if (!tracking)
            {
                baseQuery = baseQuery.AsNoTracking();
            }

            var query = queryCallback?.Invoke(baseQuery) ?? baseQuery;

            if (loadDependencies == false)
                query = query.IgnoreAutoIncludes();

            return query;
        }

        protected virtual string GetKeyProperty()
        {
            var keyName = _context.Model.FindEntityType(typeof(T))?
                 .FindPrimaryKey()?
                 .Properties.Select(x => x.Name)
                 .FirstOrDefault();
            if (keyName == null)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} does not have a primary key defined.");
            }

            return keyName;
        }



        public virtual async Task<T?> GetByIdAsync(long id, QueryCallback<T>? queryCallback = null, bool tracking = false, bool loadDependencies = true)
        {
            var query = BuildQuery(queryCallback, loadDependencies, tracking);

            var keyName = GetKeyProperty();

            return await query.Where(e => EF.Property<long>(e, keyName) == id)
                              .FirstOrDefaultAsync();
        }

        public virtual async Task<T> AddAsync(T entity, QueryCallback<T>? queryCallback = null)
        {
            try
            {

                var newEntity = await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();

                // Re-query to load navigation properties according to policy
                var keyName = GetKeyProperty();
                var keyValue = (long)(typeof(T).GetProperty(keyName)!.GetValue(newEntity.Entity)!
                                     ?? throw new KeynoteException(WrResponseStatus.InternalError));

                var reloaded = await GetByIdAsync(keyValue, queryCallback);
                if (reloaded == null)
                {
                    throw new KeynoteException(WrResponseStatus.InternalError);
                }
                return reloaded;
            }
            catch (KeynoteException ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            catch (Exception ex)
            {
                throw new KeynoteException(WrResponseStatus.BadRequest, ex.Message);
            }
        }

        public virtual async Task<List<T>> AddManyAsync(List<T> entities, QueryCallback<T>? queryCallback = null)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            var reloaded = await DynamicQueryManyAsync(queryCallback) ?? new List<T>();
            return reloaded;
        }

        public virtual async Task<long> CountAllAsync()
        {
            return await _dbSet.LongCountAsync();
        }


        public virtual async Task<T?> DynamicQuerySingleAsync(QueryCallback<T>? queryCallback = null, bool loadDependencies = true, bool tracking = false)
        {
            var query = BuildQuery(queryCallback, loadDependencies, tracking);
            return await query.FirstOrDefaultAsync();
        }

        public virtual async Task<List<T>> DynamicQueryManyAsync(QueryCallback<T>? queryCallback = null, bool loadDependencies = true, bool tracking = false)
        {
            var query = BuildQuery(queryCallback, loadDependencies, tracking);
            return await query.ToListAsync();
        }


        public virtual async Task<T> UpdateAsync(T entity, QueryCallback<T>? queryCallback = null)
        {
            var keyName = GetKeyProperty();
            var keyValueProperty = typeof(T).GetProperty(keyName);

            if (keyValueProperty == null)
            {
                throw new InvalidOperationException($"Could not find key property '{keyName}' on entity type '{typeof(T).Name}'.");
            }

            var keyValue = keyValueProperty.GetValue(entity);
            if (keyValue == null)
            {
                throw new InvalidOperationException($"Key value for '{keyName}' on entity type '{typeof(T).Name}' cannot be null.");
            }

            var existingEntity = await _dbSet.FindAsync(keyValue);

            if (existingEntity != null)
            {
                _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                await _context.SaveChangesAsync();
                return existingEntity;
            }
            else
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
        }

        public virtual async Task<List<T>> UpdateManyAsync(List<T> entities, QueryCallback<T>? queryCallback = null)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync();

            var reloaded = await DynamicQueryManyAsync(q => q.Where(e => entities.Contains(e))) ?? new();
            return reloaded;
        }


        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteByIdAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} with id {id} not found.");
            }
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteManyAsync(List<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteManyAsync(List<long> ids)
        {
            var keyName = GetKeyProperty();
            var entities = await _dbSet.Where(e => ids.Contains(EF.Property<long>(e, keyName))).ToListAsync();
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public virtual async Task<bool> ExistsAsync(long id)
        {
            var keyName = GetKeyProperty();
            return await _dbSet.AnyAsync(e => EF.Property<long>(e, keyName) == id);
        }
    }
}