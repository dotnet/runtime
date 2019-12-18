// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class SendTo<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected SendTo(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task SendTo_Datagram_UDP_ShouldImplicitlyBindLocalEndpoint()
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Parse("10.20.30.40"), 1234);

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[32];

            Task sendTask = SendToAsync(socket, new ArraySegment<byte>(buffer), remoteEndpoint);

            // Asynchronous calls shall alter the property immediately:
            if (!UsesSync)
            {
                Assert.NotNull(socket.LocalEndPoint);
            }

            await sendTask;

            // In synchronous calls, we should wait for the completion of the helper task:
            Assert.NotNull(socket.LocalEndPoint);
        }

        [Fact]
        public async Task SendTo_Datagram_UDP_AccessDenied_Throws_DoesNotBind()
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Broadcast, 1234);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[32];

            var e = await Assert.ThrowsAnyAsync<SocketException>(() => SendToAsync(socket, new ArraySegment<byte>(buffer), remoteEndpoint));
            Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            Assert.Null(socket.LocalEndPoint);
        }
    }

    public sealed class SendToSync : SendTo<SocketHelperArraySync>
    {
        public SendToSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendToSyncForceNonBlocking : SendTo<SocketHelperSyncForceNonBlocking>
    {
        public SendToSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendToApm : SendTo<SocketHelperApm>
    {
        public SendToApm(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendToTask : SendTo<SocketHelperTask>
    {
        public SendToTask(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendToEap : SendTo<SocketHelperEap>
    {
        public SendToEap(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendToSpanSync : SendTo<SocketHelperSpanSync>
    {
        public SendToSpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendToSpanSyncForceNonBlocking : SendTo<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendToSpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendToMemoryArrayTask : SendTo<SocketHelperMemoryArrayTask>
    {
        public SendToMemoryArrayTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendToMemoryNativeTask : SendTo<SocketHelperMemoryNativeTask>
    {
        public SendToMemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
