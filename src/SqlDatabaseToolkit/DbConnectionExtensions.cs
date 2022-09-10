// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbConnectionExtensions.cs" company="MareMare">
// Copyright © 2022 MareMare. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Data;
using Dapper;

namespace SqlDatabaseToolkit;

public static class DbConnectionExtensions
{
    private const int DefaultCommandTimeoutSeconds = 5;

    public static int ExecuteSql(this IDbConnection connection, string sql, dynamic? param = null, dynamic? outParam = null, int? commandTimeout = null, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        DbConnectionExtensions.CombineParameters(ref param, outParam);
        var affectedRows = connection
            .Execute(
                sql,
                param: param != null ? (object)param : null,
                transaction: transaction,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? DbConnectionExtensions.DefaultCommandTimeoutSeconds);
        return affectedRows;
    }

    public static async Task<int> ExecuteSqlAsync(this IDbConnection connection, string sql, dynamic? param = null, dynamic? outParam = null, int? commandTimeout = null, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        DbConnectionExtensions.CombineParameters(ref param, outParam);
        var affectedRows = await connection
            .ExecuteAsync(
                sql,
                param: param as object,
                transaction: transaction,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? DbConnectionExtensions.DefaultCommandTimeoutSeconds)
            .ConfigureAwait(false);
        return affectedRows;
    }

    private static void CombineParameters(ref dynamic param, dynamic? outParam = null)
    {
        if (outParam != null)
        {
            if (param != null)
            {
                param = new DynamicParameters(param);
                ((DynamicParameters)param).AddDynamicParams(outParam);
            }
            else
            {
                param = outParam;
            }
        }
    }
}
