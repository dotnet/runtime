// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public sealed class SendReceiveSpanSync : SendReceive<SocketHelperSpanSync> { }
    public sealed class SendReceiveSpanSyncForceNonBlocking : SendReceive<SocketHelperSpanSyncForceNonBlocking> { }
    public sealed class SendReceiveMemoryArrayTask : SendReceive<SocketHelperMemoryArrayTask>
    {
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
    public sealed class SendReceiveMemoryNativeTask : SendReceive<SocketHelperMemoryNativeTask> { }
}
