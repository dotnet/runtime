// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ConnectionOnMirroringTest
    {
        private static ManualResetEvent workerCompletedEvent = new ManualResetEvent(false);

        [CheckConnStrSetupFact]
        public static void TestMultipleConnectionToMirroredServer()
        {
            string mirroringStateDesc;
            string failoverPartnerName;
            bool isMirroring = GetMirroringInfo(DataTestUtility.TcpConnStr, out mirroringStateDesc, out failoverPartnerName);
            bool isSynchronized = "SYNCHRONIZED".Equals(mirroringStateDesc, StringComparison.InvariantCultureIgnoreCase);
            if (isMirroring && isSynchronized && !string.IsNullOrEmpty(failoverPartnerName))
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
                builder.ConnectTimeout = 0;

                TestWorker worker = new TestWorker(builder.ConnectionString);
                Thread childThread = new Thread(() => worker.TestMultipleConnection());
                childThread.Start();

                if (workerCompletedEvent.WaitOne(10000))
                {
                    childThread.Join();
                }
                else
                {
                    // currently Thread.Abort() throws PlatformNotSupportedException in CoreFx.
                    childThread.Interrupt();
                    throw new Exception("SqlConnection could not open and close successfully in timely manner. Possibly connection hangs.");
                }
            }
        }

        private static bool GetMirroringInfo(string connectionString, out string mirroringStateDesc, out string failoverPartnerName)
        {
            mirroringStateDesc = null;
            failoverPartnerName = null;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            string dbname = builder.InitialCatalog;

            builder.ConnectTimeout = 5;
            connectionString = builder.ConnectionString;

            DataTable dt = DataTestUtility.RunQuery(connectionString, $"select mirroring_state_desc from sys.database_mirroring where database_id = DB_ID('{dbname}')");
            mirroringStateDesc = dt.Rows[0][0].ToString();

            bool isMirroring = !string.IsNullOrEmpty(mirroringStateDesc);
            if (isMirroring)
            {
                dt = DataTestUtility.RunQuery(connectionString, $"select mirroring_partner_name from sys.database_mirroring where database_id = DB_ID('{dbname}')");
                failoverPartnerName = dt.Rows[0][0].ToString();
            }

            return isMirroring;
        }

        private class TestWorker
        {
            private string _connectionString;

            public TestWorker(string connectionString)
            {
                _connectionString = connectionString;
            }

            public void TestMultipleConnection()
            {
                List<SqlConnection> list = new List<SqlConnection>();

                for (int i = 0; i < 10; ++i)
                {
                    SqlConnection conn = new SqlConnection(_connectionString);
                    list.Add(conn);
                    conn.Open();
                }

                foreach (SqlConnection conn in list)
                {
                    conn.Dispose();
                }

                workerCompletedEvent.Set();
            }
        }
    }
}
