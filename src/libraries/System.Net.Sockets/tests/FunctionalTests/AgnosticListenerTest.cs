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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Create_Success()
        {
            // NOTE: the '0' below will cause the TcpListener to bind to an anonymous port.
            TcpListener listener = new TcpListener(IPAddress.IPv6Any, 0);
            listener.Server.DualMode = true;

            listener.Start();
            listener.Stop();
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void ConnectWithV4AndV6_Success()
        {
            int port;
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out port);
            IAsyncResult asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient v6Client = new TcpClient(AddressFamily.InterNetworkV6);
            v6Client.ConnectAsync(IPAddress.IPv6Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedV6Client = listener.EndAcceptTcpClient(asyncResult);
            Assert.Equal(AddressFamily.InterNetworkV6, acceptedV6Client.Client.RemoteEndPoint.AddressFamily);
            Assert.Equal(AddressFamily.InterNetworkV6, v6Client.Client.RemoteEndPoint.AddressFamily);

            asyncResult = listener.BeginAcceptTcpClient(null, null);

            TcpClient v4Client = new TcpClient(AddressFamily.InterNetwork);
            v4Client.ConnectAsync(IPAddress.Loopback, port).GetAwaiter().GetResult();

            TcpClient acceptedV4Client = listener.EndAcceptTcpClient(asyncResult);
            Assert.Equal(AddressFamily.InterNetworkV6, acceptedV4Client.Client.RemoteEndPoint.AddressFamily);
            Assert.Equal(AddressFamily.InterNetwork, v4Client.Client.RemoteEndPoint.AddressFamily);

            v6Client.Dispose();
            acceptedV6Client.Dispose();

            v4Client.Dispose();
            acceptedV4Client.Dispose();

            listener.Stop();
        }

        [Fact]
        public void StaticCreate_Success()
        {
            TcpListener listener = TcpListener.Create(0);

            IPEndPoint ep = (IPEndPoint)listener.LocalEndpoint;
            Assert.Equal(ep.Address, IPAddress.IPv6Any);
            Assert.Equal(ep.Port, 0);
            Assert.True(listener.Server.DualMode);

            listener.Start();
            listener.Stop();
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true, IPProtectionLevel.Unrestricted)]
        [InlineData(false, IPProtectionLevel.EdgeRestricted)]
        public void AllowNatTraversal_Windows(bool allow, IPProtectionLevel resultLevel)
        {
            var l = new TcpListener(IPAddress.Any, 0);
            l.AllowNatTraversal(allow);
            Assert.Equal((int)resultLevel, (int)l.Server.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.IPProtectionLevel));
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(true)]
        [InlineData(false)]
        public void AllowNatTraversal_AnyUnix(bool allow)
        {
            var l = new TcpListener(IPAddress.Any, 0);
            Assert.Throws<PlatformNotSupportedException>(() => l.AllowNatTraversal(allow));
        }


        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets.
        [OuterLoop] // TODO: Issue #11345
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
