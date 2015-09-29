// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class ConnectExTest
    {
        private readonly ITestOutputHelper _log;

        public ConnectExTest(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        private static void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [Fact]
        [Trait("IPv4", "true")]
        [Trait("IPv6", "true")]
        public void ConnectEx_Success()
        {
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());

            int port;
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port);

            SocketTestServer server6 = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.IPv6Loopback, port));

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                args.Completed += OnConnectAsyncCompleted;

                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                Assert.True(sock.ConnectAsync(args));
                Assert.True(complete.WaitOne(5000), "IPv4: Timed out while waiting for connection");
                Assert.True(args.SocketError == SocketError.Success);

                sock.Dispose();

                sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                args.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, port);
                complete.Reset();

                Assert.True(sock.ConnectAsync(args));
                Assert.True(complete.WaitOne(5000), "IPv6: Timed out while waiting for connection");
                Assert.True(args.SocketError == SocketError.Success);
            }
            finally
            {
                sock.Dispose();

                server.Dispose();
                server6.Dispose();
            }
        }

        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets. 
        [Fact]
        public void TestFinalizers()
        {
            // Making several passes through the FReachable list.
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion 
    }
}
