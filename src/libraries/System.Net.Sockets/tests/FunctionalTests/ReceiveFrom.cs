// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class ReceiveFrom<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        private static readonly IPEndPoint ValidUdpRemoteEndpoint = new IPEndPoint(IPAddress.Parse("10.20.30.40"), 1234);

        protected ReceiveFrom(ITestOutputHelper output) : base(output) { }

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
                Array = new byte[length],
                Count = count,
                Offset = offset
            }.ToActual();

            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => ReceiveFromAsync(socket, buffer, ValidUdpRemoteEndpoint));
        }

        [Fact]
        public async Task NullBuffer_Throws()
        {
            if (!ValidatesArrayArguments) return;
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAsync<ArgumentNullException>(() => ReceiveFromAsync(socket, null, ValidUdpRemoteEndpoint));
        }

        [Fact]
        public async Task NullEndpoint_Throws()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAnyAsync<ArgumentException>(() => ReceiveFromAsync(socket, new byte[1], null));
        }
    }

    public sealed class ReceiveFrom_Sync : ReceiveFrom<SocketHelperArraySync>
    {
        public ReceiveFrom_Sync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_SyncForceNonBlocking : ReceiveFrom<SocketHelperSyncForceNonBlocking>
    {
        public ReceiveFrom_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_Apm : ReceiveFrom<SocketHelperApm>
    {
        public ReceiveFrom_Apm(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_Task : ReceiveFrom<SocketHelperTask>
    {
        public ReceiveFrom_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_CancellableTask : ReceiveFrom<SocketHelperCancellableTask>
    {
        public ReceiveFrom_CancellableTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_Eap : ReceiveFrom<SocketHelperEap>
    {
        public ReceiveFrom_Eap(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_SpanSync : ReceiveFrom<SocketHelperSpanSync>
    {
        public ReceiveFrom_SpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_SpanSyncForceNonBlocking : ReceiveFrom<SocketHelperSpanSyncForceNonBlocking>
    {
        public ReceiveFrom_SpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_MemoryArrayTask : ReceiveFrom<SocketHelperMemoryArrayTask>
    {
        public ReceiveFrom_MemoryArrayTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveFrom_MemoryNativeTask : ReceiveFrom<SocketHelperMemoryNativeTask>
    {
        public ReceiveFrom_MemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
