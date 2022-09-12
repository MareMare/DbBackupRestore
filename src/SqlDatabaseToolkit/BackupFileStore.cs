// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupFileStore.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqlDatabaseToolkit;

/// <summary>
/// バックアップファイルのストアを表します。
/// </summary>
internal class BackupFileStore : IBackupFileStore
{
    /// <summary>オプション構成を表します。</summary>
    private readonly SqlDatabaseOptions _options;

    /// <summary><see cref="ILogger{TCategory}" /> を表します。</summary>
    private readonly ILogger<BackupFileStore>? _logger;

    /// <summary>
    /// <see cref="BackupFileStore" /> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="options"><see cref="IOptions{SqlDatabaseOptions}" />。</param>
    /// <param name="logger"><see cref="ILogger{BackupFileStore}" />。</param>
    public BackupFileStore(IOptions<SqlDatabaseOptions> options, ILogger<BackupFileStore>? logger = null)
    {
        this._options = options.Value;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task UploadAsync(DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var zipFileName = $"{timestamp:yyyyMMddHHmm}.zip";
        var zipFilePath = Path.Combine(this._options.BackupDirectory, zipFileName);

        var backupFileInfos = this._options.Databases
            .Select(database => database.ResolveBackupFilePath(this._options.BackupDirectory))
            .Select(path => new FileInfo(path))
            .Where(fi => fi.Exists)
            .ToArray();
        try
        {
            BackupFileStore.DeleteSafely(zipFilePath);
            await this.CompressCoreAsync(zipFilePath, backupFileInfos, cancellationToken).ConfigureAwait(false);
            this._logger?.LogInformation("圧縮ファイルを生成しました。{FileName}", zipFileName);
        }
        catch (Exception ex)
        {
            this._logger?.LogWarning(ex, "圧縮ファイルの生成中に例外が発生しました。{FileName}", zipFileName);
            BackupFileStore.DeleteSafely(zipFilePath);
            throw;
        }

        try
        {
            this.UploadCore(zipFilePath);
        }
        finally
        {
            BackupFileStore.DeleteSafely(zipFilePath);
        }
    }

    /// <summary>
    /// 安全にファイルを削除します。
    /// </summary>
    /// <param name="filePath">削除するファイルパス。</param>
    private static void DeleteSafely(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

#pragma warning disable CA1031 // 一般的な例外の種類はキャッチしません
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // 例外を握りつぶします。
        }
#pragma warning restore CA1031 // 一般的な例外の種類はキャッチしません
    }

    /// <summary>
    /// 非同期操作として、バックアップファイルを圧縮します。
    /// </summary>
    /// <param name="zipFilePath">生成する圧縮ファイルのファイルパス。</param>
    /// <param name="backupFileInfos">圧縮されるバックアップの <see cref="FileInfo" /> コレクション。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
    private async Task CompressCoreAsync(
        string zipFilePath,
        IEnumerable<FileInfo> backupFileInfos,
        CancellationToken cancellationToken = default)
    {
        FileStream? zipFileStream = null;
        try
        {
            zipFileStream = File.Open(zipFilePath, FileMode.Create, FileAccess.Write);
            using var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            foreach (var fi in backupFileInfos)
            {
                var entry = zipArchive.CreateEntry(fi.Name);
                entry.LastWriteTime = fi.LastWriteTime;

                Stream? stream = null;
                FileStream? backupFile = null;
                try
                {
                    var sw = Stopwatch.StartNew();
                    this._logger?.LogDebug("バックアップファイルを圧縮します。{FileName}", fi.Name);

                    stream = entry.Open();
                    backupFile = fi.Open(FileMode.Open, FileAccess.Read);
                    await backupFile.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

                    this._logger?.LogInformation(
                        "バックアップファイルを圧縮しました。{FileName} {ElapsedMilliseconds}[ms]",
                        fi.Name,
                        sw.ElapsedMilliseconds);
                }
                finally
                {
                    if (stream is not null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }

                    if (backupFile is not null)
                    {
                        await backupFile.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        finally
        {
            if (zipFileStream is not null)
            {
                await zipFileStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// アップロードします。
    /// </summary>
    /// <param name="zipFilePath">圧縮されたバックアップファイル。</param>
    private void UploadCore(string zipFilePath)
    {
        var zipFileName = Path.GetFileName(zipFilePath);
        var filePathToUpload = Path.Combine(this._options.ArchiveDirectory, zipFileName);
        File.Copy(zipFilePath, filePathToUpload);
    }
}
