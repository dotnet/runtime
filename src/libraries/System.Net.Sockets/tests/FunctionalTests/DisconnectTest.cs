// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class Disconnect<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected Disconnect(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Disconnect_Success(bool reuseSocket)
        {
            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            using (var server2 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, server1.EndPoint);
                    Assert.True(client.Connected);

                    await DisconnectAsync(client, reuseSocket);
                    Assert.False(client.Connected);

                    if (reuseSocket)
                    {
                        // Note that the new connect operation must be asynchronous
                        // (why? I'm not sure, but that's the way it works currently)
                        await client.ConnectAsync(server2.EndPoint);
                        Assert.True(client.Connected);
                    }
                    else if (UsesSync)
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(async () => await ConnectAsync(client, server2.EndPoint));
                    }
                    else
                    {
                        SocketException se = await Assert.ThrowsAsync<SocketException>(async () => await ConnectAsync(client, server2.EndPoint));
                        Assert.Equal(SocketError.IsConnected, se.SocketErrorCode);
                    }
                }
            }
        }

        [Fact]
        public async Task DisconnectAndReuse_ReconnectSync_ThrowsInvalidOperationException()
        {
            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            using (var server2 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, server1.EndPoint);
                    Assert.True(client.Connected);

                    await DisconnectAsync(client, reuseSocket: true);
                    Assert.False(client.Connected);

                    // Note that the new connect operation must be asynchronous
                    // (why? I'm not sure, but that's the way it works currently)
                    // So try connecting synchronously, and it should fail
                    Assert.Throws<InvalidOperationException>(() => client.Connect(server2.EndPoint));
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Disconnect_NotConnected_ThrowsInvalidOperationException(bool reuseSocket)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await DisconnectAsync(s, reuseSocket));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Disconnect_ObjectDisposed_ThrowsObjectDisposedException(bool reuseSocket)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                s.Dispose();
                Assert.ThrowsAsync<ObjectDisposedException>(async () => await DisconnectAsync(s, reuseSocket));
            }
        }
    }

    public sealed class Disconnect_Sync : Disconnect<SocketHelperArraySync>
    {
        public Disconnect_Sync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class Disconnect_SyncForceNonBlocking : Disconnect<SocketHelperSyncForceNonBlocking>
    {
        public Disconnect_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class Disconnect_Apm : Disconnect<SocketHelperApm>
    {
        public Disconnect_Apm(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void EndDisconnect_InvalidArguments_Throws()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => s.EndDisconnect(null));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => s.EndDisconnect(Task.CompletedTask));
            }
        }

        [Fact]
        public void BeginDisconnect_NotConnected_ThrowSync()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => s.BeginDisconnect(true, null, null));
                Assert.Throws<SocketException>(() => s.BeginDisconnect(false, null, null));
            }
        }

        [Fact]
        public void BeginDisconnection_ObjectDisposed_ThrowSync()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                s.Dispose();
                Assert.Throws<ObjectDisposedException>(() => s.BeginDisconnect(true, null, null));
                Assert.Throws<ObjectDisposedException>(() => s.BeginDisconnect(false, null, null));
            }
        }
    }

    public sealed class Disconnect_Task : Disconnect<SocketHelperTask>
    {
        public Disconnect_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class Disconnect_CancellableTask : Disconnect<SocketHelperCancellableTask>
    {
        public Disconnect_CancellableTask(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Disconnect_Precanceled_ThrowsOperationCanceledException(bool reuseSocket)
        {
            IPEndPoint loopback = new IPEndPoint(IPAddress.Loopback, 0);
            using (var server1 = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, loopback))
            {
                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, server1.EndPoint);
                    Assert.True(client.Connected);

                    CancellationTokenSource precanceledSource = new CancellationTokenSource();
                    precanceledSource.Cancel();

                    OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.DisconnectAsync(reuseSocket, precanceledSource.Token));
                    Assert.Equal(precanceledSource.Token, oce.CancellationToken);
                }
            }
        }
    }

    public sealed class Disconnect_Eap : Disconnect<SocketHelperEap>
    {
        public Disconnect_Eap(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void InvalidArguments_Throw()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                AssertExtensions.Throws<ArgumentNullException>("e", () => s.DisconnectAsync(null));
            }
        }
    }
}
