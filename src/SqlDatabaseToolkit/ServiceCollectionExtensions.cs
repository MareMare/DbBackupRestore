﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SqlDatabaseToolkit;

/// <summary>
/// <see cref="IServiceCollection" /> の拡張機能を提供します。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// <see cref="ISqlDatabaseToolkit" /> に関するサービスの依存関係を追加します。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection" />。</param>
    /// <param name="configuration"><see cref="IConfiguration" />。</param>
    /// <returns>依存関係が追加された <see cref="IServiceCollection" />。</returns>
    public static IServiceCollection AddSqlDatabaseToolkit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .Configure<SqlDatabaseOptions>(configuration.GetSection(SqlDatabaseOptions.Key))
            .AddTransient<ISqlDatabaseToolkit, SqlDatabaseToolkit>();
    }
}
