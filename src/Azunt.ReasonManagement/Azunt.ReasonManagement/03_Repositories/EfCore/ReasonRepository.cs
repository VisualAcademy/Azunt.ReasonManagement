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
            return string.IsNullOrWhiteSpace(connectionString)
                ? _factory.CreateDbContext()
                : _factory.CreateDbContext(connectionString);
        }

        public async Task<Reason> AddAsync(Reason model, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            model.Active ??= true;
            model.CreatedAt = model.CreatedAt == default
                ? DateTimeOffset.UtcNow
                : model.CreatedAt;

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

            var entity = await context.Reasons
                .FirstOrDefaultAsync(m => m.Id == model.Id);

            if (entity == null)
            {
                return false;
            }

            entity.Active = model.Active;
            entity.Name = model.Name;
            entity.Content = model.Content;
            entity.CreatedAt = model.CreatedAt == default ? entity.CreatedAt : model.CreatedAt;
            entity.CreatedBy = model.CreatedBy;

            context.Reasons.Update(entity);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(long id, string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var entity = await context.Reasons
                .FirstOrDefaultAsync(m => m.Id == id);

            if (entity == null)
            {
                return false;
            }

            context.Reasons.Remove(entity);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<ArticleSet<Reason, int>> GetArticlesAsync<TParentIdentifier>(
            int pageIndex,
            int pageSize,
            string searchField,
            string searchQuery,
            string sortOrder,
            TParentIdentifier parentIdentifier,
            string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var query = context.Reasons.AsQueryable();

            query = ApplySearch(query, searchField, searchQuery);
            query = ApplySort(query, sortOrder);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ArticleSet<Reason, int>(items, totalCount);
        }

        public async Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(
            FilterOptions<TParentIdentifier> options,
            string? connectionString = null)
        {
            await using var context = CreateContext(connectionString);

            var query = context.Reasons.AsQueryable();

            query = ApplySearch(query, options.SearchField, options.SearchQuery);
            query = ApplySort(query, options.SortOrder);

            var totalCount = await query.LongCountAsync();
            var items = await query
                .Skip(options.PageIndex * options.PageSize)
                .Take(options.PageSize)
                .ToListAsync();

            return new ArticleSet<Reason, long>(items, totalCount);
        }

        private static IQueryable<Reason> ApplySearch(
            IQueryable<Reason> query,
            string? searchField,
            string? searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return query;
            }

            var keyword = searchQuery.Trim();
            var field = searchField?.Trim().ToLowerInvariant();

            return field switch
            {
                "name" => query.Where(m => m.Name != null && m.Name.Contains(keyword)),
                "content" => query.Where(m => m.Content != null && m.Content.Contains(keyword)),
                _ => query.Where(m =>
                    (m.Name != null && m.Name.Contains(keyword)) ||
                    (m.Content != null && m.Content.Contains(keyword)))
            };
        }

        private static IQueryable<Reason> ApplySort(IQueryable<Reason> query, string? sortOrder)
        {
            return sortOrder switch
            {
                "Name" => query.OrderBy(m => m.Name),
                "NameDesc" => query.OrderByDescending(m => m.Name),
                "Content" => query.OrderBy(m => m.Content),
                "ContentDesc" => query.OrderByDescending(m => m.Content),
                "CreatedAt" => query.OrderBy(m => m.CreatedAt),
                "CreatedAtDesc" => query.OrderByDescending(m => m.CreatedAt),
                "Active" => query.OrderBy(m => m.Active),
                "ActiveDesc" => query.OrderByDescending(m => m.Active),
                _ => query.OrderByDescending(m => m.Id)
            };
        }
    }
}
