// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;
using System.Runtime.ExceptionServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class ConnectionPoolTest
    {
        private readonly string _nwnd9Tcp = null;
        private readonly string _nwnd9TcpMars = null;
        private readonly string _nwnd9Np = null;
        private readonly string _nwnd9NpMars = null;
        private readonly string _nwnd10Tcp = null;
        private readonly string _nwnd10TcpMars = null;
        private readonly string _nwnd10Np = null;
        private readonly string _nwnd10NpMars = null;

        public ConnectionPoolTest()
        {
            PrepareConnectionStrings(DataTestClass.SQL2005_Northwind, out _nwnd9Tcp, out _nwnd9TcpMars, out _nwnd9Np, out _nwnd9NpMars);
            PrepareConnectionStrings(DataTestClass.SQL2008_Northwind, out _nwnd10Tcp, out _nwnd10TcpMars, out _nwnd10Np, out _nwnd10NpMars);
        }

        [Fact]
        public void ConnectionPool_Nwnd9()
        {
            RunDataTestForSingleConnString(_nwnd9Tcp, _nwnd9Np, false);
        }

        [Fact]
        public void ConnectionPool_Nwnd9Mars()
        {
            RunDataTestForSingleConnString(_nwnd9TcpMars, _nwnd9NpMars, false);
        }

        [Fact]
        public void ConnectionPool_Nwnd10()
        {
            RunDataTestForSingleConnString(_nwnd10Tcp, _nwnd10Np, true);
        }

        [Fact]
        public void ConnectionPool_Nwnd10Mars()
        {
            RunDataTestForSingleConnString(_nwnd10TcpMars, _nwnd10NpMars, true);
        }

        private static void RunDataTestForSingleConnString(string tcpConnectionString, string npConnectionString, bool serverIsKatmaiOrLater)
        {
            BasicConnectionPoolingTest(tcpConnectionString);
            ClearAllPoolsTest(tcpConnectionString);
#if MANAGED_SNI && DEBUG
            KillConnectionTest(tcpConnectionString);
#endif
            ReclaimEmancipatedOnOpenTest(tcpConnectionString);
        }

        /// <summary>
        /// Tests that using the same connection string results in the same pool\internal connection and a different string results in a different pool\internal connection
        /// </summary>
        /// <param name="connectionString"></param>
        private static void BasicConnectionPoolingTest(string connectionString)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            InternalConnectionWrapper internalConnection = new InternalConnectionWrapper(connection);
            ConnectionPoolWrapper connectionPool = new ConnectionPoolWrapper(connection);
            connection.Close();

            SqlConnection connection2 = new SqlConnection(connectionString);
            connection2.Open();
            Assert.True(internalConnection.IsInternalConnectionOf(connection2), "New connection does not use same internal connection");
            Assert.True(connectionPool.ContainsConnection(connection2), "New connection is in a different pool");
            connection2.Close();

            SqlConnection connection3 = new SqlConnection(connectionString + ";App=SqlConnectionPoolUnitTest;");
            connection3.Open();
            Assert.False(internalConnection.IsInternalConnectionOf(connection3), "Connection with different connection string uses same internal connection");
            Assert.False(connectionPool.ContainsConnection(connection3), "Connection with different connection string uses same connection pool");
            connection3.Close();

            connectionPool.Cleanup();
            SqlConnection connection4 = new SqlConnection(connectionString);

            connection4.Open();
            Assert.True(internalConnection.IsInternalConnectionOf(connection4), "New connection does not use same internal connection");
            Assert.True(connectionPool.ContainsConnection(connection4), "New connection is in a different pool");
            connection4.Close();
        }

#if MANAGED_SNI && DEBUG
        /// <summary>
        /// Tests if killing the connection using the InternalConnectionWrapper is working
        /// </summary>
        /// <param name="connectionString"></param>
        private static void KillConnectionTest(string connectionString)
        {
            InternalConnectionWrapper wrapper = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                wrapper = new InternalConnectionWrapper(connection);

                using (SqlCommand command = new SqlCommand("SELECT 5;", connection))
                {
                    DataTestClass.AssertEqualsWithDescription(5, command.ExecuteScalar(), "Incorrect scalar result.");
                }

                wrapper.KillConnection();
            }

            using (SqlConnection connection2 = new SqlConnection(connectionString))
            {
                connection2.Open();
                Assert.False(wrapper.IsInternalConnectionOf(connection2), "New connection has internal connection that was just killed");
                using (SqlCommand command = new SqlCommand("SELECT 5;", connection2))
                {
                    DataTestClass.AssertEqualsWithDescription(5, command.ExecuteScalar(), "Incorrect scalar result.");
                }
            }
        }
#endif

        /// <summary>
        /// Tests if clearing all of the pools does actually remove the pools
        /// </summary>
        /// <param name="connectionString"></param>
        private static void ClearAllPoolsTest(string connectionString)
        {
            SqlConnection.ClearAllPools();
            Assert.True(0 == ConnectionPoolWrapper.AllConnectionPools().Length, "Pools exist after clearing all pools");

            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            ConnectionPoolWrapper pool = new ConnectionPoolWrapper(connection);
            connection.Close();
            ConnectionPoolWrapper[] allPools = ConnectionPoolWrapper.AllConnectionPools();
            DataTestClass.AssertEqualsWithDescription(1, allPools.Length, "Incorrect number of pools exist.");
            Assert.True(allPools[0].Equals(pool), "Saved pool is not in the list of all pools");
            DataTestClass.AssertEqualsWithDescription(1, pool.ConnectionCount, "Saved pool has incorrect number of connections");

            SqlConnection.ClearAllPools();
            Assert.True(0 == ConnectionPoolWrapper.AllConnectionPools().Length, "Pools exist after clearing all pools");
            DataTestClass.AssertEqualsWithDescription(0, pool.ConnectionCount, "Saved pool has incorrect number of connections.");
        }

        /// <summary>
        /// Checks if an 'emancipated' internal connection is reclaimed when a new connection is opened AND we hit max pool size
        /// NOTE: 'emancipated' means that the internal connection's SqlConnection has fallen out of scope and has no references, but was not explicitly disposed\closed
        /// </summary>
        /// <param name="connectionString"></param>
        private static void ReclaimEmancipatedOnOpenTest(string connectionString)
        {
            string newConnectionString = connectionString + ";Max Pool Size=1";
            SqlConnection.ClearAllPools();

            InternalConnectionWrapper internalConnection = CreateEmancipatedConnection(newConnectionString);
            ConnectionPoolWrapper connectionPool = internalConnection.ConnectionPool;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            DataTestClass.AssertEqualsWithDescription(1, connectionPool.ConnectionCount, "Wrong number of connections in the pool.");
            DataTestClass.AssertEqualsWithDescription(0, connectionPool.FreeConnectionCount, "Wrong number of free connections in the pool.");

            using (SqlConnection connection = new SqlConnection(newConnectionString))
            {
                connection.Open();
                Assert.True(internalConnection.IsInternalConnectionOf(connection), "Connection has wrong internal connection");
                Assert.True(connectionPool.ContainsConnection(connection), "Connection is in wrong connection pool");
            }
        }

        private static void ReplacementConnectionUsesSemaphoreTest(string connectionString)
        {
            string newConnectionString = (new SqlConnectionStringBuilder(connectionString) { MaxPoolSize = 2, ConnectTimeout = 5 }).ConnectionString;
            SqlConnection.ClearAllPools();

            SqlConnection liveConnection = new SqlConnection(newConnectionString);
            SqlConnection deadConnection = new SqlConnection(newConnectionString);
            liveConnection.Open();
            deadConnection.Open();
            InternalConnectionWrapper deadConnectionInternal = new InternalConnectionWrapper(deadConnection);
            InternalConnectionWrapper liveConnectionInternal = new InternalConnectionWrapper(liveConnection);
            deadConnectionInternal.KillConnection();
            deadConnection.Close();
            liveConnection.Close();

            Task<InternalConnectionWrapper>[] tasks = new Task<InternalConnectionWrapper>[3];
            Barrier syncBarrier = new Barrier(tasks.Length);
            Func<InternalConnectionWrapper> taskFunction = (() => ReplacementConnectionUsesSemaphoreTask(newConnectionString, syncBarrier));
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew<InternalConnectionWrapper>(taskFunction);
            }


            bool taskWithLiveConnection = false;
            bool taskWithNewConnection = false;
            bool taskWithCorrectException = false;

            Task waitAllTask = Task.Factory.ContinueWhenAll(tasks, (completedTasks) =>
            {
                foreach (var item in completedTasks)
                {
                    if (item.Status == TaskStatus.Faulted)
                    {
                        // One task should have a timeout exception
                        if ((!taskWithCorrectException) && (item.Exception.InnerException is InvalidOperationException) && (item.Exception.InnerException.Message.StartsWith(SystemDataResourceManager.Instance.ADP_PooledOpenTimeout)))
                            taskWithCorrectException = true;
                        else if (!taskWithCorrectException)
                        {
                            // Rethrow the unknown exception
                            ExceptionDispatchInfo exceptionInfo = ExceptionDispatchInfo.Capture(item.Exception);
                            exceptionInfo.Throw();
                        }
                    }
                    else if (item.Status == TaskStatus.RanToCompletion)
                    {
                        // One task should get the live connection
                        if (item.Result.Equals(liveConnectionInternal))
                        {
                            if (!taskWithLiveConnection)
                                taskWithLiveConnection = true;
                        }
                        else if (!item.Result.Equals(deadConnectionInternal) && !taskWithNewConnection)
                            taskWithNewConnection = true;
                    }
                    else
                        Console.WriteLine("ERROR: Task in unknown state: {0}", item.Status);
                }
            });

            waitAllTask.Wait();
            Assert.True(taskWithLiveConnection && taskWithNewConnection && taskWithCorrectException, string.Format("Tasks didn't finish as expected.\nTask with live connection: {0}\nTask with new connection: {1}\nTask with correct exception: {2}\n", taskWithLiveConnection, taskWithNewConnection, taskWithCorrectException));
        }

        private static InternalConnectionWrapper ReplacementConnectionUsesSemaphoreTask(string connectionString, Barrier syncBarrier)
        {
            InternalConnectionWrapper internalConnection = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    internalConnection = new InternalConnectionWrapper(connection);
                }
                catch
                {
                    syncBarrier.SignalAndWait();
                    throw;
                }

                syncBarrier.SignalAndWait();
            }

            return internalConnection;
        }

        private static InternalConnectionWrapper CreateEmancipatedConnection(string connectionString)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return new InternalConnectionWrapper(connection);
        }

        private static void PrepareConnectionStrings(string originalString, out string tcpString, out string tcpMarsString, out string npString, out string npMarsString)
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder(originalString);
            DataSourceBuilder sourceBuilder = new DataSourceBuilder(connBuilder.DataSource);
            sourceBuilder.Protocol = null;

            // TCP
            connBuilder.DataSource = sourceBuilder.ToString();
            connBuilder.MultipleActiveResultSets = false;
            tcpString = connBuilder.ConnectionString;

            // TCP + MARS
            connBuilder.MultipleActiveResultSets = true;
            tcpMarsString = connBuilder.ConnectionString;

            // Named Pipes
            sourceBuilder.Port = null;
            connBuilder.DataSource = "np:" + sourceBuilder.ToString();
            connBuilder.MultipleActiveResultSets = false;
            npString = connBuilder.ConnectionString;

            // Named Pipes + MARS
            connBuilder.MultipleActiveResultSets = true;
            npMarsString = connBuilder.ConnectionString;
        }
    }
}
