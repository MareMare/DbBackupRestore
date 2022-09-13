// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlDatabaseToolkit.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqlDatabaseToolkit;

/// <summary>
/// SQL Server 用のツールキットを表します。
/// </summary>
internal class SqlDatabaseToolkit : ISqlDatabaseToolkit
{
    /// <summary>オプション構成を表します。</summary>
    private readonly SqlDatabaseOptions _options;

    /// <summary>接続文字列を表します。</summary>
    private readonly string _connectionString;

    /// <summary><see cref="ILogger{TCategory}" /> を表します。</summary>
    private readonly ILogger<SqlDatabaseToolkit>? _logger;

    /// <summary>
    /// <see cref="SqlDatabaseToolkit" /> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="options"><see cref="IOptions{SqlDatabaseOptions}" />。</param>
    /// <param name="logger"><see cref="ILogger{SqlDatabaseToolkit}" />。</param>
    public SqlDatabaseToolkit(IOptions<SqlDatabaseOptions> options, ILogger<SqlDatabaseToolkit>? logger = null)
    {
        this._options = options.Value;
        this._logger = logger;
        this._connectionString = this._options.ConnectionString;
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public async Task BackupAsync(CancellationToken cancellationToken = default)
    {
        SqlDatabaseToolkit.PrepareDirectory(this._options.SqlServerAccount, this._options.BackupDirectory);

        foreach (var database in this._options.Databases)
        {
            var backupFilePath = database.ResolveBackupFilePath(this._options.BackupDirectory);
            await this.BackupCoreAsync(
                    database.Name,
                    backupFilePath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        SqlDatabaseToolkit.PrepareDirectory(this._options.SqlServerAccount, this._options.RestoreDirectory);

        foreach (var database in this._options.Databases)
        {
            var backupFilePath = database.ResolveBackupFilePath(this._options.BackupDirectory);
            await this.RestoreCoreAsync(
                    database.Name,
                    backupFilePath,
                    this._options.RestoreDirectory,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 指定されたディレクトリに SQL Server に対するフルコントロールのアクセス権を付与します。
    /// </summary>
    /// <param name="sqlServerAccount">SQL Server のサービスアカウント名。</param>
    /// <param name="directory">ディレクトリパス。</param>
    [SupportedOSPlatform("windows")]
    private static void PrepareDirectory(string sqlServerAccount, string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // https://docs.microsoft.com/ja-jp/dotnet/api/system.security.accesscontrol.directorysecurity?view=net-6.0
        var directoryInfo = new DirectoryInfo(directory);
        var rule = new FileSystemAccessRule(
            new NTAccount(sqlServerAccount),
            FileSystemRights.FullControl,
            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
            PropagationFlags.None,
            AccessControlType.Allow);
        var security = directoryInfo.GetAccessControl();
        security.RemoveAccessRule(rule);
        security.AddAccessRule(rule);
        directoryInfo.SetAccessControl(security);
    }

    /// <summary>
    /// 非同期操作として、バックアップを行います。
    /// </summary>
    /// <param name="databaseName">データベース名。</param>
    /// <param name="backupFilePath">バックアップファイルパス。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
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
        var commandTimeoutSeconds = this._options.CommandTimeoutSeconds;
        try
        {
            this._logger?.LogDebug("完全バックアップを開始します。{Database} {Backup}", databaseName, backupFilePath);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(
                    sql,
                    new { databaseName, description, backupFilePath },
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeoutSeconds)
                .ConfigureAwait(false);
            this._logger?.LogInformation("完全バックアップが完了しました。{Database} {Backup}", databaseName, backupFilePath);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(ex, "完全バックアップ中に例外が発生しました。{Database} {Backup}", databaseName, backupFilePath);
            throw;
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 非同期操作として、バックアップファイルからのリストアを行います。
    /// </summary>
    /// <param name="databaseName">データベース名。</param>
    /// <param name="backupFilePath">バックアップファイルパス。</param>
    /// <param name="restoreDirectoryPath">リストア先のディレクトリパス。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
    private async Task RestoreCoreAsync(
        string databaseName,
        string backupFilePath,
        string restoreDirectoryPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger?.LogDebug(
                "リストアを開始します。{Database} {Backup} --> {Restore}",
                databaseName,
                backupFilePath,
                restoreDirectoryPath);

            var commandTimeoutSeconds = this._options.CommandTimeoutSeconds;
            var pairs = await GetFilePairsAsync(
                    this._connectionString,
                    backupFilePath,
                    restoreDirectoryPath,
                    commandTimeoutSeconds,
                    cancellationToken)
                .ConfigureAwait(false);
            await RestoreCurrentlyAsync(
                    this._connectionString,
                    databaseName,
                    backupFilePath,
                    pairs,
                    commandTimeoutSeconds,
                    cancellationToken)
                .ConfigureAwait(false);

            this._logger?.LogInformation(
                "リストアが完了しました。{Database} {Backup} --> {Restore}",
                databaseName,
                backupFilePath,
                restoreDirectoryPath);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(
                ex,
                "リストア中に例外が発生しました。{Database} {Backup} --> {Restore}",
                databaseName,
                backupFilePath,
                restoreDirectoryPath);
            throw;
        }

        static string ResolveNewPhysicalPath(string originalFilePath, string moveToDirectory) =>
            Path.Combine(moveToDirectory, Path.GetFileName(originalFilePath));

        static async Task<(string LogicalName, string PhysicalName, string MoveToFilePath)[]> GetFilePairsAsync(
            string connectionString,
            string backupFilePath,
            string restoreDirectoryPath,
            int commandTimeoutSeconds,
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
                        sql,
                        new { backupFilePath },
                        commandType: CommandType.Text,
                        commandTimeout: commandTimeoutSeconds)
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
            IEnumerable<(string LogicalName, string PhysicalName, string MoveToFilePath)> filePairs,
            int commandTimeoutSeconds,
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
                        $"ALTER DATABASE [{databaseName}] SET OFFLINE WITH ROLLBACK IMMEDIATE",
                        commandType: CommandType.Text,
                        commandTimeout: commandTimeoutSeconds)
                    .ConfigureAwait(false);
                await connection.ExecuteAsync(
                        sql,
                        new { databaseName, backupFilePath },
                        commandType: CommandType.Text,
                        commandTimeout: commandTimeoutSeconds)
                    .ConfigureAwait(false);
                await connection.ExecuteAsync(
                        $"ALTER DATABASE [{databaseName}] SET ONLINE",
                        commandType: CommandType.Text,
                        commandTimeout: commandTimeoutSeconds)
                    .ConfigureAwait(false);
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
