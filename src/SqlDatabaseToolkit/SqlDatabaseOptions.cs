// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlDatabaseOptions.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace SqlDatabaseToolkit;

/// <summary>
/// オプション構成を表します。
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class SqlDatabaseOptions
{
    /// <summary>構成キーを表します。</summary>
    public static string Key => nameof(SqlDatabaseOptions);

    /// <summary>
    /// 接続文字列を取得または設定します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="string" /> 型。
    /// <para>接続文字列。既定値は <see langword="null" /> です。</para>
    /// </value>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// バックアップ先のディレクトリパスを取得または設定します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="string" /> 型。
    /// <para>バックアップ先のディレクトリパス。既定値は <see langword="null" /> です。</para>
    /// </value>
    public string BackupDirectory { get; set; } = null!;

    /// <summary>
    /// リストア先のディレクトリパスを取得または設定します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="string" /> 型。
    /// <para>リストア先のディレクトリパス。既定値は <see langword="null" /> です。</para>
    /// </value>
    public string RestoreDirectory { get; set; } = null!;

    /// <summary>
    /// コマンドタイムアウト秒を取得または設定します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="int" /> 型。
    /// <para>コマンドタイムアウト秒。既定値は <see langword="60" /> です。</para>
    /// </value>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// データベース設定のコレクションを取得または設定します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="SqlDatabase" /> 型。
    /// <para>データベース設定のコレクション。既定値は <see langword="null" /> です。</para>
    /// </value>
    public IEnumerable<SqlDatabase> Databases { get; set; } = null!;

    /// <summary>
    /// <see cref="DebuggerDisplayAttribute" /> で表示する文字列を取得します。
    /// </summary>
    /// <value>
    /// 値を表す <see cref="string" /> 型。
    /// <para><see cref="DebuggerDisplayAttribute" /> で表示する文字列。既定値は <see langword="null" /> です。</para>
    /// </value>
    private string DebuggerDisplay => $"Databases={this.Databases.Count()} {this.ConnectionString}";
}
