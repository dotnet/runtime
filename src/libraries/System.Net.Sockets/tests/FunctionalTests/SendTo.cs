// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class SendToBase<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        private static readonly IPEndPoint ValidUdpRemoteEndpoint = new IPEndPoint(IPAddress.Parse("10.20.30.40"), 1234);

        protected SendToBase(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(1, -1, 0)] // offset low
        [InlineData(1, 2, 0)] // offset high
        [InlineData(1, 0, -1)] // count low
        [InlineData(1, 1, 2)] // count high
        public async Task OutOfRange_Throws(int length, int offset, int count)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            ArraySegment<byte> buffer = new FakeArraySegment
            {
                Array = new byte[length], Count = count, Offset = offset
            }.ToActual();

            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => SendToAsync(socket, buffer, ValidUdpRemoteEndpoint));
        }

        [Fact]
        public async Task NullBuffer_Throws()
        {
            if (!ValidatesArrayArguments) return;
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAsync<ArgumentNullException>(() => SendToAsync(socket, null, ValidUdpRemoteEndpoint));
        }

        [Fact]
        public async Task NullEndpoint_Throws()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAnyAsync<ArgumentException>(() => SendToAsync(socket, new byte[1], null));
        }

        [Fact]
        public async Task Datagram_UDP_ShouldImplicitlyBindLocalEndpoint()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[32];

            Task sendTask = SendToAsync(socket, new ArraySegment<byte>(buffer), ValidUdpRemoteEndpoint);

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
        public async Task Datagram_UDP_AccessDenied_Throws_DoesNotBind()
        {
            IPEndPoint invalidEndpoint = new IPEndPoint(IPAddress.Broadcast, 1234);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[32];

            var e = await Assert.ThrowsAnyAsync<SocketException>(() => SendToAsync(socket, new ArraySegment<byte>(buffer), invalidEndpoint));
            Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            Assert.Null(socket.LocalEndPoint);
        }
    }

    public static class SendTo
    {
        public static class Sync
        {
            public sealed class Span : SendToBase<SocketHelperSpanSync>
            {
                public Span(ITestOutputHelper output) : base(output) { }
            }

            public sealed class SpanForceNonBlocking : SendToBase<SocketHelperSpanSyncForceNonBlocking>
            {
                public SpanForceNonBlocking(ITestOutputHelper output) : base(output) { }
            }

            public sealed class MemoryArrayTask : SendToBase<SocketHelperMemoryArrayTask>
            {
                public MemoryArrayTask(ITestOutputHelper output) : base(output) { }
            }

            public sealed class MemoryNativeTask : SendToBase<SocketHelperMemoryNativeTask>
            {
                public MemoryNativeTask(ITestOutputHelper output) : base(output) { }
            }

            public sealed class ArraySync : SendToBase<SocketHelperArraySync>
            {
                public ArraySync(ITestOutputHelper output) : base(output) { }
            }

            public sealed class ArrayForceNonBlocking : SendToBase<SocketHelperSyncForceNonBlocking>
            {
                public ArrayForceNonBlocking(ITestOutputHelper output) : base(output) {}
            }
        }

        public static class Async
        {
            public sealed class Apm : SendToBase<SocketHelperApm>
            {
                public Apm(ITestOutputHelper output) : base(output) {}
            }

            public sealed class Task : SendToBase<SocketHelperTask>
            {
                public Task(ITestOutputHelper output) : base(output) {}
            }

            public sealed class Eap : SendToBase<SocketHelperEap>
            {
                public Eap(ITestOutputHelper output) : base(output) {}
            }
        }
    }
}
