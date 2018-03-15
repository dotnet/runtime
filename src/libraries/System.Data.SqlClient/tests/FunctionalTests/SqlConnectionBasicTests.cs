// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlConnectionBasicTests
    {
        [Fact]
        public void ConnectionTest()
        {
            using (TestTdsServer server = TestTdsServer.StartTestServer())
            {
                using (SqlConnection connection = new SqlConnection(server.ConnectionString))
                {
                    connection.Open();
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotArmProcess))] 
        [PlatformSpecific(TestPlatforms.Windows)]
        public void IntegratedAuthConnectionTest()
        {
            using (TestTdsServer server = TestTdsServer.StartTestServer())
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(server.ConnectionString);
                builder.IntegratedSecurity = true;
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                }
            }
        }

        [Fact]
        public void SqlConnectionDbProviderFactoryTest()
        {
            SqlConnection con = new SqlConnection();
            PropertyInfo dbProviderFactoryProperty = con.GetType().GetProperty("DbProviderFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(dbProviderFactoryProperty);
            DbProviderFactory factory = dbProviderFactoryProperty.GetValue(con) as DbProviderFactory;
            Assert.NotNull(factory);
            Assert.Same(typeof(SqlClientFactory), factory.GetType());
            Assert.Same(SqlClientFactory.Instance, factory);
        }

        [Fact]
        public void SqlConnectionValidParameters()
        {
            var con = new SqlConnection("Timeout=1234;packet Size=5678 ;;; ;");
            Assert.Equal(1234, con.ConnectionTimeout);
            Assert.Equal(5678, con.PacketSize);
        }

        [Fact]
        public void SqlConnectionEmptyParameters()
        {
            var con = new SqlConnection("Timeout=;packet Size= ;Integrated Security=;");
            //default values are defined in internal class DbConnectionStringDefaults
            Assert.Equal(15, con.ConnectionTimeout);
            Assert.Equal(8000, con.PacketSize);
            Assert.False(new SqlConnectionStringBuilder(con.ConnectionString).IntegratedSecurity);
        }

        [Fact]
        public void SqlConnectionInvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => new SqlConnection("Timeout=null;"));
            Assert.Throws<ArgumentException>(() => new SqlConnection("Timeout= null;"));
            Assert.Throws<ArgumentException>(() => new SqlConnection("Timeout=1 1;"));
            Assert.Throws<ArgumentException>(() => new SqlConnection("Timeout=1a;"));
            Assert.Throws<ArgumentException>(() => new SqlConnection("Integrated Security=truee"));
        }

        [Fact]
        public void ClosedConnectionSchemaRetrieval()
        {
            using (SqlConnection connection = new SqlConnection(string.Empty))
            {
                Assert.Throws<InvalidOperationException>(() => connection.GetSchema());
            }
        }

        [Theory]
        [InlineData("RandomStringForTargetServer", false, true)]
        [InlineData("RandomStringForTargetServer", true, false)]
        [InlineData(null, false, false)]
        [InlineData("", false, false)]
        public void RetrieveWorkstationId(string workstation, bool withDispose, bool shouldMatchSetWorkstationId)
        {
            string connectionString = $"Workstation Id={workstation}";
            SqlConnection conn = new SqlConnection(connectionString);
            if(withDispose)
            {
                conn.Dispose();
            }
            string expected = shouldMatchSetWorkstationId ? workstation : Environment.MachineName;
            Assert.Equal(expected, conn.WorkstationId);
        }

        [Fact]
        public void ConnectionTimeoutTestWithThread()
        {
            int timeoutSec = 5;
            string connStrNotAvailable = $"Server=tcp:fakeServer,1433;uid=fakeuser;pwd=fakepwd;Connection Timeout={timeoutSec}";

            List<ConnectionWorker> list = new List<ConnectionWorker>();
            for (int i = 0; i < 10; ++i)
            {
                list.Add(new ConnectionWorker(connStrNotAvailable));
            }

            ConnectionWorker.Start();
            ConnectionWorker.Stop();

            double theMax = 0;
            foreach (ConnectionWorker w in list)
            {
                if (theMax < w.MaxTimeElapsed)
                {
                    theMax = w.MaxTimeElapsed;
                }
            }

            int threshold = (timeoutSec + 1) * 1000;

            Console.WriteLine($"ConnectionTimeoutTestWithThread: Elapsed Time {theMax} and threshold {threshold}");
        }

        [OuterLoop("Can take up to 4 seconds")]
        [Fact]
        public void ExceptionsWithMinPoolSizeCanBeHandled()
        {
            string connectionString = $"Data Source={Guid.NewGuid().ToString()};uid=random;pwd=asd;Connect Timeout=2; Min Pool Size=3";
            for (int i = 0; i < 2; i++)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    Exception exception = Record.Exception(() => connection.Open());
                    Assert.True(exception is InvalidOperationException || exception is SqlException, $"Unexpected exception: {exception}");
                }
            }
        }

        [Fact]
        public void ConnectionTestInvalidCredentialCombination()
        {
            var cleartextCredsConnStr = "User=test;Password=test;";
            var sspiConnStr = "Integrated Security=true;";
            var testPassword = new SecureString();
            testPassword.MakeReadOnly();
            var sqlCredential = new SqlCredential(string.Empty, testPassword);

            // Verify that SSPI and cleartext username/password are not in the connection string.
            Assert.Throws<ArgumentException>(() => { new SqlConnection(cleartextCredsConnStr, sqlCredential); });

            Assert.Throws<ArgumentException>(() => { new SqlConnection(sspiConnStr, sqlCredential); });

            // Verify that credential may not be set with cleartext username/password or SSPI.
            using (var conn = new SqlConnection(cleartextCredsConnStr))
            {
                Assert.Throws<InvalidOperationException>(() => { conn.Credential = sqlCredential; });
            }

            using (var conn = new SqlConnection(sspiConnStr))
            {
                Assert.Throws<InvalidOperationException>(() => { conn.Credential = sqlCredential; });
            }

            // Verify that connection string with username/password or SSPI may not be set with credential present.
            using (var conn = new SqlConnection(string.Empty, sqlCredential))
            {
                Assert.Throws<InvalidOperationException>(() => { conn.ConnectionString = cleartextCredsConnStr; });

                Assert.Throws<InvalidOperationException>(() => { conn.ConnectionString = sspiConnStr; });
            }
        }

        [Fact]
        public void ConnectionTestValidCredentialCombination()
        {
            var testPassword = new SecureString();
            testPassword.MakeReadOnly();
            var sqlCredential = new SqlCredential(string.Empty, testPassword);
            var conn = new SqlConnection(string.Empty, sqlCredential);

            Assert.Equal(sqlCredential, conn.Credential);
        }

        public class ConnectionWorker
        {
            private static ManualResetEventSlim startEvent = new ManualResetEventSlim(false);
            private static List<ConnectionWorker> workerList = new List<ConnectionWorker>();
            private ManualResetEventSlim doneEvent = new ManualResetEventSlim(false);
            private double maxTimeElapsed;
            private Thread thread;
            private string connectionString;

            public ConnectionWorker(string connectionString)
            {
                workerList.Add(this);
                this.connectionString = connectionString;
                thread = new Thread(new ThreadStart(SqlConnectionOpen));
                thread.Start();
            }

            public double MaxTimeElapsed
            {
                get
                {
                    return maxTimeElapsed;
                }
            }

            public static void Start()
            {
                startEvent.Set();
            }

            public static void Stop()
            {
                foreach (ConnectionWorker w in workerList)
                {
                    w.doneEvent.Wait();
                }
            }

            public void SqlConnectionOpen()
            {
                startEvent.Wait();

                Stopwatch sw = new Stopwatch();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    sw.Start();
                    try
                    {
                        con.Open();
                    }
                    catch { }
                    sw.Stop();
                }

                double elapsed = sw.Elapsed.TotalMilliseconds;
                if (maxTimeElapsed < elapsed)
                {
                    maxTimeElapsed = elapsed;
                }

                doneEvent.Set();
            }
        }
    }
}
