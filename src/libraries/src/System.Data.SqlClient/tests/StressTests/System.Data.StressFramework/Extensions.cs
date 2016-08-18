// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Stress.Data
{
    public static class Extensions
    {
        /// <param name="probability">the probability that true will be returned</param>
        public static bool NextBool(this Random rnd, double probability)
        {
            return rnd.NextDouble() < probability;
        }

        /// <summary>
        /// Generate a true or false with equal probability.
        /// </summary>
        public static bool NextBool(this Random rnd)
        {
            return rnd.NextBool(0.5);
        }

        public static Task<int> ExecuteNonQuerySyncOrAsync(this DbCommand command, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                command.ExecuteNonQuery,
                () => command.ExecuteNonQueryAsync(token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<object> ExecuteScalarSyncOrAsync(this DbCommand command, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                command.ExecuteScalar,
                () => command.ExecuteScalarAsync(token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<DbDataReader> ExecuteReaderSyncOrAsync(this DbCommand command, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                command.ExecuteReader,
                () => command.ExecuteReaderAsync(token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<DbDataReader> ExecuteReaderSyncOrAsync(this DbCommand command, CommandBehavior cb, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => command.ExecuteReader(cb),
                () => command.ExecuteReaderAsync(cb, token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<SqlDataReader> ExecuteReaderSyncOrAsync(this SqlCommand command, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                command.ExecuteReader,
                () => command.ExecuteReaderAsync(token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<SqlDataReader> ExecuteReaderSyncOrAsync(this SqlCommand command, CommandBehavior cb, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => command.ExecuteReader(cb),
                () => command.ExecuteReaderAsync(cb, token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public static Task<XmlReader> ExecuteXmlReaderSyncOrAsync(this SqlCommand command, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                command.ExecuteXmlReader,
                () => command.ExecuteXmlReaderAsync(token),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }
    }
}
