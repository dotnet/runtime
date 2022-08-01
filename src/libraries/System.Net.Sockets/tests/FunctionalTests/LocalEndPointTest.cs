// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    // The test class is declared non-parallel because of possible IPv4/IPv6 port-collision on Unix:
    // When running in parallel with other tests, there is some chance that Accept() calls in LocalEndPointTest will
    // accept a connection request from another, DualMode client living in a parallel test
    // that is intended to connect to a server of opposite AddressFamily in the parallel test.
    [Collection(nameof(DisableParallelization))]
    public abstract class LocalEndPointTest<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected abstract bool IPv6 { get; }

        private IPAddress Wildcard => IPv6 ? IPAddress.IPv6Any : IPAddress.Any;

        private IPAddress Loopback => IPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;

        public LocalEndPointTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task UdpSocket_WhenBoundToWildcardAddress_LocalEPDoesNotChangeOnSendTo()
        {
            using (Socket receiver = CreateUdpSocket())
            using (Socket sender = CreateUdpSocket())
            {
                int receiverPort = receiver.BindToAnonymousPort(Wildcard);

                Assert.Null(sender.LocalEndPoint);

                int senderPortAfterBind = sender.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(sender)); // wildcard before sendto

                var sendToEP = new IPEndPoint(Loopback, receiverPort);

                await SendToAsync(sender, new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(Wildcard, GetLocalEPAddress(sender)); // stays as wildcard after sendto
                Assert.Equal(senderPortAfterBind, GetLocalEPPort(sender));

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(Wildcard, 0);
                receiver.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);
                Assert.Equal(Loopback, ((IPEndPoint)receiveFromEP).Address); // received from specific address
                Assert.Equal(senderPortAfterBind, ((IPEndPoint)receiveFromEP).Port);
            }
        }

        [Fact]
        public async Task UdpSocket_WhenNotBound_LocalEPChangeToWildcardOnSendTo()
        {
            using (Socket receiver = CreateUdpSocket())
            using (Socket sender = CreateUdpSocket())
            {
                int receiverPort = receiver.BindToAnonymousPort(Wildcard);

                Assert.Null(sender.LocalEndPoint); // null before sendto

                var sendToEP = new IPEndPoint(Loopback, receiverPort);

                await SendToAsync(sender, new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(Wildcard, GetLocalEPAddress(sender)); // changes to wildcard after sendto

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(Wildcard, 0);
                receiver.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);
                Assert.Equal(Loopback, ((IPEndPoint)receiveFromEP).Address); // received from specific address
            }
        }

        [Fact]
        public async Task TcpClientSocket_WhenBoundToWildcardAddress_LocalEPChangeToSpecificOnConnect()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);
                int clientPortAfterBind = client.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // wildcard before connect

                server.Listen();
                Task<Socket> acceptTask = AcceptAsync(server);

                await ConnectAsync(client, new IPEndPoint(Loopback, serverPort));

                Assert.Equal(Loopback, GetLocalEPAddress(client)); // changes to specific after connect
                Assert.Equal(clientPortAfterBind, GetLocalEPPort(client));

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public async Task TcpClientSocket_WhenNotBound_LocalEPChangeToSpecificOnConnect()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Loopback);
                server.Listen();
                Task<Socket> acceptTask = AcceptAsync(server);

                Assert.Null(client.LocalEndPoint); // null before connect

                await ConnectAsync(client, new IPEndPoint(Loopback, serverPort));

                Assert.Equal(Loopback, GetLocalEPAddress(client)); // changes to specific after connect

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public async Task TcpAcceptSocket_WhenServerBoundToWildcardAddress_LocalEPIsSpecific()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(server)); // server -> wildcard before accept

                server.Listen();
                Task<Socket> acceptTask = AcceptAsync(server);

                await ConnectAsync(client, new IPEndPoint(Loopback, serverPort));

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
                Assert.Equal(accept.LocalEndPoint, client.RemoteEndPoint);

                Assert.Equal(Wildcard, GetLocalEPAddress(server)); // server -> stays as wildcard
                Assert.Equal(Loopback, GetLocalEPAddress(accept)); // accept -> specific
                Assert.Equal(serverPort, GetLocalEPPort(accept));
            }
        }

        [Fact]
        public async Task TcpAcceptSocket_WhenServerBoundToSpecificAddress_LocalEPIsSame()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Loopback);

                Assert.Equal(Loopback, GetLocalEPAddress(server)); // server -> specific before accept

                server.Listen();
                Task<Socket> acceptTask = AcceptAsync(server);

                await ConnectAsync(client, new IPEndPoint(Loopback, serverPort));

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);

                Assert.Equal(GetLocalEPAddress(server), GetLocalEPAddress(accept)); // accept -> same address
                Assert.Equal(serverPort, GetLocalEPPort(accept));
            }
        }

        [Fact]
        public void LocalEndPoint_IsCached()
        {
            using (Socket socket = CreateTcpSocket())
            {
                socket.BindToAnonymousPort(Loopback);

                EndPoint localEndPointCall1 = socket.LocalEndPoint;
                EndPoint localEndPointCall2 = socket.LocalEndPoint;

                Assert.Same(localEndPointCall1, localEndPointCall2);
            }
        }

        private Socket CreateUdpSocket()
        {
            return new Socket(
                IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp
            );
        }

        private Socket CreateTcpSocket()
        {
            return new Socket(
                IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
        }

        private IPAddress GetLocalEPAddress(Socket socket)
        {
            return ((IPEndPoint)socket.LocalEndPoint).Address;
        }

        private int GetLocalEPPort(Socket socket)
        {
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }
    public abstract class LocalEndPointTestIPv4<T> : LocalEndPointTest<T> where T : SocketHelperBase, new()
    {
        protected override bool IPv6 => false;

        public LocalEndPointTestIPv4(ITestOutputHelper output) : base(output) { }
    }

    public abstract class LocalEndPointTestIPv6<T> : LocalEndPointTest<T> where T : SocketHelperBase, new()
    {
        protected override bool IPv6 => true;

        public LocalEndPointTestIPv6(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    public sealed class LocalEndPointTestIPv4Sync : LocalEndPointTestIPv4<SocketHelperArraySync>
    {
        public LocalEndPointTestIPv4Sync(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    public sealed class LocalEndPointTestIPv4SyncForceNonBlocking : LocalEndPointTestIPv4<SocketHelperSyncForceNonBlocking>
    {
        public LocalEndPointTestIPv4SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    public sealed class LocalEndPointTestIPv4Apm : LocalEndPointTestIPv4<SocketHelperApm>
    {
        public LocalEndPointTestIPv4Apm(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    public sealed class LocalEndPointTestIPv4Task : LocalEndPointTestIPv4<SocketHelperTask>
    {
        public LocalEndPointTestIPv4Task(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    public sealed class LocalEndPointTestIPv4Eap : LocalEndPointTestIPv4<SocketHelperEap>
    {
        public LocalEndPointTestIPv4Eap(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv6", "true")]
    public sealed class LocalEndPointTestIPv6Sync : LocalEndPointTestIPv6<SocketHelperArraySync>
    {
        public LocalEndPointTestIPv6Sync(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv6", "true")]
    public sealed class LocalEndPointTestIPv6SyncForceNonBlocking : LocalEndPointTestIPv6<SocketHelperSyncForceNonBlocking>
    {
        public LocalEndPointTestIPv6SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv6", "true")]
    public sealed class LocalEndPointTestIPv6Apm : LocalEndPointTestIPv6<SocketHelperApm>
    {
        public LocalEndPointTestIPv6Apm(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv6", "true")]
    public sealed class LocalEndPointTestIPv6Task : LocalEndPointTestIPv6<SocketHelperTask>
    {
        public LocalEndPointTestIPv6Task(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv6", "true")]
    public sealed class LocalEndPointTestIPv6Eap : LocalEndPointTestIPv6<SocketHelperEap>
    {
        public LocalEndPointTestIPv6Eap(ITestOutputHelper output) : base(output) { }
    }
}
