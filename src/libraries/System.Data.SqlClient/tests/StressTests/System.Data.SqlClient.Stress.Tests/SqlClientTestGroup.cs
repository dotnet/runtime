// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Xml;
using Stress.Data;

using DPStressHarness;
using System.IO;

namespace Stress.Data.SqlClient
{
    public class SqlClientTestGroup : DataTestGroup
    {
        /// <summary>
        /// SqlNotificationRequest options string
        /// </summary>
        private static string s_notificationOptions;

        /// <summary>
        /// Connection string for SqlDependency.Start()/Stop()
        /// 
        /// The connection string used for SqlDependency.Start() must always be exactly the same every time 
        /// if you are connecting to the same database with the same user and same application domain, so
        /// don't randomise the connection string for calling SqlDependency.Start()
        /// </summary>
        private static string s_sqlDependencyConnString;

        /// <summary>
        /// A thread which randomly calls SqlConnection.ClearAllPools.
        /// This significantly increases the probability of hitting some bugs, such as:
        ///     vstfdevdiv 674236 (SqlConnection.Open() throws InvalidOperationException for absolutely valid connection request)
        ///     sqlbuvsts 328845 (InvalidOperationException: The requested operation cannot be completed because the connection has been broken.) (this is LSE QFE)
        /// However, calling ClearAllPools all the time might also significantly decrease the probability of hitting some other bug,
        /// so this thread will alternate between hammering on ClearAllPools for several minutes, and then doing nothing for several minutes.
        /// </summary>
        private static Thread s_clearAllPoolsThread;

        /// <summary>
        /// Call .Set() on this to cleanly stop the ClearAllPoolsThread.
        /// </summary>
        private static ManualResetEvent s_clearAllPoolsThreadStop = new ManualResetEvent(false);

        private static void ClearAllPoolsThreadFunc()
        {
            Random rnd = new TrackedRandom((int)Environment.TickCount);

            // Swap between calling ClearAllPools and doing nothing every 5 minutes.
            TimeSpan halfCycleTime = TimeSpan.FromMinutes(5);

            int minWait = 10; // milliseconds
            int maxWait = 1000; // milliseconds

            bool active = true; // Start active so we can hit vstfdevdiv 674236 asap
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!s_clearAllPoolsThreadStop.WaitOne(rnd.Next(minWait, maxWait)))
            {
                if (stopwatch.Elapsed > halfCycleTime)
                {
                    active = !active;
                    stopwatch.Reset();
                    stopwatch.Start();
                }

                if (active)
                {
                    SqlConnection.ClearAllPools();
                }
            }
        }

        public override void GlobalTestSetup()
        {
            base.GlobalTestSetup();

            s_clearAllPoolsThread = new Thread(ClearAllPoolsThreadFunc);
            s_clearAllPoolsThread.Start();

            // set the notification options for SqlNotificationRequest tests
            s_notificationOptions = "service=StressNotifications;local database=" + ((SqlServerDataSource)Source).Database;

            s_sqlDependencyConnString = Factory.CreateBaseConnectionString(null, DataStressFactory.ConnectionStringOptions.DisableMultiSubnetFailover);
        }

        public override void GlobalTestCleanup()
        {
            s_clearAllPoolsThreadStop.Set();
            s_clearAllPoolsThread.Join();

            SqlClientStressFactory factory = Factory as SqlClientStressFactory;
            if (factory != null)
            {
                factory.Terminate();
            }

            base.GlobalTestCleanup();
        }

        public override void GlobalExceptionHandler(Exception e)
        {
            base.GlobalExceptionHandler(e);
        }

        protected override DataStressFactory CreateFactory(ref string scenario, ref DataSource source)
        {
            SqlClientStressFactory factory = new SqlClientStressFactory();
            factory.Initialize(ref scenario, ref source);
            return factory;
        }

        protected override bool IsCommandCancelledException(Exception e)
        {
            return
                base.IsCommandCancelledException(e) ||
                ((e is SqlException || e is InvalidOperationException) && e.Message.ToLower().Contains("operation cancelled")) ||
                (e is SqlException && e.Message.StartsWith("A severe error occurred on the current command.")) ||
                (e is AggregateException && e.InnerException != null && IsCommandCancelledException(e.InnerException)) ||
                (e is System.Reflection.TargetInvocationException && e.InnerException != null && IsCommandCancelledException(e.InnerException));
        }

        protected override bool IsReaderClosedException(Exception e)
        {
            return
                e is TaskCanceledException
                ||
                (
                    e is InvalidOperationException
                    &&
                    (
                        (e.Message.StartsWith("Invalid attempt to call") && e.Message.EndsWith("when reader is closed."))
                        ||
                        e.Message.Equals("Invalid attempt to read when no data is present.")
                        ||
                        e.Message.Equals("Invalid operation. The connection is closed.")
                    )
                )
                ||
                (
                    e is ObjectDisposedException
                    &&
                    (
                        e.Message.Equals("Cannot access a disposed object.\r\nObject name: 'SqlSequentialStream'.")
                        ||
                        e.Message.Equals("Cannot access a disposed object.\r\nObject name: 'SqlSequentialTextReader'.")
                    )
                );
        }

        protected override bool AllowReaderCloseDuringReadAsync()
        {
            return true;
        }

        /// <summary>
        /// Utility function used by async tests
        /// </summary>
        /// <param name="com">SqlCommand to be executed.</param>
        /// <param name="query">Indicates if data is being queried</param>
        /// <param name="xml">Indicates if the query should be executed as an Xml</param>
        /// <param name="useBeginAPI"></param>
        /// <param name="cts">The Cancellation Token Source</param>
        /// <returns>The result of beginning of Async execution.</returns>
        private IAsyncResult SqlCommandBeginExecute(SqlCommand com, bool query, bool xml, bool useBeginAPI, CancellationTokenSource cts = null)
        {
            DataStressErrors.Assert(!(useBeginAPI && cts != null), "Cannot use begin api with CancellationTokenSource");

            CancellationToken token = (cts != null) ? cts.Token : CancellationToken.None;

            if (xml)
            {
                com.CommandText = com.CommandText + " FOR XML AUTO";
                return useBeginAPI ? null : com.ExecuteXmlReaderAsync(token);
            }
            else if (query)
            {
                return useBeginAPI ? null : com.ExecuteReaderAsync(token);
            }
            else
            {
                return useBeginAPI ? null : com.ExecuteNonQueryAsync(token);
            }
        }

        /// <summary>
        /// Utility function used by async tests
        /// </summary>
        /// <param name="rnd"> Used to randomize reader.Read() call, whether it should continue or break, and is passed down to ConsumeReaderAsync</param>
        /// <param name="result"> The Async result from Begin operation.</param>
        /// <param name="com"> The Sql Command to Execute</param>
        /// <param name="query">Indicates if data is being queried and where ExecuteQuery or Non-query to be used with the reader</param>
        /// <param name="xml">Indicates if the query should be executed as an Xml</param>
        /// <param name="cancelled">Indicates if command was cancelled and is used to throw exception if a Command cancellation related exception is encountered</param>
        /// <param name="cts">The Cancellation Token Source</param>
        private void SqlCommandEndExecute(Random rnd, IAsyncResult result, SqlCommand com, bool query, bool xml, bool cancelled, CancellationTokenSource cts = null)
        {
            try
            {
                bool closeReader = ShouldCloseDataReader();
                if (xml)
                {
                    XmlReader reader = null;
                    if (result != null && result is Task<XmlReader>)
                    {
                        reader = AsyncUtils.GetResult<XmlReader>(result);
                    }
                    else
                    {
                        reader = AsyncUtils.ExecuteXmlReader(com);
                    }

                    while (reader.Read())
                    {
                        if (rnd.Next(10) == 0) break;
                        if (rnd.Next(2) == 0) continue;
                        reader.ReadElementContentAsString();
                    }
                    if (closeReader) reader.Dispose();
                }
                else if (query)
                {
                    DataStressReader reader = null;
                    if (result != null && result is Task<SqlDataReader>)
                    {
                        reader = new DataStressReader(AsyncUtils.GetResult<SqlDataReader>(result));
                    }
                    else
                    {
                        reader = new DataStressReader(AsyncUtils.ExecuteReader(com));
                    }

                    CancellationToken token = (cts != null) ? cts.Token : CancellationToken.None;

                    AsyncUtils.WaitAndUnwrapException(ConsumeReaderAsync(reader, false, token, rnd));

                    if (closeReader) reader.Close();
                }
                else
                {
                    if (result != null && result is Task<int>)
                    {
                        int temp = AsyncUtils.GetResult<int>(result);
                    }
                    else
                    {
                        AsyncUtils.ExecuteNonQuery(com);
                    }
                }
            }
            catch (Exception e)
            {
                if (cancelled && IsCommandCancelledException(e))
                {
                    // expected exception, ignore
                }
                else
                {
                    throw;
                }
            }
        }


        /// <summary>
        /// Utility function for tests
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="read"></param>
        /// <param name="poll"></param>
        /// <param name="handle"></param>
        /// <param name="xml"></param>
        private void TestSqlAsync(Random rnd, bool read, bool poll, bool handle, bool xml)
        {
            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                SqlCommand com = (SqlCommand)Factory.GetCommand(rnd, table, conn, read, xml);
                bool useBeginAPI = rnd.NextBool();

                IAsyncResult result = SqlCommandBeginExecute(com, read, xml, useBeginAPI);
                // Cancel 1/10 commands
                bool cancel = (rnd.Next(10) == 0);
                if (cancel)
                {
                    if (com.Connection.State != ConnectionState.Closed) com.Cancel();
                }

                if (result != null)
                    WaitForAsyncOpToComplete(rnd, result, poll, handle);
                // At random end query or forget it
                if (rnd.Next(2) == 0)
                    SqlCommandEndExecute(rnd, result, com, read, xml, cancel);

                // Randomly wait for the command to complete after closing the connection to verify devdiv bug 200550.
                // This was fixed for .NET 4.5 Task-based API, but not for the older Begin/End IAsyncResult API.
                conn.Close();
                if (!useBeginAPI && rnd.NextBool())
                    result.AsyncWaitHandle.WaitOne();
            }
        }

        private void WaitForAsyncOpToComplete(Random rnd, IAsyncResult result, bool poll, bool handle)
        {
            if (poll)
            {
                long ret = 0;
                bool wait = !result.IsCompleted;
                while (wait)
                {
                    wait = !result.IsCompleted;
                    Thread.Sleep(100);
                    if (ret++ > 300) //30 second max wait time  then exit
                        wait = false;
                }
            }
            else if (handle)
            {
                WaitHandle wait = result.AsyncWaitHandle;
                wait.WaitOne(rnd.Next(1000));
            }
        }

        /// <summary>
        /// SqlClient Async Non-blocking Read Test
        /// </summary>
        [StressTest("TestSqlAsyncNonBlockingRead", Weight = 10)]
        public void TestSqlAsyncNonBlockingRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: false, handle: false, xml: false);
        }

        /// <summary>
        /// SqlClient Async Non-blocking Write Test
        /// </summary>
        [StressTest("TestSqlAsyncNonBlockingWrite", Weight = 10)]
        public void TestSqlAsyncNonBlockingWrite()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: false, poll: false, handle: false, xml: false);
        }

        /// <summary>
        /// SqlClient Async Polling Read Test
        /// </summary>        
        [StressTest("TestSqlAsyncPollingRead", Weight = 10)]
        public void TestSqlAsyncPollingRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: true, handle: false, xml: false);
        }

        /// <summary>
        /// SqlClient Async Polling Write Test
        /// </summary>
        [StressTest("TestSqlAsyncPollingWrite", Weight = 10)]
        public void TestSqlAsyncPollingWrite()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: false, poll: true, handle: false, xml: false);
        }

        /// <summary>
        /// SqlClient Async Event Read Test
        /// </summary>
        [StressTest("TestSqlAsyncEventRead", Weight = 10)]
        public void TestSqlAsyncEventRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: false, handle: true, xml: false);
        }

        /// <summary>
        /// SqlClient Async Event Write Test
        /// </summary>        
        [StressTest("TestSqlAsyncEventWrite", Weight = 10)]
        public void TestSqlAsyncEventWrite()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: false, poll: false, handle: true, xml: false);
        }


        /// <summary>
        /// SqlClient Async Xml Non-blocking Read Test
        /// </summary>        
        [StressTest("TestSqlXmlAsyncNonBlockingRead", Weight = 10)]
        public void TestSqlXmlAsyncNonBlockingRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: false, handle: false, xml: true);
        }

        /// <summary>
        /// SqlClient Async Xml Polling Read Test
        /// </summary>        
        [StressTest("TestSqlXmlAsyncPollingRead", Weight = 10)]
        public void TestSqlXmlAsyncPollingRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: true, handle: false, xml: true);
        }

        /// <summary>
        /// SqlClient Async Xml Event Read Test
        /// </summary>
        [StressTest("TestSqlXmlAsyncEventRead", Weight = 10)]
        public void TestSqlXmlAsyncEventRead()
        {
            Random rnd = RandomInstance;
            TestSqlAsync(rnd, read: true, poll: false, handle: true, xml: true);
        }


        [StressTest("TestSqlXmlCommandReader", Weight = 10)]
        public void TestSqlXmlCommandReader()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                SqlCommand com = (SqlCommand)Factory.GetCommand(rnd, table, conn, query: true, isXml: true);
                com.CommandText = com.CommandText + " FOR XML AUTO";

                // Cancel 1/10 commands
                bool cancel = rnd.Next(10) == 0;
                if (cancel)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CommandCancel), com);
                }

                try
                {
                    XmlReader reader = com.ExecuteXmlReader();

                    while (reader.Read())
                    {
                        if (rnd.Next(10) == 0) break;
                        if (rnd.Next(2) == 0) continue;
                        reader.ReadElementContentAsString();
                    }
                    if (rnd.Next(10) != 0) reader.Dispose();
                }
                catch (Exception ex)
                {
                    if (cancel && IsCommandCancelledException(ex))
                    {
                        // expected, ignore
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Utility function used for testing cancellation on Execute*Async APIs.
        /// </summary>
        private void TestSqlAsyncCancellation(Random rnd, bool read, bool xml)
        {
            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                SqlCommand com = (SqlCommand)Factory.GetCommand(rnd, table, conn, read, xml);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task t = (Task)SqlCommandBeginExecute(com, read, xml, false, cts);

                cts.CancelAfter(rnd.Next(2000));
                SqlCommandEndExecute(rnd, (IAsyncResult)t, com, read, xml, true, cts);
            }
        }

        /// <summary>
        /// SqlClient Async Xml Event Read Test
        /// </summary>
        [StressTest("TestExecuteXmlReaderAsyncCancellation", Weight = 10)]
        public void TestExecuteXmlReaderAsyncCancellation()
        {
            Random rnd = RandomInstance;
            TestSqlAsyncCancellation(rnd, true, true);
        }

        /// <summary>
        /// SqlClient Async Xml Event Read Test
        /// </summary>
        [StressTest("TestExecuteReaderAsyncCancellation", Weight = 10)]
        public void TestExecuteReaderAsyncCancellation()
        {
            Random rnd = RandomInstance;
            TestSqlAsyncCancellation(rnd, true, false);
        }

        /// <summary>
        /// SqlClient Async Xml Event Read Test
        /// </summary>
        [StressTest("TestExecuteNonQueryAsyncCancellation", Weight = 10)]
        public void TestExecuteNonQueryAsyncCancellation()
        {
            Random rnd = RandomInstance;
            TestSqlAsyncCancellation(rnd, false, false);
        }


        private class MARSCommand
        {
            internal SqlCommand cmd;
            internal IAsyncResult result;
            internal bool query;
            internal bool xml;
        }

        [StressTest("TestSqlAsyncMARS", Weight = 10)]
        public void TestSqlAsyncMARS()
        {
            const int MaxCmds = 11;
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd, DataStressFactory.ConnectionStringOptions.EnableMars))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);

                // MARS session cache is by default 10. 
                // This is documented here: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/enabling-multiple-active-result-sets
                // We want to stress test this by allowing 11 concurrent commands. Hence the max in rnd.Next below is 12.
                MARSCommand[] cmds = new MARSCommand[rnd.Next(5, MaxCmds + 1)];

                for (int i = 0; i < cmds.Length; i++)
                {
                    cmds[i] = new MARSCommand();

                    // Make every 3rd query xml reader
                    if (i % 3 == 0)
                    {
                        cmds[i].query = true;
                        cmds[i].xml = true;
                    }
                    else
                    {
                        cmds[i].query = rnd.NextBool();
                        cmds[i].xml = false;
                    }

                    cmds[i].cmd = (SqlCommand)Factory.GetCommand(rnd, table, conn, cmds[i].query, cmds[i].xml);
                    cmds[i].result = SqlCommandBeginExecute(cmds[i].cmd, cmds[i].query, cmds[i].xml, rnd.NextBool());
                    if (cmds[i].result != null)
                        WaitForAsyncOpToComplete(rnd, cmds[i].result, true, false);
                }

                // After all commands have been launched, wait for them to complete now.
                for (int i = 0; i < cmds.Length; i++)
                {
                    SqlCommandEndExecute(rnd, cmds[i].result, cmds[i].cmd, cmds[i].query, cmds[i].xml, false);
                }
            }
        }


        [StressTest("TestStreamInputParameter", Weight = 10)]
        public void TestStreamInputParameter()
        {
            Random rnd = RandomInstance;
            int dataSize = 100000;
            byte[] data = new byte[dataSize];
            rnd.NextBytes(data);

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                SqlCommand cmd = (SqlCommand)conn.CreateCommand();
                cmd.CommandText = "SELECT @blob";
                SqlParameter param = cmd.Parameters.Add("@blob", SqlDbType.VarBinary, dataSize);
                param.Direction = ParameterDirection.Input;
                param.Value = new MemoryStream(data);
                CommandExecute(rnd, cmd, true);
            }
        }

        [StressTest("TestTextReaderInputParameter", Weight = 10)]
        public void TestTextReaderInputParameter()
        {
            Random rnd = RandomInstance;
            int dataSize = 100000;
            string data = new string('a', dataSize);

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                SqlCommand cmd = (SqlCommand)conn.CreateCommand();
                cmd.CommandText = "SELECT @blob";
                SqlParameter param = cmd.Parameters.Add("@blob", SqlDbType.VarChar, dataSize);
                param.Direction = ParameterDirection.Input;
                param.Value = new StringReader(data);
                CommandExecute(rnd, cmd, true);
            }
        }
    }
}
