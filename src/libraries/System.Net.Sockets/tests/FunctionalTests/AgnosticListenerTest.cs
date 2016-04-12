// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    /// <summary>
    /// Summary description for AgnosticListenerTest
    /// </summary>
    public class AgnosticListenerTest
    {
        public AgnosticListenerTest(ITestOutputHelper _log)
        {
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());
        }

        [Fact]
        public void Create_Success()
        {
            // NOTE: the '0' below will cause the TcpListener to bind to an anonymous port.
            TcpListener listener = new TcpListener(IPAddress.IPv6Any, 0);
            listener.Server.DualMode = true;

            listener.Start();
            listener.Stop();
        }

        [Fact]
        public void ConnectWithV4_Success()
        {
            int port;
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out port);
            IAsyncResult asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient client = new TcpClient(AddressFamily.InterNetwork);
            client.ConnectAsync(IPAddress.Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedClient = listener.EndAcceptTcpClient(asyncResult);
            client.Dispose();
            acceptedClient.Dispose();
            listener.Stop();
        }

        [Fact]
        public void ConnectWithV6_Success()
        {
            int port;
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out port);
            IAsyncResult asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient client = new TcpClient(AddressFamily.InterNetworkV6);
            client.ConnectAsync(IPAddress.IPv6Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedClient = listener.EndAcceptTcpClient(asyncResult);
            client.Dispose();
            acceptedClient.Dispose();
            listener.Stop();
        }

        [Fact]
        public void ConnectWithV4AndV6_Success()
        {
            int port;
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out port);
            IAsyncResult asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient v6Client = new TcpClient(AddressFamily.InterNetworkV6);
            v6Client.ConnectAsync(IPAddress.IPv6Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedV6Client = listener.EndAcceptTcpClient(asyncResult);

            asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient v4Client = new TcpClient(AddressFamily.InterNetwork);
            v4Client.ConnectAsync(IPAddress.Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedV4Client = listener.EndAcceptTcpClient(asyncResult);

            v6Client.Dispose();
            acceptedV6Client.Dispose();

            v4Client.Dispose();
            acceptedV4Client.Dispose();

            listener.Stop();
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
