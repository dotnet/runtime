// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class AsyncTest
    {
        private const int TaskTimeout = 5000;

        [CheckConnStrSetupFact]
        public static void ExecuteTest()
        {
            SqlCommand com = new SqlCommand("select * from Orders");
            SqlConnection con = new SqlConnection(DataTestUtility.TcpConnStr);

            com.Connection = con;

            con.Open();

            Task<SqlDataReader> readerTask = com.ExecuteReaderAsync();
            bool taskCompleted = readerTask.Wait(TaskTimeout);
            Assert.True(taskCompleted, "FAILED: ExecuteReaderAsync Task did not complete successfully.");

            SqlDataReader reader = readerTask.Result;

            int rows;
            for (rows = 0; reader.Read(); rows++) ;

            Assert.True(rows == 830, string.Format("FAILED: ExecuteTest reader had wrong number of rows. Expected: {0}. Actual: {1}", 830, rows));

            reader.Dispose();
            con.Close();
        }

        [CheckConnStrSetupFact]
        public static void FailureTest()
        {
            bool failure = false;
            bool taskCompleted = false;

            SqlCommand com = new SqlCommand("select * from Orders");
            SqlConnection con = new SqlConnection((new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { Pooling = false }).ConnectionString);
            com.Connection = con;
            con.Open();

            Task<int> nonQueryTask = com.ExecuteNonQueryAsync();
            try
            {
                com.ExecuteNonQueryAsync().Wait(TaskTimeout);
            }
            catch (AggregateException agrEx)
            {
                agrEx.Handle(
                    (ex) =>
                    {
                        Assert.True(ex is InvalidOperationException, "FAILED: Thrown exception for ExecuteNonQueryAsync was not an InvalidOperationException");
                        failure = true;
                        return true;
                    });
            }
            Assert.True(failure, "FAILED: No exception thrown after trying second ExecuteNonQueryAsync.");
            failure = false;

            taskCompleted = nonQueryTask.Wait(TaskTimeout);
            Assert.True(taskCompleted, "FAILED: ExecuteNonQueryAsync Task did not complete successfully.");

            Task<SqlDataReader> readerTask = com.ExecuteReaderAsync();
            try
            {
                com.ExecuteReaderAsync().Wait(TaskTimeout);
            }
            catch (AggregateException agrEx)
            {
                agrEx.Handle(
                    (ex) =>
                    {
                        Assert.True(ex is InvalidOperationException, "FAILED: Thrown exception for ExecuteReaderAsync was not an InvalidOperationException: " + ex);
                        failure = true;
                        return true;
                    });
            }
            Assert.True(failure, "FAILED: No exception thrown after trying second ExecuteReaderAsync.");

            taskCompleted = readerTask.Wait(TaskTimeout);
            Assert.True(taskCompleted, "FAILED: ExecuteReaderAsync Task did not complete successfully.");

            readerTask.Result.Dispose();
            con.Close();
        }
    }
}