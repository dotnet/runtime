// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class AsyncTest
    {
        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void TestReadAsyncTimeConsumed()
        {
            const string sql = "SET NOCOUNT ON"
                            + " SELECT 'a'"
                            + " DECLARE @t DATETIME = SYSDATETIME()"
                            + " WHILE DATEDIFF(s, @t, SYSDATETIME()) < 20 BEGIN"
                            + "   SELECT 2 x INTO #y"
                            + "   DROP TABLE #y"
                            + " END"
                            + " SELECT 'b'";
            Task<double> t = RunReadAsync(sql);
            double elapsedSync = RunReadSync(sql);
            t.Wait();
            double elapsedAsync = t.Result;
            Assert.True(elapsedAsync < elapsedSync, "Asynchronous operation should be finished quicker than synchronous one");
            int limit = 100;
            Assert.True(elapsedAsync < limit, $"Asynchronous operation should be finished within {limit}ms");
        }

        private static async Task<double> RunReadAsync(string sql)
        {
            double maxElapsedTimeMillisecond = 0;
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                await connection.OpenAsync();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        Task<bool> t;
                        Stopwatch stopwatch = new Stopwatch();
                        do
                        {
                            do
                            {
                                stopwatch.Start();
                                t = reader.ReadAsync();
                                stopwatch.Stop();
                                double elased = stopwatch.Elapsed.TotalMilliseconds;
                                if (maxElapsedTimeMillisecond < elased)
                                {
                                    maxElapsedTimeMillisecond = elased;
                                }
                            }
                            while (await t);
                        }
                        while (reader.NextResult());
                    }
                }
            }

            return maxElapsedTimeMillisecond;
        }

        private static double RunReadSync(string sql)
        {
            double maxElapsedTimeMillisecond = 0;
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        bool result;
                        Stopwatch stopwatch = new Stopwatch();
                        do
                        {
                            do
                            {
                                stopwatch.Start();
                                result = reader.Read();
                                stopwatch.Stop();
                                double elased = stopwatch.Elapsed.TotalMilliseconds;
                                if (maxElapsedTimeMillisecond < elased)
                                {
                                    maxElapsedTimeMillisecond = elased;
                                }
                            }
                            while (result);
                        }
                        while (reader.NextResult());
                    }
                }
            }

            return maxElapsedTimeMillisecond;
        }
    }
}