// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupFileStore.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
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
    public void Download()
    {
        if (string.IsNullOrEmpty(this._options.ArchiveDirectory))
        {
            this._logger?.LogInformation("圧縮ファイル格納先のディレクトリパスが未指定なのでダウンロードしません。");
            return;
        }

        BackupFileStore.PrepareDirectory(this._options.ArchiveDirectory);
        BackupFileStore.PrepareDirectory(this._options.BackupDirectory);

        var zipFileName = BackupFileStore.ResolveZipFileName();
        var searchPattern = Regex.Replace(zipFileName, @"[\d]", _ => "?");
        var foundFileInfo = Directory.EnumerateFiles(this._options.ArchiveDirectory, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .MaxBy(fi => fi.LastWriteTime);
        if (foundFileInfo is null)
        {
            return;
        }

        ZipFile.ExtractToDirectory(foundFileInfo.FullName, this._options.BackupDirectory, true);
        this._logger?.LogInformation("圧縮ファイルをダウンロードしました。{FileName}", zipFileName);
    }

    /// <inheritdoc />
    public async Task UploadAsync(DateTime timestamp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(this._options.ArchiveDirectory))
        {
            this._logger?.LogInformation("圧縮ファイル格納先のディレクトリパスが未指定なのでアップロードしません。");
            return;
        }

        var zipFileName = BackupFileStore.ResolveZipFileName(timestamp);
        var zipFilePath = Path.Combine(this._options.BackupDirectory, zipFileName);
        var zipFilePathToUpload = Path.Combine(this._options.ArchiveDirectory, zipFileName);

        var backupFileInfos = this._options.Databases
            .Select(database => database.ResolveBackupFilePath(this._options.BackupDirectory))
            .Select(path => new FileInfo(path))
            .Where(fi => fi.Exists)
            .ToArray();
        try
        {
            BackupFileStore.DeleteFileSafely(zipFilePath);
            await this.CompressCoreAsync(zipFilePath, backupFileInfos, cancellationToken).ConfigureAwait(false);
            this._logger?.LogInformation("圧縮ファイルを生成しました。{FileName}", zipFileName);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(ex, "圧縮ファイルの生成中に例外が発生しました。{FileName}", zipFileName);
            BackupFileStore.DeleteFileSafely(zipFilePath);
            throw;
        }

        try
        {
            BackupFileStore.PrepareDirectory(this._options.ArchiveDirectory);
            BackupFileStore.DeleteFileSafely(zipFilePathToUpload);
            File.Copy(zipFilePath, zipFilePathToUpload);
            this._logger?.LogInformation("圧縮ファイルをアップロードしました。{FileName}", zipFileName);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(ex, "圧縮ファイルのアップロード中に例外が発生しました。{FileName}", zipFileName);
            throw;
        }
        finally
        {
            BackupFileStore.DeleteFileSafely(zipFilePath);
        }

        try
        {
            this.PurgeStore(zipFilePath);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(ex, "ストアのパージ中に例外が発生しました。");
            throw;
        }
    }

    /// <summary>
    /// 圧縮ファイル名を解決します。
    /// </summary>
    /// <param name="timestamp">日時。</param>
    /// <returns>圧縮ファイル名。</returns>
    private static string ResolveZipFileName(DateTime? timestamp = null) =>
        $"Backup_{(timestamp ?? DateTime.Now):yyyyMMddHHmm}.zip";

    /// <summary>
    /// 安全にファイルを削除します。
    /// </summary>
    /// <param name="filePath">削除するファイルパス。</param>
    private static void DeleteFileSafely(string filePath)
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
    /// 指定されたディレクトリを準備します。
    /// </summary>
    /// <param name="directory">ディレクトリパス。</param>
    private static void PrepareDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
    /// ストアをパージします。
    /// </summary>
    /// <param name="zipFilePath">圧縮されたバックアップファイル。</param>
    private void PurgeStore(string zipFilePath)
    {
        var zipFileName = Path.GetFileName(zipFilePath);
        var searchPattern = Regex.Replace(zipFileName, @"[\d]", _ => "?");
        var foundFileInfos = Directory.EnumerateFiles(this._options.ArchiveDirectory, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(fi => fi.LastWriteTime)
            .ToArray();
        var fileInfosToDelete = foundFileInfos.Skip(3).ToArray(); // 今回分を含めて最新 3 世代を保有します。
        foreach (var fi in fileInfosToDelete)
        {
            fi.Delete();
            this._logger?.LogInformation("削除対象のファイルをストアから削除しました。{FilePath}", fi.Name);
        }
    }
}
