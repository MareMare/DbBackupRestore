// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDatabaseToolkit;

var baseDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
Console.WriteLine($"Base Directory: {baseDirectory}");
Console.WriteLine($"Expected Config Path: {Path.Combine(baseDirectory, "appsettings.json")}");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) => services.AddSqlDatabaseToolkit(hostContext.Configuration))
    .Build();

var fileStore = host.Services.GetRequiredService<IBackupFileStore>();
fileStore.Download();

var toolkit = host.Services.GetRequiredService<ISqlDatabaseToolkit>();
await toolkit.RestoreAsync().ConfigureAwait(false);
