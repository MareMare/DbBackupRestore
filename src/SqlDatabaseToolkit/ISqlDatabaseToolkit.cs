// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISqlDatabaseToolkit.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SqlDatabaseToolkit;

/// <summary>
/// SQL Server 用のツールキットを示すインターフェイスを表します。
/// </summary>
public interface ISqlDatabaseToolkit
{
    /// <summary>
    /// 非同期操作として、バックアップを行います。
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
    Task BackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 非同期操作として、リストアを行います。
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken" />。</param>
    /// <returns>完了を表す <see cref="Task" />。</returns>
    Task RestoreAsync(CancellationToken cancellationToken = default);
}
