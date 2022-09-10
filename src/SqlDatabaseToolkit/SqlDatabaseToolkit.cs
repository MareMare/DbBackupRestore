﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlDatabaseToolkit.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqlDatabaseToolkit;

public interface ISqlDatabaseToolkit
{
    Task BackupAsync(CancellationToken cancellationToken = default);

    Task RestoreAsync(CancellationToken cancellationToken = default);
}

internal class SqlDatabaseToolkit : ISqlDatabaseToolkit
{
    private readonly SqlDatabaseOptions _options;
    private readonly string _connectionString;
    private readonly ILogger<SqlDatabaseToolkit>? _logger;

    public SqlDatabaseToolkit(IOptions<SqlDatabaseOptions> options, ILogger<SqlDatabaseToolkit>? logger = null)
    {
        var option = this._options = options.Value;
        this._connectionString = option.ConnectionString;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task BackupAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(this._options.BackupDirectory) ?? string.Empty;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        foreach (var database in this._options.Databases)
        {
            var backupFilePath = Path.Combine(this._options.BackupDirectory, $"{database.Name}.bak");
            await this.BackupCoreAsync(
                    database.Name,
                    backupFilePath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(this._options.RestoreDirectory) ?? string.Empty;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        foreach (var database in this._options.Databases)
        {
            var backupFilePath = Path.Combine(this._options.BackupDirectory, $"{database.Name}.bak");
            await this.RestoreCoreAsync(
                    database.Name,
                    backupFilePath,
                    this._options.RestoreDirectory,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task BackupCoreAsync(
        string databaseName,
        string backupFilePath,
        CancellationToken cancellationToken = default)
    {
        var description = $"{databaseName} - 完全バックアップ";
        var sql = new StringBuilder()
            .Append("BACKUP DATABASE @databaseName")
            .Append(" TO DISK = @backupFilePath WITH NOFORMAT")
            .Append(", NAME = @description")
            .Append(", NOINIT")
            .Append(", SKIP")
            .Append(", NOREWIND")
            .Append(", NOUNLOAD")
            .Append(", STATS = 10")
            .ToString();

        var connection = new SqlConnection(this._connectionString);
        try
        {
            this._logger?.LogDebug("完全バックアップを開始します。{Database} {Backup}", databaseName, backupFilePath);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(
                    sql: sql,
                    param: new { databaseName, description, backupFilePath },
                    commandType: CommandType.Text,
                    commandTimeout: (int?)TimeSpan.FromSeconds(10).TotalSeconds)
                .ConfigureAwait(false);
            this._logger?.LogInformation("完全バックアップが完了しました。{Database} {Backup}", databaseName, backupFilePath);
        }
        catch (Exception ex)
        {
            this._logger?.LogWarning(ex, "完全バックアップ中に例外が発生しました。{Database} {Backup}", databaseName, backupFilePath);
            throw;
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task RestoreCoreAsync(
        string databaseName,
        string backupFilePath,
        string restoreDirectoryPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger?.LogDebug("リストアを開始します。{Database} {Backup} --> {Restore}", databaseName, backupFilePath, restoreDirectoryPath);

            var pairs = await GetFilePairsAsync(
                    this._connectionString,
                    backupFilePath,
                    restoreDirectoryPath,
                    cancellationToken)
                .ConfigureAwait(false);
            await RestoreCurrentlyAsync(this._connectionString, databaseName, backupFilePath, pairs, cancellationToken).ConfigureAwait(false);

            this._logger?.LogInformation("リストアが完了しました。{Database} {Backup} --> {Restore}", databaseName, backupFilePath, restoreDirectoryPath);
        }
        catch (Exception ex)
        {
            this._logger?.LogWarning(
                ex,
                "リストア中に例外が発生しました。{Database} {Backup} --> {Restore}",
                databaseName,
                backupFilePath,
                restoreDirectoryPath);
            throw;
        }

        static string ResolveNewPhysicalPath(string originalFilePath, string moveToDirectory)
        {
            return Path.Combine(moveToDirectory, Path.GetFileName(originalFilePath));
        }

        static async Task<(string logicalName, string physicalName, string moveToFilePath)[]> GetFilePairsAsync(
            string connectionString,
            string backupFilePath,
            string restoreDirectoryPath,
            CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(connectionString);
            try
            {
                var sql = new StringBuilder()
                    .Append("RESTORE FILELISTONLY FROM DISK = @backupFilePath")
                    .ToString();

                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var records = await connection.QueryAsync(
                        sql: sql,
                        param: new { backupFilePath },
                        commandType: CommandType.Text,
                        commandTimeout: (int?)TimeSpan.FromSeconds(10).TotalSeconds)
                    .ConfigureAwait(false);

                var results = records
                    .Select(record =>
                        new
                        {
                            LogicalName = (string)record.LogicalName,
                            PhysicalName = (string)record.PhysicalName,
                            MoveToFilePath = ResolveNewPhysicalPath((string)record.PhysicalName, restoreDirectoryPath),
                        })
                    .Select(pair => (pair.LogicalName, pair.PhysicalName, pair.MoveToFilePath))
                    .ToArray();
                return results;
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        static async Task RestoreCurrentlyAsync(
            string connectionString,
            string databaseName,
            string backupFilePath,
            (string logicalName, string physicalName, string moveToFilePath)[] filePairs,
            CancellationToken cancellationToken = default)
        {
            var builder = new StringBuilder()
                .Append("RESTORE DATABASE @databaseName")
                .Append(" FROM DISK = @backupFilePath WITH REPLACE")
                .Append(",NOUNLOAD")
                .Append(",STATS = 5");
            foreach (var (logicalName, _, moveToFilePath) in filePairs)
            {
                var formattedString = $",MOVE N'{logicalName}' TO N'{moveToFilePath}'";
                builder.Append(formattedString);
            }

            var sql = builder.ToString();
            var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await connection.ExecuteAsync(
                        sql: $"ALTER DATABASE [{databaseName}] SET OFFLINE WITH ROLLBACK IMMEDIATE",
                        commandType: CommandType.Text,
                        commandTimeout: (int?)TimeSpan.FromSeconds(10).TotalSeconds)
                    .ConfigureAwait(false);
                await connection.ExecuteAsync(
                        sql: sql,
                        param: new { databaseName, backupFilePath, },
                        commandType: CommandType.Text,
                        commandTimeout: (int?)TimeSpan.FromSeconds(10).TotalSeconds)
                    .ConfigureAwait(false);
                await connection.ExecuteAsync(
                        sql: $"ALTER DATABASE [{databaseName}] SET ONLINE",
                        commandType: CommandType.Text,
                        commandTimeout: (int?)TimeSpan.FromSeconds(10).TotalSeconds)
                    .ConfigureAwait(false);
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
