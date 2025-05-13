using Azunt.ReasonManagement;
using Dul.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azunt.ReasonManagement
{
    public class ReasonRepository : IReasonRepository
    {
        private readonly ReasonAppDbContextFactory _factory;
        private readonly ILogger<ReasonRepository> _logger;

        public ReasonRepository(
            ReasonAppDbContextFactory factory,
            ILoggerFactory loggerFactory)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger<ReasonRepository>();
        }

        private ReasonAppDbContext CreateContext(string? connectionString)
        {
            return string.IsNullOrEmpty(connectionString)
                ? _factory.CreateDbContext()
                : _factory.CreateDbContext(connectionString);
        }

        public async Task<Reason> AddAsync(Reason model, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            model.CreatedAt = DateTime.UtcNow;
            context.Reasons.Add(model);
            await context.SaveChangesAsync();
            return model;
        }

        public async Task<List<Reason>> GetAllAsync(string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            return await context.Reasons
                .OrderByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<Reason> GetByIdAsync(long id, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            return await context.Reasons
                       .SingleOrDefaultAsync(m => m.Id == id)
                   ?? new Reason();
        }

        public async Task<bool> UpdateAsync(Reason model, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            context.Attach(model);
            context.Entry(model).State = EntityState.Modified;
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(long id, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);
            var entity = await context.Reasons.FindAsync(id);
            if (entity == null) return false;
            context.Reasons.Remove(entity);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<ArticleSet<Reason, int>> GetArticlesAsync<TParentIdentifier>(
            int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var query = context.Reasons.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(m => m.Name!.Contains(searchQuery));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.Id)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ArticleSet<Reason, int>(items, totalCount);
        }

        public async Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(
            FilterOptions<TParentIdentifier> options, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var query = context.Reasons.AsQueryable();

            if (!string.IsNullOrEmpty(options.SearchQuery))
            {
                query = query.Where(m => m.Name!.Contains(options.SearchQuery));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.Id)
                .Skip(options.PageIndex * options.PageSize)
                .Take(options.PageSize)
                .ToListAsync();

            return new ArticleSet<Reason, long>(items, totalCount);
        }
    }
}
