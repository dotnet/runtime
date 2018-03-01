// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class CommandCancelTest
    {
        // Shrink the packet size - this should make timeouts more likely
        private static readonly string s_connStr = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { PacketSize = 512 }).ConnectionString;

        [CheckConnStrSetupFact]
        public static void PlainCancelTest()
        {
            PlainCancel(s_connStr);
        }

        [CheckConnStrSetupFact]
        public static void PlainMARSCancelTest()
        {
            PlainCancel((new SqlConnectionStringBuilder(s_connStr) { MultipleActiveResultSets = true }).ConnectionString);
        }

        [CheckConnStrSetupFact]
        public static void PlainCancelTestAsync()
        {
            PlainCancelAsync(s_connStr);
        }

        [CheckConnStrSetupFact]
        public static void PlainMARSCancelTestAsync()
        {
            PlainCancelAsync((new SqlConnectionStringBuilder(s_connStr) { MultipleActiveResultSets = true }).ConnectionString);
        }

        private static void PlainCancel(string connString)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("select * from dbo.Orders; waitfor delay '00:00:10'; select * from dbo.Orders", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    cmd.Cancel();
                    DataTestUtility.AssertThrowsWrapper<SqlException>(
                        () =>
                        {
                            do
                            {
                                while (reader.Read())
                                {
                                }
                            }
                            while (reader.NextResult());
                        },
                        "A severe error occurred on the current command.  The results, if any, should be discarded.");
                }
            }
        }

        private static void PlainCancelAsync(string connString)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("select * from dbo.Orders; waitfor delay '00:00:10'; select * from dbo.Orders", conn))
            {
                conn.Open();
                Task<SqlDataReader> readerTask = cmd.ExecuteReaderAsync();
                DataTestUtility.AssertThrowsWrapper<SqlException>(
                    () =>
                    {
                        readerTask.Wait(2000);
                        SqlDataReader reader = readerTask.Result;
                        cmd.Cancel();
                        do
                        {
                            while (reader.Read())
                            {
                            }
                        }
                        while (reader.NextResult());
                    },
                    "A severe error occurred on the current command.  The results, if any, should be discarded.");
            }
        }

        [CheckConnStrSetupFact]
        public static void MultiThreadedCancel_NonAsync()
        {
            MultiThreadedCancel(s_connStr, false);
        }

        [CheckConnStrSetupFact]
        public static void MultiThreadedCancel_Async()
        {
            MultiThreadedCancel(s_connStr, true);
        }

        [CheckConnStrSetupFact]
        public static void TimeoutCancel()
        {
            TimeoutCancel(s_connStr);
        }

        [CheckConnStrSetupFact]
        public static void CancelAndDisposePreparedCommand()
        {
            CancelAndDisposePreparedCommand(s_connStr);
        }

        [CheckConnStrSetupFact]
        public static void TimeOutDuringRead()
        {
            TimeOutDuringRead(s_connStr);
        }

        private static void MultiThreadedCancel(string constr, bool async)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                var command = con.CreateCommand();
                command.CommandText = "select * from orders; waitfor delay '00:00:08'; select * from customers";

                Thread rThread1 = new Thread(ExecuteCommandCancelExpected);
                Thread rThread2 = new Thread(CancelSharedCommand);
                Barrier threadsReady = new Barrier(2);
                object state = new Tuple<bool, SqlCommand, Barrier>(async, command, threadsReady);

                rThread1.Start(state);
                rThread2.Start(state);
                rThread1.Join();
                rThread2.Join();

                CommandCancelTest.VerifyConnection(command);
            }
        }

        private static void TimeoutCancel(string constr)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandTimeout = 1;
                cmd.CommandText = "WAITFOR DELAY '00:00:30';select * from Customers";

                string errorMessage = SystemDataResourceManager.Instance.SQL_Timeout;
                DataTestUtility.ExpectFailure<SqlException>(() => cmd.ExecuteReader(), errorMessage);

                VerifyConnection(cmd);
            }
        }

        //InvalidOperationException from connection.Dispose if that connection has prepared command cancelled during reading of data
        private static void CancelAndDisposePreparedCommand(string constr)
        {
            int expectedValue = 1;
            using (var connection = new SqlConnection(constr))
            {
                try
                {
                    // Generate a query with a large number of results.
                    using (var command = new SqlCommand("select @P from sysobjects a cross join sysobjects b cross join sysobjects c cross join sysobjects d cross join sysobjects e cross join sysobjects f", connection))
                    {
                        command.Parameters.Add(new SqlParameter("@P", SqlDbType.Int) { Value = expectedValue });
                        connection.Open();
                        // Prepare the query.
                        // Currently this does nothing until command.ExecuteReader is called.
                        // Ideally this should call sp_prepare up-front.
                        command.Prepare();
                        using (var reader = command.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            if (reader.Read())
                            {
                                int actualValue = reader.GetInt32(0);
                                Assert.True(actualValue == expectedValue, string.Format("Got incorrect value. Expected: {0}, Actual: {1}", expectedValue, actualValue));
                            }
                            // Abandon reading the results.
                            command.Cancel();
                        }
                    }
                }
                finally
                {
                    connection.Dispose(); // before the fix, InvalidOperationException happened here
                }
            }
        }

        private static void VerifyConnection(SqlCommand cmd)
        {
            Assert.True(cmd.Connection.State == ConnectionState.Open, "FAILURE: - unexpected non-open state after Execute!");

            cmd.CommandText = "select 'ABC'"; // Verify Connection
            string value = (string)cmd.ExecuteScalar();
            Assert.True(value == "ABC", "FAILURE: upon validation execute on connection: '" + value + "'");
        }

        private static void ExecuteCommandCancelExpected(object state)
        {
            var stateTuple = (Tuple<bool, SqlCommand, Barrier>)state;
            bool async = stateTuple.Item1;
            SqlCommand command = stateTuple.Item2;
            Barrier threadsReady = stateTuple.Item3;

            string errorMessage = SystemDataResourceManager.Instance.SQL_OperationCancelled;
            DataTestUtility.ExpectFailure<SqlException>(() =>
            {
                threadsReady.SignalAndWait();
                using (SqlDataReader r = command.ExecuteReader())
                {
                    do
                    {
                        while (r.Read())
                        {
                        }
                    } while (r.NextResult());
                }
            }, errorMessage);
        }

        private static void CancelSharedCommand(object state)
        {
            var stateTuple = (Tuple<bool, SqlCommand, Barrier>)state;

            // sleep 1 seconds before cancel to ensure ExecuteReader starts and ensure it does not end before Cancel is called (command is running WAITFOR 8 seconds)
            stateTuple.Item3.SignalAndWait();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            stateTuple.Item2.Cancel();
        }

        private static void TimeOutDuringRead(string constr)
        {
            // Create the proxy
            ProxyServer proxy = ProxyServer.CreateAndStartProxy(constr, out constr);
            proxy.SimulatedPacketDelay = 100;
            proxy.SimulatedOutDelay = true;

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    // Start the command
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT @p", conn);
                    cmd.Parameters.AddWithValue("p", new byte[20000]);
                    SqlDataReader reader = cmd.ExecuteReader();
                    reader.Read();

                    // Tweak the timeout to 1ms, stop the proxy from proxying and then try GetValue (which should timeout)
                    reader.SetDefaultTimeout(1);
                    proxy.PauseCopying();
                    string errorMessage = SystemDataResourceManager.Instance.SQL_Timeout;
                    Exception exception = Assert.Throws<SqlException>(() => reader.GetValue(0));
                    Assert.True(exception.Message.Contains(errorMessage));

                    // Return everything to normal and close
                    proxy.ResumeCopying();
                    reader.SetDefaultTimeout(30000);
                    reader.Dispose();
                }

                proxy.Stop();
            }
            catch
            {
                // In case of error, stop the proxy and dump its logs (hopefully this will help with debugging
                proxy.Stop();
                Console.WriteLine(proxy.GetServerEventLog());
                Assert.True(false, "Error while reading through proxy");
                throw;
            }
        }
    }
}
