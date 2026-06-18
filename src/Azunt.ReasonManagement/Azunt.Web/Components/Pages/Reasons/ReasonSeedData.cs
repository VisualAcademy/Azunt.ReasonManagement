using Azunt.ReasonManagement;

namespace Azunt.Web.Components.Pages.Reasons;

public static class ReasonSeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReasonRepository>();

        var existing = await repository.GetAllAsync();
        if (existing.Count > 0)
        {
            return;
        }

        await repository.AddAsync(new Reason
        {
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "System",
            Name = "Initial Reason 1",
            Content = "This is the first sample reason for EF Core In-Memory testing."
        });

        await repository.AddAsync(new Reason
        {
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "System",
            Name = "Initial Reason 2",
            Content = "This is the second sample reason for quick Blazor Server CRUD testing."
        });
    }
}
