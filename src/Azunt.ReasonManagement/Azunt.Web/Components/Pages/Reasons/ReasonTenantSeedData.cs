using Azunt.ReasonManagement;
using Azunt.Web.Services;

namespace Azunt.Web.Components.Pages.Reasons;

public static class ReasonTenantSeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IReasonRepository>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var user = userService.GetUserNotCached();
        var connectionString = user.Tenant.ConnectionString;

        var existing = await repository.GetAllAsync(connectionString);
        if (existing.Count > 0)
        {
            return;
        }

        await repository.AddAsync(new Reason
        {
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.UserName,
            Name = "Tenant Reason 1",
            Content = $"This sample reason is stored in the tenant database for {user.Tenant.Name}."
        }, connectionString);

        await repository.AddAsync(new Reason
        {
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.UserName,
            Name = "Tenant Reason 2",
            Content = "This data is loaded through IUserService.GetUserNotCached().Tenant.ConnectionString."
        }, connectionString);
    }
}
