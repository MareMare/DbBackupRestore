// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlDatabase.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace SqlDatabaseToolkit
{
    /// <summary>
    /// データベース設定を表します。
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SqlDatabase
    {
        /// <summary>
        /// データベース名を取得または設定します。
        /// </summary>
        /// <value>
        /// 値を表す <see cref="string" /> 型。
        /// <para>データベース名。既定値は <see langword="null" /> です。</para>
        /// </value>
        public string Name { get; set; } = null!;

        /// <summary>
        /// 拡張子を含むバックアップファイル名を取得します。
        /// </summary>
        /// <value>
        /// 値を表す <see cref="string" /> 型。
        /// <para>拡張子を含むバックアップファイル名。既定値は <see langword="null" /> です。</para>
        /// </value>
        public string BackupFileName => $"{this.Name}.bak";

        /// <summary>
        /// <see cref="DebuggerDisplayAttribute" /> で表示する文字列を取得します。
        /// </summary>
        /// <value>
        /// 値を表す <see cref="string" /> 型。
        /// <para><see cref="DebuggerDisplayAttribute" /> で表示する文字列。既定値は <see langword="null" /> です。</para>
        /// </value>
        private string DebuggerDisplay => this.Name;

        /// <summary>
        /// バックアップファイルパスを解決します。
        /// </summary>
        /// <param name="directory">基準のディレクトリ。</param>
        /// <returns>バックアップファイルパス。</returns>
        public string ResolveBackupFilePath(string directory) => Path.Combine(directory, this.BackupFileName);
    }
}
