// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class DisconnectTest
    {
        private readonly ITestOutputHelper _log;

        public DisconnectTest(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
            Assert.True(Capability.IPv4Support() || Capability.IPv6Support());
        }
        public void OnCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [Fact]
        [OuterLoop("Issue #11345")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Disconnect_Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            using (var server2 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
                args.UserToken = completed;
                args.RemoteEndPoint = server1.EndPoint;
                args.DisconnectReuseSocket = true;

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);

                    client.Disconnect(true);

                    args.RemoteEndPoint = server2.EndPoint;

                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);
                }
            }
        }

        [Fact]
        [OuterLoop("Issue #11345")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void DisconnectAsync_Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            using (var server2 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
                args.UserToken = completed;
                args.RemoteEndPoint = server1.EndPoint;
                args.DisconnectReuseSocket = true;

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);

                    Assert.True(client.DisconnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);

                    args.RemoteEndPoint = server2.EndPoint;

                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);
                }
            }
        }

        [Fact]
        [OuterLoop("Issue #11345")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void BeginDisconnect_Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            using (var server2 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
                args.UserToken = completed;
                args.RemoteEndPoint = server1.EndPoint;

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);

                    client.EndDisconnect(client.BeginDisconnect(true, null, null));

                    args.RemoteEndPoint = server2.EndPoint;

                    Assert.True(client.ConnectAsync(args));
                    completed.WaitOne();
                    Assert.Equal(SocketError.Success, args.SocketError);
                }
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Windows)]
        public void Disconnect_NonWindows_NotSupported()
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => client.Disconnect(true));
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Windows)]
        public void DisconnectAsync_NonWindows_NotSupported()
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.DisconnectReuseSocket = true;
                Assert.Throws<PlatformNotSupportedException>(() => client.DisconnectAsync(args));
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Windows)]
        public void BeginDisconnect_NonWindows_NotSupported()
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => client.BeginDisconnect(true, null, null));
            }
        }
    }
}
