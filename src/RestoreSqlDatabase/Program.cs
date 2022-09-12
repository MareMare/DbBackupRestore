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
    .ConfigureServices((hostContext, services) => services.AddSqlDatabaseToolkit(hostContext.Configuration))
    .Build();
var toolkit = host.Services.GetRequiredService<ISqlDatabaseToolkit>();
await toolkit.RestoreAsync().ConfigureAwait(false);
