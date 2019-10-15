// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public sealed class SendReceiveSpanSync : SendReceive<SocketHelperSpanSync>
    {
        public SendReceiveSpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveSpanSyncForceNonBlocking : SendReceive<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendReceiveSpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveMemoryArrayTask : SendReceive<SocketHelperMemoryArrayTask>
    {
        public SendReceiveMemoryArrayTask(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Precanceled_Throws()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.SendAsync((ReadOnlyMemory<byte>)new byte[0], SocketFlags.None, cts.Token));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReceiveAsync((Memory<byte>)new byte[0], SocketFlags.None, cts.Token));
                }
            }
        }

        [Fact]
        public async Task CanceledDuringOperation_Throws()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    CancellationTokenSource cts;

                    for (int len = 0; len < 2; len++)
                    {
                        cts = new CancellationTokenSource();
                        ValueTask<int> vt = server.ReceiveAsync((Memory<byte>)new byte[len], SocketFlags.None, cts.Token);
                        Assert.False(vt.IsCompleted);
                        cts.Cancel();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
                    }

                    // Make sure subsequent operations aren't canceled.
                    await server.SendAsync((ReadOnlyMemory<byte>)new byte[1], SocketFlags.None);
                    Assert.Equal(1, await client.ReceiveAsync((Memory<byte>)new byte[10], SocketFlags.None));
                }
            }
        }

        [Fact]
        public async Task CanceledOneOfMultipleReceives_Udp_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                var cts = new CancellationTokenSource();

                // Create three UDP receives, only one of which we'll cancel.
                byte[] buffer1 = new byte[1], buffer2 = new byte[1], buffer3 = new byte[1];
                ValueTask<int> r1 = client.ReceiveAsync(buffer1.AsMemory(), SocketFlags.None, cts.Token);
                ValueTask<int> r2 = client.ReceiveAsync(buffer2.AsMemory(), SocketFlags.None);
                ValueTask<int> r3 = client.ReceiveAsync(buffer3.AsMemory(), SocketFlags.None);

                // Cancel one of them, and validate it's been canceled.
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await r1);
                Assert.Equal(0, buffer1[0]);

                // Send data to complete the others, and validate they complete successfully.
                using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    server.SendTo(new byte[1] { 42 }, client.LocalEndPoint);
                    server.SendTo(new byte[1] { 43 }, client.LocalEndPoint);
                }

                Assert.Equal(1, await r2);
                Assert.Equal(1, await r3);
                Assert.True(
                    (buffer2[0] == 42 && buffer3[0] == 43) ||
                    (buffer2[0] == 43 && buffer3[0] == 42),
                    $"buffer2[0]={buffer2[0]}, buffer3[0]={buffer3[0]}");
            }
        }

        [Fact]
        public async Task DisposedSocket_ThrowsOperationCanceledException()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    server.Shutdown(SocketShutdown.Both);
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.SendAsync((ReadOnlyMemory<byte>)new byte[0], SocketFlags.None, cts.Token));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReceiveAsync((Memory<byte>)new byte[0], SocketFlags.None, cts.Token));
                }
            }
        }
    }
    public sealed class SendReceiveMemoryNativeTask : SendReceive<SocketHelperMemoryNativeTask>
    {
        public SendReceiveMemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
