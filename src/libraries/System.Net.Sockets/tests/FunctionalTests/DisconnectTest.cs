// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
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
        public void InvalidArguments_Throw()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => s.EndDisconnect(null));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => s.EndDisconnect(Task.CompletedTask));
                s.Dispose();
                Assert.Throws<ObjectDisposedException>(() => s.Disconnect(true));
                Assert.Throws<ObjectDisposedException>(() => s.BeginDisconnect(true, null, null));
                Assert.Throws<ObjectDisposedException>(() => s.EndDisconnect(null));
                Assert.Throws<ObjectDisposedException>(() => { s.DisconnectAsync(null); });
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop("Issue #11345")]
        public void Disconnect_Success(bool reuseSocket)
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
                args.DisconnectReuseSocket = reuseSocket;

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(SocketError.Success, args.SocketError);

                    client.Disconnect(reuseSocket);

                    Assert.False(client.Connected);

                    args.RemoteEndPoint = server2.EndPoint;

                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(reuseSocket ? SocketError.Success : SocketError.IsConnected, args.SocketError);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop("Issue #11345")]
        public void DisconnectAsync_Success(bool reuseSocket)
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
                args.DisconnectReuseSocket = reuseSocket;

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(SocketError.Success, args.SocketError);

                    if (client.DisconnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(SocketError.Success, args.SocketError);
                    Assert.False(client.Connected);

                    args.RemoteEndPoint = server2.EndPoint;

                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(reuseSocket ? SocketError.Success : SocketError.IsConnected, args.SocketError);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop("Issue #11345")]
        public void BeginDisconnect_Success(bool reuseSocket)
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
                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(SocketError.Success, args.SocketError);

                    IAsyncResult ar = client.BeginDisconnect(reuseSocket, null, null);
                    client.EndDisconnect(ar);

                    Assert.False(client.Connected);

                    Assert.Throws<InvalidOperationException>(() => client.EndDisconnect(ar));

                    args.RemoteEndPoint = server2.EndPoint;

                    if (client.ConnectAsync(args))
                    {
                        completed.WaitOne();
                    }

                    Assert.Equal(reuseSocket ? SocketError.Success : SocketError.IsConnected, args.SocketError);
                }
            }
        }
    }
}
