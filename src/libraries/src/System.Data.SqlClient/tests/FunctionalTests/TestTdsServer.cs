// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.Servers;
using System.Net;
using System.Runtime.CompilerServices;

namespace System.Data.SqlClient.Tests
{
    internal class TestTdsServer : GenericTDSServer, IDisposable
    {
        private TDSServerEndPoint _endpoint = null;

        private SqlConnectionStringBuilder connectionStringBuilder;

        public TestTdsServer(TDSServerArguments args) : base(args) { }

        public TestTdsServer(QueryEngine engine, TDSServerArguments args) : base(args)
        {
            this.Engine = engine;
        }

        public static TestTdsServer StartServerWithQueryEngine(QueryEngine engine, bool enableFedAuth = false, bool enableLog = false, [CallerMemberName] string methodName = "")
        {
            TDSServerArguments args = new TDSServerArguments()
            {
                Log = enableLog ? Console.Out : null,
            };

            if (enableFedAuth)
            {
                args.FedAuthRequiredPreLoginOption = Microsoft.SqlServer.TDS.PreLogin.TdsPreLoginFedAuthRequiredOption.FedAuthRequired;
            }

            TestTdsServer server = engine == null ? new TestTdsServer(args) : new TestTdsServer(engine, args);
            server._endpoint = new TDSServerEndPoint(server) { ServerEndPoint = new IPEndPoint(IPAddress.Any, 0) };
            server._endpoint.EndpointName = methodName;
            // The server EventLog should be enabled as it logs the exceptions.
            server._endpoint.EventLog = Console.Out;
            server._endpoint.Start();

            int port = server._endpoint.ServerEndPoint.Port;
            server.connectionStringBuilder = new SqlConnectionStringBuilder() { DataSource = "localhost," + port, ConnectTimeout = 5, Encrypt = false };
            server.ConnectionString = server.connectionStringBuilder.ConnectionString;
            return server;
        }

        public static TestTdsServer StartTestServer(bool enableFedAuth = false, bool enableLog = false, [CallerMemberName] string methodName = "")
        {
            return StartServerWithQueryEngine(null, false, false, methodName);
        }

        public void Dispose() => _endpoint?.Stop();

        public string ConnectionString { get; private set; }

    }
}
