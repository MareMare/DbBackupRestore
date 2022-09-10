// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlDatabaseOptions.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace SqlDatabaseToolkit;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class SqlDatabaseOptions
{
    public static string Key => nameof(SqlDatabaseOptions);

    public string ConnectionString { get; set; } = null!;

    public string BackupDirectory { get; set; } = null!;

    public string RestoreDirectory { get; set; } = null!;

    public Database[] Databases { get; set; } = null!;

    private string DebuggerDisplay => $"Databases={this.Databases.Length} {this.ConnectionString}";
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Database
{
    public string Name { get; set; } = null!;

    private string DebuggerDisplay => this.Name;
}
