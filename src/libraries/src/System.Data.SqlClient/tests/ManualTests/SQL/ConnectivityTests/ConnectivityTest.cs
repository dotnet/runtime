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

        [CheckConnStrSetupFact]
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

        [CheckConnStrSetupFact]
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
    }
}
