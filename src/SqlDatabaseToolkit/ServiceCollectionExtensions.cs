using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SqlDatabaseToolkit;

/// <summary>
/// <see cref="IServiceCollection" /> の拡張機能を提供します。
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDatabaseToolkit(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .Configure<SqlDatabaseOptions>(configuration.GetSection(SqlDatabaseOptions.Key))
            .AddTransient<ISqlDatabaseToolkit, SqlDatabaseToolkit>();
    }
}
