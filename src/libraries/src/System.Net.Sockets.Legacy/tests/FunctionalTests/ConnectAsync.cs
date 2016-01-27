// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ConnectAsync
    {
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
                int port;
                using (SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    args.Completed += OnConnectCompleted;
                    args.UserToken = completed;

                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Assert.True(client.ConnectAsync(args));

                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "IPv4: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Dispose();
                }
            }

            if (Socket.OSSupportsIPv6)
            {
                int port;
                using (SocketTestServer.SocketTestServerFactory(IPAddress.IPv6Loopback, out port))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, port);
                    args.Completed += OnConnectCompleted;
                    args.UserToken = completed;

                    Socket client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    Assert.True(client.ConnectAsync(args));

                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "IPv6: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Dispose();
                }
            }
        }
    }
}
