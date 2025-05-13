using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.ReasonManagement;

/// <summary>
/// ReasonApp 의존성 주입 확장 메서드
/// </summary>
public static class ReasonServicesRegistrationExtensions
{
    // 선택 가능한 저장소 모드 정의
    public enum RepositoryMode
    {
        EfCore,
        Dapper,
        AdoNet
    }

    /// <summary>
    /// ReasonApp 모듈의 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컨테이너</param>
    /// <param name="connectionString">연결 문자열</param>
    /// <param name="mode">레포지토리 사용 모드 (기본: EF Core)</param>
    /// <param name="dbContextLifetime">DbContext 수명 주기 (기본: Transient)</param>
    public static void AddDependencyInjectionContainerForReasonApp(
        this IServiceCollection services,
        string connectionString,
        RepositoryMode mode = RepositoryMode.EfCore,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Transient)
    {
        switch (mode)
        {
            case RepositoryMode.EfCore:
                // EF Core 방식 등록
                services.AddDbContext<ReasonAppDbContext>(
                    options => options.UseSqlServer(connectionString),
                    dbContextLifetime);

                services.AddTransient<IReasonRepository, ReasonRepository>();
                services.AddTransient<ReasonAppDbContextFactory>();
                break;

            case RepositoryMode.Dapper:
                // Dapper 방식 등록
                services.AddTransient<IReasonRepository>(provider =>
                    new ReasonRepositoryDapper(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            case RepositoryMode.AdoNet:
                // ADO.NET 방식 등록
                services.AddTransient<IReasonRepository>(provider =>
                    new ReasonRepositoryAdoNet(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid repository mode '{mode}'. Supported modes: EfCore, Dapper, AdoNet.");
        }
    }
}
