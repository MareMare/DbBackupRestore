// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBackupFileStore.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SqlDatabaseToolkit;

/// <summary>
/// バックアップファイルのストアを示すインターフェイスを表します。
/// </summary>
public interface IBackupFileStore
{
    /// <summary>
    /// バックアップファイルをストアからダウンロードします。
    /// </summary>
    void Download();

    /// <summary>
    /// 非同期操作として、バックアップファイルをストアへアップロードします。
    /// </summary>
    /// <param name="timestamp">バックアップ日時。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
    Task UploadAsync(DateTime timestamp, CancellationToken cancellationToken = default);
}
