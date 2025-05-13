using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dul.Articles;

namespace Azunt.ReasonManagement;

public class ReasonRepositoryDapper : IReasonRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<ReasonRepositoryDapper> _logger;

    public ReasonRepositoryDapper(string defaultConnectionString, ILoggerFactory loggerFactory)
    {
        _defaultConnectionString = defaultConnectionString;
        _logger = loggerFactory.CreateLogger<ReasonRepositoryDapper>();
    }

    private SqlConnection GetConnection(string? connectionString)
    {
        return new SqlConnection(connectionString ?? _defaultConnectionString);
    }

    public async Task<Reason> AddAsync(Reason model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = @"INSERT INTO Reasons (Active, CreatedAt, CreatedBy, Name)
                    OUTPUT INSERTED.Id
                    VALUES (@Active, @CreatedAt, @CreatedBy, @Name)";

        model.CreatedAt = DateTimeOffset.UtcNow;
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<List<Reason>> GetAllAsync(string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Reasons ORDER BY Id DESC";
        var list = await conn.QueryAsync<Reason>(sql);
        return list.ToList();
    }

    public async Task<Reason> GetByIdAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Reasons WHERE Id = @Id";
        var model = await conn.QuerySingleOrDefaultAsync<Reason>(sql, new { Id = id });
        return model ?? new Reason();
    }

    public async Task<bool> UpdateAsync(Reason model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = @"UPDATE Reasons SET
                        Active = @Active,
                        Name = @Name
                    WHERE Id = @Id";

        var rows = await conn.ExecuteAsync(sql, model);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var sql = "DELETE FROM Reasons WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<ArticleSet<Reason, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null)
    {
        var all = await GetAllAsync(connectionString);
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Reason, int>(paged, filtered.Count);
    }

    public async Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options, string? connectionString = null)
    {
        var all = await GetAllAsync(connectionString);
        var filtered = all
            .Where(m => string.IsNullOrWhiteSpace(options.SearchQuery) ||
                        (m.Name != null && m.Name.Contains(options.SearchQuery)))
            .ToList();

        var paged = filtered
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return new ArticleSet<Reason, long>(paged, filtered.Count);
    }
}
