// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public abstract class LocalEndPointTest
    {
        protected abstract bool IPv6 { get; }

        private IPAddress Wildcard => IPv6 ? IPAddress.IPv6Any : IPAddress.Any;

        private IPAddress Loopback => IPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;

        [Fact]
        public void UdpSocket_ClientBoundToWildcardAddress_LocalEPDoesNotChangeOnSendTo()
        {
            using (Socket server = CreateUdpSocket())
            using (Socket client = CreateUdpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);

                Assert.Null(client.LocalEndPoint);

                int clientPortAfterBind = client.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // wildcard before sendto

                var sendToEP = new IPEndPoint(Loopback, serverPort);

                client.SendTo(new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // stays as wildcard after sendto
                Assert.Equal(clientPortAfterBind, GetLocalEPPort(client));

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(Wildcard, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);
                Assert.Equal(Loopback, ((IPEndPoint)receiveFromEP).Address); // received from specific address
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)receiveFromEP).Port);

                IAsyncResult sendToResult = client.BeginSendTo(new byte[] { 4, 5, 6 }, 0, 3, SocketFlags.None, sendToEP, null, null);
                sendToResult.AsyncWaitHandle.WaitOne();
                client.EndSendTo(sendToResult);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // stays as wildcard after async WSASendTo
                Assert.Equal(clientPortAfterBind, GetLocalEPPort(client));

                buf = new byte[3];
                receiveFromEP = new IPEndPoint(Wildcard, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 4, 5, 6 }, buf);
                Assert.Equal(Loopback, ((IPEndPoint)receiveFromEP).Address); // received from specific address
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)receiveFromEP).Port);
            }
        }

        [Fact]
        public void UdpSocket_ClientNotBound_LocalEPBecomesWildcardOnSendTo()
        {
            using (Socket server = CreateUdpSocket())
            using (Socket client = CreateUdpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);

                Assert.Null(client.LocalEndPoint); // null before sendto

                var sendToEP = new IPEndPoint(Loopback, serverPort);

                client.SendTo(new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // wildcard after sendto

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(Wildcard, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);
            }
        }

        [Fact]
        public void UdpSocket_ClientNotBound_LocalEPBecomesWildcardOnAsyncSendTo()
        {
            using (Socket server = CreateUdpSocket())
            using (Socket client = CreateUdpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);

                Assert.Null(client.LocalEndPoint); // null before async WSASendTo

                var sendToEP = new IPEndPoint(Loopback, serverPort);

                IAsyncResult sendToResult = client.BeginSendTo(new byte[] { 4, 5, 6 }, 0, 3, SocketFlags.None, sendToEP, null, null);
                sendToResult.AsyncWaitHandle.WaitOne();
                client.EndSendTo(sendToResult);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // wildcard after async WSASendTo

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(Wildcard, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 4, 5, 6 }, buf);
            }
        }

        [Fact]
        public async Task TcpSocket_ClientBoundToWildcardAddress_LocalEPChangeToSpecificOnConnnect()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);
                int clientPortAfterBind = client.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(client)); // wildcard before connect

                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                client.Connect(new IPEndPoint(Loopback, serverPort));

                Assert.Equal(Loopback, GetLocalEPAddress(client)); // specific after connect
                Assert.Equal(clientPortAfterBind, GetLocalEPPort(client));

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public async Task TcpSocket_ClientNotBound_LocalEPChangeToSpecificOnConnnect()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Loopback);
                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                Assert.Null(client.LocalEndPoint); // null before connect

                client.Connect(new IPEndPoint(Loopback, serverPort));

                Assert.Equal(Loopback, GetLocalEPAddress(client)); // specific after connect

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public async Task TcpSocket_ServerBoundToWildcardAddress_AcceptSocketLocalEPIsSpecific()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Wildcard);

                Assert.Equal(Wildcard, GetLocalEPAddress(server)); // server -> wildcard before accept

                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                client.Connect(new IPEndPoint(Loopback, serverPort));

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
                Assert.Equal(accept.LocalEndPoint, client.RemoteEndPoint);

                Assert.Equal(Wildcard, GetLocalEPAddress(server)); // server -> stays as wildcard
                Assert.Equal(Loopback, GetLocalEPAddress(accept)); // accept -> specific
                Assert.Equal(serverPort, GetLocalEPPort(accept));
            }
        }

        [Fact]
        public async Task TcpSocket_ServerBoundToSpecificAddress_AcceptSocketLocalEPIsSame()
        {
            using (Socket server = CreateTcpSocket())
            using (Socket client = CreateTcpSocket())
            {
                int serverPort = server.BindToAnonymousPort(Loopback);

                Assert.Equal(Loopback, GetLocalEPAddress(server)); // server -> specific before accept

                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                client.Connect(new IPEndPoint(Loopback, serverPort));

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

    [Trait("IPv4", "true")]
    public class LocalEndPointIPv4Test : LocalEndPointTest
    {
        protected override bool IPv6 => false;
    }

    [Trait("IPv6", "true")]
    public class LocalEndPointIPv6Test : LocalEndPointTest
    {
        protected override bool IPv6 => true;
    }
}
