using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Dul.Articles;

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
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Reasons (Active, CreatedAt, CreatedBy, Name)
                            OUTPUT INSERTED.Id
                            VALUES (@Active, @CreatedAt, @CreatedBy, @Name)";

        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);

        await conn.OpenAsync();
        model.Id = (long)await cmd.ExecuteScalarAsync();
        return model;
    }

    public async Task<List<Reason>> GetAllAsync(string? connectionString = null)
    {
        var result = new List<Reason>();

        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Reasons ORDER BY Id DESC";

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Reason
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                CreatedAt = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return result;
    }

    public async Task<Reason> GetByIdAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Reasons WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Reason
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                CreatedAt = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }

        return new Reason(); // 빈 모델 반환
    }

    public async Task<bool> UpdateAsync(Reason model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Reasons SET
                                Active = @Active,
                                Name = @Name
                            WHERE Id = @Id";

        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Reasons WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<ArticleSet<Reason, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null)
    {
        // 심플 버전
        var result = await GetAllAsync(connectionString);
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? result
            : result.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Reason, int>(paged, filtered.Count);
    }

    public async Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options, string? connectionString = null)
    {
        var result = await GetAllAsync(connectionString);
        var filtered = result
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
