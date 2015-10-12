// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class Disconnect
    {
        public void OnCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        [ActiveIssue(3580)]
        public void Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            if (Socket.OSSupportsIPv4)
            {
                int port, port1;
                using (SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port))
                using (SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port1))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += OnCompleted;
                    args.UserToken = completed;
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    args.DisconnectReuseSocket = true;

                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    Assert.True(client.ConnectAsync(args));
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Disconnect(true);

                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port1);

                    Assert.True(client.ConnectAsync(args));
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    client.Dispose();
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void DisconnectAsync_Throws_PlatformNotSupported()
        {
            IPAddress address = null;
            if (Socket.OSSupportsIPv4)
            {
                address = IPAddress.Loopback;
            }
            else if (Socket.OSSupportsIPv6)
            {
                address = IPAddress.IPv6Loopback;
            }
            else
            {
                return;
            }

            int port;
            using (SocketTestServer.SocketTestServerFactory(address, out port))
            {
                var completed = new AutoResetEvent(false);

                var args = new SocketAsyncEventArgs
                {
                    UserToken = completed,
                    RemoteEndPoint = new IPEndPoint(address, port),
                    DisconnectReuseSocket = true
                };
                args.Completed += OnCompleted;

                var client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Assert.True(client.ConnectAsync(args));
                Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                Assert.Throws<PlatformNotSupportedException>(() => client.Disconnect(true));

                client.Dispose();
            }
        }
    }
}
