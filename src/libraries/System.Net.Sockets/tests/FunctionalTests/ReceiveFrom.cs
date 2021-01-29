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
        protected static IPEndPoint GetGetDummyTestEndpoint(AddressFamily addressFamily = AddressFamily.InterNetwork) =>
            addressFamily == AddressFamily.InterNetwork ? new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234) : new IPEndPoint(IPAddress.Parse("1:2:3::4"), 1234);

        protected static readonly TimeSpan CancellationTestTimeout = TimeSpan.FromSeconds(30);

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

            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => ReceiveFromAsync(socket, buffer, GetGetDummyTestEndpoint()));
        }

        [Fact]
        public async Task NullBuffer_Throws()
        {
            if (!ValidatesArrayArguments) return;
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAsync<ArgumentNullException>(() => ReceiveFromAsync(socket, null, GetGetDummyTestEndpoint()));
        }

        [Fact]
        public async Task NullEndpoint_Throws()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Assert.ThrowsAnyAsync<ArgumentException>(() => ReceiveFromAsync(socket, new byte[1], null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveSent_TCP_Success(bool ipv6)
        {
            if (ipv6 && PlatformDetection.IsOSX)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/47335")]
                // accept() will create a (seemingly) DualMode socket on Mac,
                // but since recvmsg() does not work with DualMode on that OS, we throw PNSE CheckDualModeReceiveSupport().
                // Weirdly, the flag is readable, but an attempt to write it leads to EINVAL.
                // The best option seems to be to skip this test for the Mac+IPV6 case
                return;
            }

            (Socket sender, Socket receiver) = SocketTestExtensions.CreateConnectedSocketPair(ipv6);
            using (sender)
            using (receiver)
            {
                byte[] sendBuffer = { 1, 2, 3 };
                sender.Send(sendBuffer);

                byte[] receiveBuffer = new byte[3];
                var r = await ReceiveFromAsync(receiver, receiveBuffer, sender.LocalEndPoint);
                Assert.Equal(3, r.ReceivedBytes);
                AssertExtensions.SequenceEqual(sendBuffer, receiveBuffer);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ClosedBeforeOperation_Throws_ObjectDisposedException(bool closeOrDispose)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.BindToAnonymousPort(IPAddress.Any);
            if (closeOrDispose) socket.Close();
            else socket.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => ReceiveFromAsync(socket, new byte[1], GetGetDummyTestEndpoint()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/47561")]
        public async Task ClosedDuringOperation_Throws_ObjectDisposedExceptionOrSocketException(bool closeOrDispose)
        {
            if (UsesSync && PlatformDetection.IsOSX)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/47342")]
                // On Mac, Close/Dispose hangs when invoked concurrently with a blocking UDP receive.
                return;
            }

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.BindToAnonymousPort(IPAddress.Any);

            Task receiveTask = ReceiveFromAsync(socket, new byte[1], GetGetDummyTestEndpoint());
            await Task.Delay(100);
            if (closeOrDispose) socket.Close();
            else socket.Dispose();

            if (UsesApm)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => receiveTask)
                    .TimeoutAfter(CancellationTestTimeout);
            }
            else
            {
                SocketException ex = await Assert.ThrowsAsync<SocketException>(() => receiveTask)
                    .TimeoutAfter(CancellationTestTimeout);
                SocketError expectedError = UsesSync ? SocketError.Interrupted : SocketError.OperationAborted;
                Assert.Equal(expectedError, ex.SocketErrorCode);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // It's allowed to shutdown() UDP sockets on Windows, however on Unix this will lead to ENOTCONN
        [Theory]
        [InlineData(SocketShutdown.Both)]
        [InlineData(SocketShutdown.Receive)]
        public async Task ShutdownReceiveBeforeOperation_ThrowsSocketException(SocketShutdown shutdown)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.BindToAnonymousPort(IPAddress.Any);
            socket.Shutdown(shutdown);

            // [ActiveIssue("https://github.com/dotnet/runtime/issues/47469")]
            // Shutdown(Both) does not seem to take immediate effect for Receive(Message)From in a consistent manner, trying to workaround with a delay:
            if (shutdown == SocketShutdown.Both) await Task.Delay(50);

            SocketException exception = await Assert.ThrowsAnyAsync<SocketException>(() => ReceiveFromAsync(socket, new byte[1], GetGetDummyTestEndpoint()))
                .TimeoutAfter(CancellationTestTimeout);

            Assert.Equal(SocketError.Shutdown, exception.SocketErrorCode);
        }

        [PlatformSpecific(TestPlatforms.Windows)] // It's allowed to shutdown() UDP sockets on Windows, however on Unix this will lead to ENOTCONN
        [Fact]
        public async Task ShutdownSend_ReceiveFromShouldSucceed()
        {
            using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.BindToAnonymousPort(IPAddress.Loopback);
            receiver.Shutdown(SocketShutdown.Send);

            using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.BindToAnonymousPort(IPAddress.Loopback);
            sender.SendTo(new byte[1], receiver.LocalEndPoint);

            var r = await ReceiveFromAsync(receiver, new byte[1], sender.LocalEndPoint);
            Assert.Equal(1, r.ReceivedBytes);
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

        [Theory]
        [MemberData(nameof(LoopbacksAndBuffers))]
        public async Task WhenCanceled_Throws(IPAddress loopback, bool precanceled)
        {
            using var socket = new Socket(loopback.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using var dummy = new Socket(loopback.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.BindToAnonymousPort(loopback);
            dummy.BindToAnonymousPort(loopback);
            Memory<byte> buffer = new byte[1];

            CancellationTokenSource cts = new CancellationTokenSource();
            if (precanceled) cts.Cancel();
            else cts.CancelAfter(100);

            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => socket.ReceiveFromAsync(buffer, SocketFlags.None, dummy.LocalEndPoint, cts.Token).AsTask())
                .TimeoutAfter(CancellationTestTimeout);
            Assert.Equal(cts.Token, ex.CancellationToken);
        }
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
