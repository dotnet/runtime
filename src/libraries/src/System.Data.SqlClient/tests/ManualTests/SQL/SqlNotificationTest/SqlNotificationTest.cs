// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SqlNotificationTest : IDisposable
    {
        // Misc constants
        private const int CALLBACK_TIMEOUT = 5000; // milliseconds

        // Database schema
        private readonly string _tableName = "dbo.[SQLDEP_" + Guid.NewGuid().ToString() + "]";
        private readonly string _queueName = "SQLDEP_" + Guid.NewGuid().ToString();
        private readonly string _serviceName = "SQLDEP_" + Guid.NewGuid().ToString();
        private readonly string _schemaQueue;

        // Connection information used by all tests
        private readonly string _startConnectionString;
        private readonly string _execConnectionString;

        public SqlNotificationTest()
        {
            _startConnectionString = DataTestUtility.TcpConnStr;
            _execConnectionString = DataTestUtility.TcpConnStr;

            var startBuilder = new SqlConnectionStringBuilder(_startConnectionString);
            if (startBuilder.IntegratedSecurity)
            {
                _schemaQueue = string.Format("[{0}]", _queueName);
            }
            else
            {
                _schemaQueue = string.Format("[{0}].[{1}]", startBuilder.UserID, _queueName);
            }

            Setup();
        }

        public void Dispose()
        {
            Cleanup();
        }

        #region StartStop_Tests

        [CheckConnStrSetupFact]
        public void Test_DoubleStart_SameConnStr()
        {
            Assert.True(SqlDependency.Start(_startConnectionString), "Failed to start listener.");

            Assert.False(SqlDependency.Start(_startConnectionString), "Expected failure when trying to start listener.");

            Assert.False(SqlDependency.Stop(_startConnectionString), "Expected failure when trying to completely stop listener.");

            Assert.True(SqlDependency.Stop(_startConnectionString), "Failed to stop listener.");
        }

        [CheckConnStrSetupFact]
        public void Test_DoubleStart_DifferentConnStr()
        {
            SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder(_startConnectionString);

            // just change something that doesn't impact the dependency dispatcher
            if (cb.ShouldSerialize("connect timeout"))
                cb.ConnectTimeout = cb.ConnectTimeout + 1;
            else
                cb.ConnectTimeout = 50;

            Assert.True(SqlDependency.Start(_startConnectionString), "Failed to start listener.");

            try
            {
                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => SqlDependency.Start(cb.ToString()));
            }
            finally
            {
                Assert.True(SqlDependency.Stop(_startConnectionString), "Failed to stop listener.");

                Assert.False(SqlDependency.Stop(cb.ToString()), "Expected failure when trying to completely stop listener.");
            }
        }

        [CheckConnStrSetupFact]
        public void Test_Start_DifferentDB()
        {
            SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder(_startConnectionString)
            {
                InitialCatalog = "tempdb"
            };
            string altDatabaseConnectionString = cb.ToString();

            Assert.True(SqlDependency.Start(_startConnectionString), "Failed to start listener.");

            Assert.True(SqlDependency.Start(altDatabaseConnectionString), "Failed to start listener.");

            Assert.True(SqlDependency.Stop(_startConnectionString), "Failed to stop listener.");

            Assert.True(SqlDependency.Stop(altDatabaseConnectionString), "Failed to stop listener.");
        }
        #endregion

        #region SqlDependency_Tests

        [CheckConnStrSetupFact]
        public void Test_SingleDependency_NoStart()
        {
            using (SqlConnection conn = new SqlConnection(_execConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT a, b, c FROM " + _tableName, conn))
            {
                conn.Open();

                SqlDependency dep = new SqlDependency(cmd);
                dep.OnChange += delegate (object o, SqlNotificationEventArgs args)
                {
                    Console.WriteLine("4 Notification callback. Type={0}, Info={1}, Source={2}", args.Type, args.Info, args.Source);
                };

                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => cmd.ExecuteReader());
            }
        }

        [CheckConnStrSetupFact]
        public void Test_SingleDependency_Stopped()
        {
            SqlDependency.Start(_startConnectionString);
            SqlDependency.Stop(_startConnectionString);

            using (SqlConnection conn = new SqlConnection(_execConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT a, b, c FROM " + _tableName, conn))
            {
                conn.Open();

                SqlDependency dep = new SqlDependency(cmd);
                dep.OnChange += delegate (object o, SqlNotificationEventArgs args)
                {
                    // Delegate won't be called, since notifications were stoppped
                    Console.WriteLine("5 Notification callback. Type={0}, Info={1}, Source={2}", args.Type, args.Info, args.Source);
                };

                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => cmd.ExecuteReader());
            }
        }

        [CheckConnStrSetupFact]
        public void Test_SingleDependency_AllDefaults_SqlAuth()
        {
            Assert.True(SqlDependency.Start(_startConnectionString), "Failed to start listener.");

            try
            {
                // create a new event every time to avoid mixing notification callbacks
                ManualResetEvent notificationReceived = new ManualResetEvent(false);
                ManualResetEvent updateCompleted = new ManualResetEvent(false);

                using (SqlConnection conn = new SqlConnection(_execConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT a, b, c FROM " + _tableName, conn))
                {
                    conn.Open();

                    SqlDependency dep = new SqlDependency(cmd);
                    dep.OnChange += delegate (object o, SqlNotificationEventArgs arg)
                    {
                        Assert.True(updateCompleted.WaitOne(CALLBACK_TIMEOUT, false), "Received notification, but update did not complete.");

                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationType.Change, arg.Type, "Unexpected Type value.");
                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationInfo.Update, arg.Info, "Unexpected Info value.");
                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationSource.Data, arg.Source, "Unexpected Source value.");

                        notificationReceived.Set();
                    };

                    cmd.ExecuteReader();
                }

                int count = RunSQL("UPDATE " + _tableName + " SET c=" + Environment.TickCount);
                DataTestUtility.AssertEqualsWithDescription(1, count, "Unexpected count value.");

                updateCompleted.Set();

                Assert.True(notificationReceived.WaitOne(CALLBACK_TIMEOUT, false), "Notification not received within the timeout period");
            }
            finally
            {
                Assert.True(SqlDependency.Stop(_startConnectionString), "Failed to stop listener.");
            }
        }

        [CheckConnStrSetupFact]
        public void Test_SingleDependency_CustomQueue_SqlAuth()
        {
            Assert.True(SqlDependency.Start(_startConnectionString, _queueName), "Failed to start listener.");

            try
            {
                // create a new event every time to avoid mixing notification callbacks
                ManualResetEvent notificationReceived = new ManualResetEvent(false);
                ManualResetEvent updateCompleted = new ManualResetEvent(false);

                using (SqlConnection conn = new SqlConnection(_execConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT a, b, c FROM " + _tableName, conn))
                {
                    conn.Open();

                    SqlDependency dep = new SqlDependency(cmd, "service=" + _serviceName + ";local database=msdb", 0);
                    dep.OnChange += delegate (object o, SqlNotificationEventArgs args)
                    {
                        Assert.True(updateCompleted.WaitOne(CALLBACK_TIMEOUT, false), "Received notification, but update did not complete.");

                        Console.WriteLine("7 Notification callback. Type={0}, Info={1}, Source={2}", args.Type, args.Info, args.Source);
                        notificationReceived.Set();
                    };

                    cmd.ExecuteReader();
                }

                int count = RunSQL("UPDATE " + _tableName + " SET c=" + Environment.TickCount);
                DataTestUtility.AssertEqualsWithDescription(1, count, "Unexpected count value.");

                updateCompleted.Set();

                Assert.False(notificationReceived.WaitOne(CALLBACK_TIMEOUT, false), "Notification should not be received.");
            }
            finally
            {
                Assert.True(SqlDependency.Stop(_startConnectionString, _queueName), "Failed to stop listener.");
            }
        }

        /// <summary>
        /// SqlDependecy premature timeout
        /// </summary>
        [CheckConnStrSetupFact]
        public void Test_SingleDependency_Timeout()
        {
            Assert.True(SqlDependency.Start(_startConnectionString), "Failed to start listener.");

            try
            {
                // with resolution of 15 seconds, SqlDependency should fire timeout notification only after 45 seconds, leave 5 seconds gap from both sides.
                const int SqlDependencyTimerResolution = 15; // seconds
                const int testTimeSeconds = SqlDependencyTimerResolution * 3 - 5;
                const int minTimeoutEventInterval = testTimeSeconds - 1;
                const int maxTimeoutEventInterval = testTimeSeconds + SqlDependencyTimerResolution + 1;

                // create a new event every time to avoid mixing notification callbacks
                ManualResetEvent notificationReceived = new ManualResetEvent(false);
                DateTime startUtcTime;

                using (SqlConnection conn = new SqlConnection(_execConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT a, b, c FROM " + _tableName, conn))
                {
                    conn.Open();

                    // create SqlDependency with timeout
                    SqlDependency dep = new SqlDependency(cmd, null, testTimeSeconds);
                    dep.OnChange += delegate (object o, SqlNotificationEventArgs arg)
                    {
                        // notification of Timeout can arrive either from server or from client timer. Handle both situations here:
                        SqlNotificationInfo info = arg.Info;
                        if (info == SqlNotificationInfo.Unknown)
                        {
                            // server timed out before the client, replace it with Error to produce consistent output for trun
                            info = SqlNotificationInfo.Error;
                        }

                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationType.Change, arg.Type, "Unexpected Type value.");
                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationInfo.Error, arg.Info, "Unexpected Info value.");
                        DataTestUtility.AssertEqualsWithDescription(SqlNotificationSource.Timeout, arg.Source, "Unexpected Source value.");
                        notificationReceived.Set();
                    };

                    cmd.ExecuteReader();
                    startUtcTime = DateTime.UtcNow;
                }

                Assert.True(
                    notificationReceived.WaitOne(TimeSpan.FromSeconds(maxTimeoutEventInterval), false),
                    string.Format("Notification not received within the maximum timeout period of {0} seconds", maxTimeoutEventInterval));

                // notification received in time, check that it is not too early
                TimeSpan notificationTime = DateTime.UtcNow - startUtcTime;
                Assert.True(
                    notificationTime >= TimeSpan.FromSeconds(minTimeoutEventInterval),
                    string.Format(
                        "Notification was not expected before {0} seconds: received after {1} seconds",
                        minTimeoutEventInterval, notificationTime.TotalSeconds));
            }
            finally
            {
                Assert.True(SqlDependency.Stop(_startConnectionString), "Failed to stop listener.");
            }
        }

        #endregion

        #region Utility_Methods
        private static string[] CreateSqlSetupStatements(string tableName, string queueName, string serviceName)
        {
            return new string[] {
                string.Format("CREATE TABLE {0}(a INT NOT NULL, b NVARCHAR(10), c INT NOT NULL)", tableName),
                string.Format("INSERT INTO {0} (a, b, c) VALUES (1, 'foo', 0)", tableName),
                string.Format("CREATE QUEUE {0}", queueName),
                string.Format("CREATE SERVICE [{0}] ON QUEUE {1} ([http://schemas.microsoft.com/SQL/Notifications/PostQueryNotification])", serviceName, queueName)
            };
        }

        private static string[] CreateSqlCleanupStatements(string tableName, string queueName, string serviceName)
        {
            return new string[] {
                string.Format("DROP TABLE {0}", tableName),
                string.Format("DROP SERVICE [{0}]", serviceName),
                string.Format("DROP QUEUE {0}", queueName)
            };
        }

        private void Setup()
        {
            RunSQL(CreateSqlSetupStatements(_tableName, _schemaQueue, _serviceName));
        }

        private void Cleanup()
        {
            RunSQL(CreateSqlCleanupStatements(_tableName, _schemaQueue, _serviceName));
        }

        private int RunSQL(params string[] stmts)
        {
            int count = -1;
            using (SqlConnection conn = new SqlConnection(_execConnectionString))
            {
                conn.Open();

                SqlCommand cmd = conn.CreateCommand();

                foreach (string stmt in stmts)
                {
                    cmd.CommandText = stmt;
                    int tmp = cmd.ExecuteNonQuery();
                    count = ((0 <= tmp) ? ((0 <= count) ? count + tmp : tmp) : count);
                }
            }
            return count;
        }

        #endregion
    }
}
