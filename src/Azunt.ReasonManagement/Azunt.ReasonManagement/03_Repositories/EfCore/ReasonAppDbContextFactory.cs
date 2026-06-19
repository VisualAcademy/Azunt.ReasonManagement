using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.ReasonManagement;

/// <summary>
/// ReasonAppDbContext 인스턴스를 생성하는 Factory 클래스입니다.
/// 기본값은 EF Core In-Memory이며, 연결 문자열이 제공되면 SQL Server를 사용합니다.
/// 테스트용 멀티테넌트 시나리오에서는 "InMemory:DatabaseName" 형식의 문자열로
/// 테넌트별 In-Memory 데이터베이스를 분리할 수 있습니다.
/// </summary>
public class ReasonAppDbContextFactory
{
    public const string InMemoryConnectionPrefix = "InMemory:";

    private readonly IConfiguration? _configuration;
    private readonly string? _defaultConnectionString;

    /// <summary>
    /// 기본 생성자입니다. 연결 문자열 없이 사용하면 In-Memory DbContext를 생성합니다.
    /// </summary>
    public ReasonAppDbContextFactory()
    {
    }

    /// <summary>
    /// SQL Server 연결 문자열 또는 "InMemory:DatabaseName" 테스트 연결 문자열을 직접 전달받는 생성자입니다.
    /// </summary>
    public ReasonAppDbContextFactory(string defaultConnectionString)
    {
        _defaultConnectionString = defaultConnectionString;
    }

    /// <summary>
    /// IConfiguration을 주입받는 생성자입니다.
    /// DefaultConnection이 있으면 해당 연결 문자열을 사용하고,
    /// 없으면 In-Memory를 사용합니다.
    /// </summary>
    public ReasonAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 연결 문자열을 사용하여 DbContext 인스턴스를 생성합니다.
    /// 빈 문자열이면 기본 In-Memory, "InMemory:DatabaseName"이면 지정된 In-Memory,
    /// 그 외의 문자열이면 SQL Server 연결 문자열로 처리합니다.
    /// </summary>
    public ReasonAppDbContext CreateDbContext(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return CreateInMemoryDbContext();
        }

        if (IsInMemoryConnectionString(connectionString))
        {
            var databaseName = GetInMemoryDatabaseName(connectionString);
            return CreateInMemoryDbContext(databaseName);
        }

        return CreateSqlServerDbContext(connectionString);
    }

    /// <summary>
    /// DbContextOptions를 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ReasonAppDbContext CreateDbContext(DbContextOptions<ReasonAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ReasonAppDbContext(options);
    }

    /// <summary>
    /// 기본 DbContext 인스턴스를 생성합니다.
    /// 생성자 또는 appsettings.json에 DefaultConnection이 있으면 해당 연결 문자열을 사용하고,
    /// 없으면 In-Memory를 사용합니다.
    /// </summary>
    public ReasonAppDbContext CreateDbContext()
    {
        if (!string.IsNullOrWhiteSpace(_defaultConnectionString))
        {
            return CreateDbContext(_defaultConnectionString);
        }

        var configuredConnection = _configuration?.GetConnectionString("DefaultConnection");

        return CreateDbContext(configuredConnection);
    }

    /// <summary>
    /// EF Core In-Memory DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ReasonAppDbContext CreateInMemoryDbContext(string databaseName = ReasonInMemoryDatabase.DefaultName)
    {
        var options = new DbContextOptionsBuilder<ReasonAppDbContext>()
            .UseInMemoryDatabase(databaseName, ReasonInMemoryDatabase.Root)
            .Options;

        return new ReasonAppDbContext(options);
    }

    /// <summary>
    /// SQL Server DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ReasonAppDbContext CreateSqlServerDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
        }

        var options = new DbContextOptionsBuilder<ReasonAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ReasonAppDbContext(options);
    }

    public static bool IsInMemoryConnectionString(string connectionString)
    {
        return connectionString.StartsWith(InMemoryConnectionPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetInMemoryDatabaseName(string connectionString)
    {
        var databaseName = connectionString[InMemoryConnectionPrefix.Length..].Trim();

        return string.IsNullOrWhiteSpace(databaseName)
            ? ReasonInMemoryDatabase.DefaultName
            : databaseName;
    }
}
