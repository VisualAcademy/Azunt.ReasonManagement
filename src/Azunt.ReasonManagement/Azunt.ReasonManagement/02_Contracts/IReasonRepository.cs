using Dul.Articles;

namespace Azunt.ReasonManagement;

public interface IReasonRepository
{
    Task<Reason> AddAsync(Reason model, string? connectionString = null);
    Task<List<Reason>> GetAllAsync(string? connectionString = null);
    Task<Reason> GetByIdAsync(long id, string? connectionString = null);
    Task<bool> UpdateAsync(Reason model, string? connectionString = null);
    Task<bool> DeleteAsync(long id, string? connectionString = null);
    Task<ArticleSet<Reason, int>> GetArticlesAsync<TParentIdentifier>(int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string? connectionString = null);
    Task<ArticleSet<Reason, long>> GetByAsync<TParentIdentifier>(FilterOptions<TParentIdentifier> options, string? connectionString = null);
}
