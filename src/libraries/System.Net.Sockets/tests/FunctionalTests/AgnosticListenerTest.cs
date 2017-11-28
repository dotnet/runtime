// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading.Tasks;
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
        public async Task ConnectWithV4_Success()
        {
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out int port);
            Task<TcpClient> acceptTask = Task.Factory.FromAsync(listener.BeginAcceptTcpClient(null, null), listener.EndAcceptTcpClient);

            TcpClient client = new TcpClient(AddressFamily.InterNetwork);
            Task connectTask = client.ConnectAsync(IPAddress.Loopback, port);

            await (new Task[] { acceptTask, connectTask }).WhenAllOrAnyFailed();
                        
            client.Dispose();
            acceptTask.Result.Dispose();
            listener.Stop();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ConnectWithV6_Success()
        {
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out int port);
            Task<TcpClient> acceptTask = Task.Factory.FromAsync(listener.BeginAcceptTcpClient(null, null), listener.EndAcceptTcpClient);

            TcpClient client = new TcpClient(AddressFamily.InterNetworkV6);
            Task connectTask = client.ConnectAsync(IPAddress.IPv6Loopback, port);

            await (new Task[] { acceptTask, connectTask }).WhenAllOrAnyFailed();

            client.Dispose();
            acceptTask.Result.Dispose();
            listener.Stop();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ConnectWithV4AndV6_Success()
        {
            TcpListener listener = SocketTestExtensions.CreateAndStartTcpListenerOnAnonymousPort(out int port);
            Task<TcpClient> acceptTask = Task.Factory.FromAsync(listener.BeginAcceptTcpClient(null, null), listener.EndAcceptTcpClient);

            TcpClient v6Client = new TcpClient(AddressFamily.InterNetworkV6);
            Task connectTask = v6Client.ConnectAsync(IPAddress.IPv6Loopback, port);

            Task[] tasks = new Task[] { acceptTask, connectTask };
            await tasks.WhenAllOrAnyFailed();

            TcpClient acceptedV6Client = acceptTask.Result;
            Assert.Equal(AddressFamily.InterNetworkV6, acceptedV6Client.Client.RemoteEndPoint.AddressFamily);
            Assert.Equal(AddressFamily.InterNetworkV6, v6Client.Client.RemoteEndPoint.AddressFamily);

            acceptTask = Task.Factory.FromAsync(listener.BeginAcceptTcpClient(null, null), listener.EndAcceptTcpClient);

            TcpClient v4Client = new TcpClient(AddressFamily.InterNetwork);
            connectTask = v4Client.ConnectAsync(IPAddress.Loopback, port);
            tasks[0] = acceptTask;
            tasks[1] = connectTask;
            await tasks.WhenAllOrAnyFailed();

            TcpClient acceptedV4Client = acceptTask.Result;
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
        [PlatformSpecific(TestPlatforms.Windows)]  // Unix platforms do not support TcpListener.AllowNatTraversal
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix platforms do not support TcpListener.AllowNatTraversal
        [InlineData(true)]
        [InlineData(false)]
        public void AllowNatTraversal_AnyUnix(bool allow)
        {
            var l = new TcpListener(IPAddress.Any, 0);
            Assert.Throws<PlatformNotSupportedException>(() => l.AllowNatTraversal(allow));
        }
    }
}
