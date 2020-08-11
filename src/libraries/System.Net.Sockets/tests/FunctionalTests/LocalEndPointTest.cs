// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class LocalEndPointTest
    {
        [Fact]
        public void UdpSocket_BoundToWildcardAddress_LocalEPDoesNotChangeOnSendTo()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int serverPort = server.BindToAnonymousPort(IPAddress.Any);

                Assert.Null(client.LocalEndPoint);

                int clientPortAfterBind = client.BindToAnonymousPort(IPAddress.Any);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);

                var sendToEP = new IPEndPoint(IPAddress.Loopback, serverPort);

                client.SendTo(new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)client.LocalEndPoint).Port);

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(IPAddress.Any, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)receiveFromEP).Port);

                IAsyncResult sendToResult = client.BeginSendTo(new byte[] { 4, 5, 6 }, 0, 3, SocketFlags.None, sendToEP, null, null);
                sendToResult.AsyncWaitHandle.WaitOne();
                client.EndSendTo(sendToResult);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)client.LocalEndPoint).Port);

                buf = new byte[3];
                receiveFromEP = new IPEndPoint(IPAddress.Any, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 4, 5, 6 }, buf);
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)receiveFromEP).Port);
            }
        }

        [Fact]
        public void UdpSocket_NotBound_LocalEPBecomesWildcardAddressOnSendTo()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int serverPort = server.BindToAnonymousPort(IPAddress.Any);

                Assert.Null(client.LocalEndPoint);

                var sendToEP = new IPEndPoint(IPAddress.Loopback, serverPort);

                client.SendTo(new byte[] { 1, 2, 3 }, sendToEP);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);

                byte[] buf = new byte[3];
                EndPoint receiveFromEP = new IPEndPoint(IPAddress.Any, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 1, 2, 3 }, buf);

                IAsyncResult sendToResult = client.BeginSendTo(new byte[] { 4, 5, 6 }, 0, 3, SocketFlags.None, sendToEP, null, null);
                sendToResult.AsyncWaitHandle.WaitOne();
                client.EndSendTo(sendToResult);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);

                buf = new byte[3];
                receiveFromEP = new IPEndPoint(IPAddress.Any, 0);
                server.ReceiveFrom(buf, ref receiveFromEP);

                Assert.Equal(new byte[] { 4, 5, 6 }, buf);
            }
        }

        [Fact]
        public async Task TcpSocket_BoundToWildcardAddress_LocalEPChangeToSpecificOnConnnect()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int serverPort = server.BindToAnonymousPort(IPAddress.Any);
                int clientPortAfterBind = client.BindToAnonymousPort(IPAddress.Any);

                Assert.Equal(IPAddress.Any, ((IPEndPoint)client.LocalEndPoint).Address);

                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));

                Assert.Equal(IPAddress.Loopback, ((IPEndPoint)client.LocalEndPoint).Address);
                Assert.Equal(clientPortAfterBind, ((IPEndPoint)client.LocalEndPoint).Port);

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public async Task TcpSocket_NotBound_LocalEPChangeToSpecificOnConnnect()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int serverPort = server.BindToAnonymousPort(IPAddress.Any);
                server.Listen();
                Task<Socket> acceptTask = server.AcceptAsync();

                Assert.Null(client.LocalEndPoint);

                client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort);

                Assert.Equal(IPAddress.Loopback, ((IPEndPoint)client.LocalEndPoint).Address);

                Socket accept = await acceptTask;
                Assert.Equal(accept.RemoteEndPoint, client.LocalEndPoint);
            }
        }

        [Fact]
        public void LocalEndPoint_IsCached()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Any);

                EndPoint localEndPointCall1 = socket.LocalEndPoint;
                EndPoint localEndPointCall2 = socket.LocalEndPoint;

                Assert.Same(localEndPointCall1, localEndPointCall2);
            }
        }
    }
}
