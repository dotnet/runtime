// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ConnectAsync
    {
        private const int TestPortBase = TestPortBases.ConnectAsync;

        public void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();            
        }

        [Fact]
        public void Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            if (Socket.OSSupportsIPv4)
            {
                using (SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.Loopback, TestPortBase)))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestPortBase);
                    args.Completed += OnConnectCompleted;
                    args.UserToken = completed;

                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Assert.True(client.ConnectAsync(args));

                    Assert.True(completed.WaitOne(5000), "IPv4: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Dispose();
                }
            }

            if (Socket.OSSupportsIPv6)
            {
                using (SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase)))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase);
                    args.Completed += OnConnectCompleted;
                    args.UserToken = completed;

                    Socket client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    Assert.True(client.ConnectAsync(args));

                    Assert.True(completed.WaitOne(5000), "IPv6: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Dispose();
                }
            }
        }
    }
}
