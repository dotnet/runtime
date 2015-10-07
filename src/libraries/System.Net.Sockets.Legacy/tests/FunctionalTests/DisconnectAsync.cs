// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class DisconnectAsync
    {
        public void OnCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            if (Socket.OSSupportsIPv4)
            {
                int port1, port2;
                using (SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port1))
                using (SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port2))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += OnCompleted;
                    args.UserToken = completed;
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port1);
                    args.DisconnectReuseSocket = true;

                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    Assert.True(client.ConnectAsync(args));
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    Assert.True(client.DisconnectAsync(args));
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);

                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port2);

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

                Assert.Throws<PlatformNotSupportedException>(() => client.DisconnectAsync(args));

                client.Dispose();
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
