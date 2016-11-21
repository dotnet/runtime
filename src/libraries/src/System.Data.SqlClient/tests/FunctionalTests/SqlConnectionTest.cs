// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;
using Microsoft.SqlServer.TDS;
using Microsoft.SqlServer.TDS.Servers;
using Microsoft.SqlServer.TDS.EndPoint;
using System.Net;

namespace System.Data.SqlClient.Tests
{
    public class SqlConnectionBasicTests
    {

        [Fact]
        public void ConnectionTest()
        {
            Exception e = null;

            using (TestTdsServer server = TestTdsServer.StartTestServer())
            {
                try
                {
                    SqlConnection connection = new SqlConnection(server.ConnectionString);
                    connection.Open();
                }
                catch (Exception ce)
                {
                    e = ce;
                }
            }
            Assert.Null(e);
        }

    }

    internal class TestTdsServer : GenericTDSServer, IDisposable
    {
        private TDSServerEndPoint _endpoint = null;

        private SqlConnectionStringBuilder connectionStringBuilder;

        public TestTdsServer(TDSServerArguments args) : base(args) { }

        public static TestTdsServer StartTestServer(bool enableFedAuth = false)
        {
            TDSServerArguments args = new TDSServerArguments()
            {
                Log = Console.Out,
            };

            if (enableFedAuth)
            {
                args.FedAuthRequiredPreLoginOption = Microsoft.SqlServer.TDS.PreLogin.TdsPreLoginFedAuthRequiredOption.FedAuthRequired;
            }

            TestTdsServer server = new TestTdsServer(args);
            server._endpoint = new TDSServerEndPoint(server) { ServerEndPoint = new IPEndPoint(IPAddress.Any, 12345) };
            server._endpoint.Start();

            server.connectionStringBuilder = new SqlConnectionStringBuilder() { DataSource = "localhost,12345", ConnectTimeout = 30, Encrypt = false };
            server.ConnectionString = server.connectionStringBuilder.ConnectionString;
            return server;
        }

        public void Dispose() => _endpoint?.Stop();

        public string ConnectionString { get; private set; }

    }
}
