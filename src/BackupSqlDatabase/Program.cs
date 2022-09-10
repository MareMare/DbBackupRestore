// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDatabaseToolkit;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(ConfigureServices)
    .Build();
var toolkit = host.Services.GetRequiredService<ISqlDatabaseToolkit>();
try
{
    await toolkit.BackupAsync().ConfigureAwait(false);
    return 0;
}
catch
{
    return -1;
}

static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    var configuration = hostContext.Configuration;
    services.AddSqlDatabaseToolkit(configuration);
}
