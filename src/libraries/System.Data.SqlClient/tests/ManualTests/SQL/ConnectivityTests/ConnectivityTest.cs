// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ConnectivityParametersTest
    {
        private const string COL_PROGRAM_NAME = "ProgramName";
        private const string COL_HOSTNAME = "HostName";
        private static readonly string s_databaseName = "d_" + Guid.NewGuid().ToString().Replace('-', '_');
        private static readonly string s_tableName = "Person";
        private static readonly string s_connectionString = DataTestUtility.TcpConnStr;
        private static readonly string s_dbConnectionString = new SqlConnectionStringBuilder(s_connectionString) { InitialCatalog = s_databaseName }.ConnectionString;
        private static readonly string s_createDatabaseCmd = $"CREATE DATABASE {s_databaseName}";
        private static readonly string s_createTableCmd = $"CREATE TABLE {s_tableName} (NAME NVARCHAR(40), AGE INT)";
        private static readonly string s_alterDatabaseSingleCmd = $"ALTER DATABASE {s_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
        private static readonly string s_alterDatabaseMultiCmd = $"ALTER DATABASE {s_databaseName} SET MULTI_USER WITH ROLLBACK IMMEDIATE;";
        private static readonly string s_selectTableCmd = $"SELECT COUNT(*) FROM {s_tableName}";
        private static readonly string s_dropDatabaseCmd = $"DROP DATABASE {s_databaseName}";

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void EnvironmentHostNameTest()
        {
            SqlConnectionStringBuilder builder = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { Pooling = true });
            builder.ApplicationName = "HostNameTest";

            using (SqlConnection sqlConnection = new SqlConnection(builder.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand("sp_who2", sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int programNameOrdinal = reader.GetOrdinal(COL_PROGRAM_NAME);
                            string programName = reader.GetString(programNameOrdinal);
                            
                            if (programName != null && programName.Trim().Equals(builder.ApplicationName))
                            {
                                // Get the hostname
                                int hostnameOrdinal = reader.GetOrdinal(COL_HOSTNAME);
                                string hostnameFromServer = reader.GetString(hostnameOrdinal);
                                string expectedMachineName = Environment.MachineName.ToUpper();
                                string hostNameFromServer = hostnameFromServer.Trim().ToUpper();
                                Assert.Matches(expectedMachineName, hostNameFromServer);
                                return;
                            }
                        }
                    }
                }
            }
            Assert.True(false, "No non-empty hostname found for the application");
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void ConnectionTimeoutTestWithThread()
        {
            const int timeoutSec = 5;
            const int numOfTry = 2;
            const int numOfThreads = 5;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            builder.DataSource = "invalidhost";
            builder.ConnectTimeout = timeoutSec;
            string connStrNotAvailable = builder.ConnectionString;

            for (int i = 0; i < numOfThreads; ++i)
            {
                new ConnectionWorker(connStrNotAvailable, numOfTry);
            }

            ConnectionWorker.Start();
            ConnectionWorker.Stop();

            double timeTotal = 0;
            double timeElapsed = 0;

            foreach (ConnectionWorker w in ConnectionWorker.WorkerList)
            {
                timeTotal += w.TimeElapsed;
            }
            timeElapsed = timeTotal / Convert.ToDouble(ConnectionWorker.WorkerList.Count);

            int threshold = timeoutSec * numOfTry * 2 * 1000;

            Assert.True(timeElapsed < threshold);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void ProcessIdTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            string sqlProviderName = builder.ApplicationName;
            string sqlProviderProcessID = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

            using (SqlConnection sqlConnection = new SqlConnection(builder.ConnectionString))
            {
                sqlConnection.Open();
                string strCommand = $"SELECT PROGRAM_NAME,HOSTPROCESS FROM SYS.SYSPROCESSES WHERE PROGRAM_NAME LIKE ('%{sqlProviderName}%')";
                using (SqlCommand command = new SqlCommand(strCommand, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.Equal(sqlProviderName,reader.GetString(0).Trim());
                            Assert.Equal(sqlProviderProcessID, reader.GetString(1).Trim());
                        }
                    }
                }
            }
        }
        public class ConnectionWorker
        {
            private static List<ConnectionWorker> workerList = new List<ConnectionWorker>();
            private ManualResetEventSlim _doneEvent = new ManualResetEventSlim(false);
            private double _timeElapsed;
            private Thread _thread;
            private string _connectionString;
            private int _numOfTry;

            public ConnectionWorker(string connectionString, int numOfTry)
            {
                workerList.Add(this);
                _connectionString = connectionString;
                _numOfTry = numOfTry;
                _thread = new Thread(new ThreadStart(SqlConnectionOpen));
            }

            public static List<ConnectionWorker> WorkerList => workerList;

            public double TimeElapsed => _timeElapsed;

            public static void Start()
            {
                foreach (ConnectionWorker w in workerList)
                {
                    w._thread.Start();
                }
            }

            public static void Stop()
            {
                foreach (ConnectionWorker w in workerList)
                {
                    w._doneEvent.Wait();
                }
            }

            public void SqlConnectionOpen()
            {
                Stopwatch sw = new Stopwatch();
                double totalTime = 0;
                for (int i = 0; i < _numOfTry; ++i)
                {
                    using (SqlConnection con = new SqlConnection(_connectionString))
                    {
                        sw.Start();
                        try
                        {
                            con.Open();
                        }
                        catch { }
                        sw.Stop();
                    }
                    totalTime += sw.Elapsed.TotalMilliseconds;
                    sw.Reset();
                }

                _timeElapsed = totalTime / Convert.ToDouble(_numOfTry);

                _doneEvent.Set();
            }
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void ConnectionKilledTest()
        {
            try
            {
                // Setup Database and Table.
                DataTestUtility.RunNonQuery(s_connectionString, s_createDatabaseCmd);
                DataTestUtility.RunNonQuery(s_dbConnectionString, s_createTableCmd);

                // Kill all the connections and set Database to SINGLE_USER Mode.
                DataTestUtility.RunNonQuery(s_connectionString, s_alterDatabaseSingleCmd);
                // Set Database back to MULTI_USER Mode
                DataTestUtility.RunNonQuery(s_connectionString, s_alterDatabaseMultiCmd);

                // Execute SELECT statement.
                DataTestUtility.RunNonQuery(s_dbConnectionString, s_selectTableCmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Assert.Null(ex);
            }
            finally
            {
                // Kill all the connections, set Database to SINGLE_USER Mode and drop Database
                DataTestUtility.RunNonQuery(s_connectionString, s_alterDatabaseSingleCmd);
                DataTestUtility.RunNonQuery(s_connectionString, s_dropDatabaseCmd);
            }
        }
    }
}
