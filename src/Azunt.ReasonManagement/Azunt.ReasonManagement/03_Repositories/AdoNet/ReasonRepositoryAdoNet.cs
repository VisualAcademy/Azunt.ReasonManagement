using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.ReasonManagement;

public class ReasonRepositoryAdoNet : IReasonRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<ReasonRepositoryAdoNet> _logger;

    public ReasonRepositoryAdoNet(string defaultConnectionString, ILoggerFactory loggerFactory)
    {
        _defaultConnectionString = defaultConnectionString;
        _logger = loggerFactory.CreateLogger<ReasonRepositoryAdoNet>();
    }

    private SqlConnection GetConnection(string? connectionString)
    {
        return new SqlConnection(connectionString ?? _defaultConnectionString);
    }

    public async Task<Reason> AddAsync(Reason model, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"INSERT INTO Reasons (Active, CreatedAt, CreatedBy, Name, Content)
                            OUTPUT INSERTED.Id
                            VALUES (@Active, @CreatedAt, @CreatedBy, @Name, @Content)";

        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", model.Content ?? (object)DBNull.Value);

        await conn.OpenAsync();
        model.Id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return model;
    }

    public async Task<List<Reason>> GetAllAsync(string? connectionString = null)
    {
        var result = new List<Reason>();

        await using var conn = GetConnection(connectionString);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name, Content FROM Reasons ORDER BY Id DESC";

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(ReadReason(reader));
        }

        return result;
    }

    public async Task<Reason> GetByIdAsync(long id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name, Content FROM Reasons WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? ReadReason(reader)
            : new Reason();
    }

    public async Task<bool> UpdateAsync(Reason model, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"UPDATE Reasons SET
                                Active = @Active,
                                Name = @Name,
                                Content = @Content
                            WHERE Id = @Id";

        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", model.Content ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM Reasons WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
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
        var result = await GetAllAsync(connectionString);
        var filtered = ApplySearch(result, searchField, searchQuery);
        var sorted = ApplySort(filtered, sortOrder);

        var paged = sorted
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Reason, int>(paged, filtered.Count);
    }

    public async Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options,
        string? connectionString = null)
    {
        var result = await GetAllAsync(connectionString);
        var filtered = ApplySearch(result, options.SearchField, options.SearchQuery);
        var sorted = ApplySort(filtered, options.SortOrder);

        var paged = sorted
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return new ArticleSet<Reason, long>(paged, filtered.Count);
    }

    private static Reason ReadReason(SqlDataReader reader)
    {
        return new Reason
        {
            Id = reader.GetInt64(0),
            Active = reader.IsDBNull(1) ? null : reader.GetBoolean(1),
            CreatedAt = reader.IsDBNull(2) ? default : reader.GetDateTimeOffset(2),
            CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
            Name = reader.IsDBNull(4) ? null : reader.GetString(4),
            Content = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    private static List<Reason> ApplySearch(IEnumerable<Reason> source, string? searchField, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return source.ToList();
        }

        var keyword = searchQuery.Trim();
        var field = searchField?.Trim().ToLowerInvariant();

        return field switch
        {
            "name" => source.Where(m => m.Name != null && m.Name.Contains(keyword)).ToList(),
            "content" => source.Where(m => m.Content != null && m.Content.Contains(keyword)).ToList(),
            _ => source.Where(m =>
                (m.Name != null && m.Name.Contains(keyword)) ||
                (m.Content != null && m.Content.Contains(keyword))).ToList()
        };
    }

    private static IEnumerable<Reason> ApplySort(IEnumerable<Reason> source, string? sortOrder)
    {
        return sortOrder switch
        {
            "Name" => source.OrderBy(m => m.Name),
            "NameDesc" => source.OrderByDescending(m => m.Name),
            "Content" => source.OrderBy(m => m.Content),
            "ContentDesc" => source.OrderByDescending(m => m.Content),
            "CreatedAt" => source.OrderBy(m => m.CreatedAt),
            "CreatedAtDesc" => source.OrderByDescending(m => m.CreatedAt),
            "Active" => source.OrderBy(m => m.Active),
            "ActiveDesc" => source.OrderByDescending(m => m.Active),
            _ => source.OrderByDescending(m => m.Id)
        };
    }
}
