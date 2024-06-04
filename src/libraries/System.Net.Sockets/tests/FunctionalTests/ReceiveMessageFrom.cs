// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Net.Sockets.Tests
{
    public abstract class ReceiveMessageFrom<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected static Socket CreateSocket(AddressFamily addressFamily = AddressFamily.InterNetwork) => new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);

        protected static IPEndPoint GetGetDummyTestEndpoint(AddressFamily addressFamily = AddressFamily.InterNetwork) =>
            addressFamily == AddressFamily.InterNetwork ? new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234) : new IPEndPoint(IPAddress.Parse("1:2:3::4"), 1234);

        protected static readonly TimeSpan CancellationTestTimeout = TimeSpan.FromSeconds(30);

        protected ReceiveMessageFrom(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(1, -1, 0)] // offset low
        [InlineData(1, 2, 0)] // offset high
        [InlineData(1, 0, -1)] // count low
        [InlineData(1, 0, 2)] // count high
        [InlineData(1, 1, 1)] // count high
        public async Task OutOfRange_Throws_ArgumentOutOfRangeException(int length, int offset, int count)
        {
            using Socket socket = CreateSocket();

            ArraySegment<byte> buffer = new FakeArraySegment
            {
                Array = new byte[length],
                Count = count,
                Offset = offset
            }.ToActual();

            await AssertThrowsSynchronously<ArgumentOutOfRangeException>(() => ReceiveMessageFromAsync(socket, buffer, GetGetDummyTestEndpoint()));
        }

        [Fact]
        public async Task NullBuffer_Throws_ArgumentNullException()
        {
            if (!ValidatesArrayArguments) return;
            using Socket socket = CreateSocket();
            await AssertThrowsSynchronously<ArgumentNullException>(() => ReceiveMessageFromAsync(socket, null, GetGetDummyTestEndpoint()));
        }

        [Fact]
        public async Task NullEndpoint_Throws_ArgumentException()
        {
            using Socket socket = CreateSocket();
            if (UsesEap)
            {
                await AssertThrowsSynchronously<ArgumentException>(() => ReceiveMessageFromAsync(socket, new byte[1], null));
            }
            else
            {
                await AssertThrowsSynchronously<ArgumentNullException>(() => ReceiveMessageFromAsync(socket, new byte[1], null));
            }
        }

        [Fact]
        public async Task AddressFamilyDoesNotMatch_Throws_ArgumentException()
        {
            using var ipv4Socket = CreateSocket();
            EndPoint ipV6Endpoint = GetGetDummyTestEndpoint(AddressFamily.InterNetworkV6);
            await AssertThrowsSynchronously<ArgumentException>(() => ReceiveMessageFromAsync(ipv4Socket, new byte[1], ipV6Endpoint));
        }

        [Fact]
        public async Task NotBound_Throws_InvalidOperationException()
        {
            // ReceiveFromAsync(saea) fails on a Debug.Assert():
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/47714")]
            if (UsesEap) return;

            using Socket socket = CreateSocket();
            await AssertThrowsSynchronously<InvalidOperationException>(() => ReceiveMessageFromAsync(socket, new byte[1], GetGetDummyTestEndpoint()));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveSent_TCP_Success(bool ipv6)
        {
            if (ipv6 && PlatformDetection.IsApplePlatform)
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
                var r = await ReceiveMessageFromAsync(receiver, receiveBuffer, sender.LocalEndPoint);
                Assert.Equal(3, r.ReceivedBytes);
                AssertExtensions.SequenceEqual(sendBuffer, receiveBuffer);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveSent_UDP_Success(bool ipv4)
        {
            const int Offset = 10;
            const int DatagramSize = 256;
            const int DatagramsToSend = 16;

            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            using Socket receiver = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            receiver.SetSocketOption(ipv4 ? SocketOptionLevel.IP : SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
            ConfigureNonBlocking(sender);
            ConfigureNonBlocking(receiver);

            receiver.BindToAnonymousPort(address);
            sender.BindToAnonymousPort(address);

            byte[] sendBuffer = new byte[DatagramSize];
            var receiveInternalBuffer = new byte[DatagramSize + Offset];
            var emptyBuffer = new byte[Offset];
            ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(receiveInternalBuffer, Offset, DatagramSize);

            Random rnd = new Random(0);

            IPEndPoint remoteEp = new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, 0);

            for (int i = 0; i < DatagramsToSend; i++)
            {
                rnd.NextBytes(sendBuffer);
                sender.SendTo(sendBuffer, receiver.LocalEndPoint);

                SocketReceiveMessageFromResult result = await ReceiveMessageFromAsync(receiver, receiveBuffer, remoteEp);
                IPPacketInformation packetInformation = result.PacketInformation;

                Assert.Equal(DatagramSize, result.ReceivedBytes);
                AssertExtensions.SequenceEqual(emptyBuffer, new ReadOnlySpan<byte>(receiveInternalBuffer, 0, Offset));
                AssertExtensions.SequenceEqual(sendBuffer, new ReadOnlySpan<byte>(receiveInternalBuffer, Offset, DatagramSize));
                Assert.Equal(sender.LocalEndPoint, result.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);
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

            await Assert.ThrowsAsync<ObjectDisposedException>(() => ReceiveMessageFromAsync(socket, new byte[1], GetGetDummyTestEndpoint()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task ClosedDuringOperation_Throws_ObjectDisposedExceptionOrSocketException(bool closeOrDispose)
        {
            if (UsesSync && PlatformDetection.IsOSX)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/47342")]
                // On Mac, Close/Dispose hangs when invoked concurrently with a blocking UDP receive.
                return;
            }

            int msDelay = 100;
            if (UsesSync)
            {
                // In sync case Dispose may happen before the operation is started,
                // in that case we would see an ObjectDisposedException instead of a SocketException.
                // We may need to try the run a couple of times to deal with the timing race.
                await RetryHelper.ExecuteAsync(() => RunTestAsync(), maxAttempts: 10, retryWhen: e => e is XunitException);
            }
            else
            {
                await RunTestAsync();
            }

            async Task RunTestAsync()
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.BindToAnonymousPort(IPAddress.Any);

                Task receiveTask = ReceiveMessageFromAsync(socket, new byte[1], GetGetDummyTestEndpoint());
                await Task.Delay(msDelay);
                msDelay *= 2;
                if (closeOrDispose) socket.Close();
                else socket.Dispose();

                SocketException ex = await Assert.ThrowsAsync<SocketException>(() => receiveTask).WaitAsync(CancellationTestTimeout);
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

            SocketException exception = await Assert.ThrowsAnyAsync<SocketException>(() => ReceiveMessageFromAsync(socket, new byte[1], GetGetDummyTestEndpoint()))
                .WaitAsync(CancellationTestTimeout);

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

            var r = await ReceiveMessageFromAsync(receiver, new byte[1], sender.LocalEndPoint);
            Assert.Equal(1, r.ReceivedBytes);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // MSG_BCAST is Windows-specifc
        public async Task SendBroadcast_BroadcastFlagIsSetOnReceive()
        {
            using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ConfigureNonBlocking(receiver);
            int receiverPort = receiver.BindToAnonymousPort(IPAddress.Loopback);

            using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            int senderPort = sender.BindToAnonymousPort(IPAddress.Loopback);
            var destEp = new IPEndPoint(IPAddress.Parse("127.255.255.255"), receiverPort);

            sender.SendTo(new byte[1], destEp);

            var sourceEp = new IPEndPoint(IPAddress.Any, senderPort);
            var r = await ReceiveMessageFromAsync(receiver, new byte[1], sourceEp);
            Assert.Equal(SocketFlags.Broadcast, r.SocketFlags);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Windows doesn't report MSG_TRUNC
        public async Task ReceiveTruncated_TruncatedFlagIsSetOnReceive()
        {
            using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.BindToAnonymousPort(IPAddress.Loopback);
            using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sender.SendTo(new byte[2], receiver.LocalEndPoint);

            var r = await ReceiveMessageFromAsync(receiver, new byte[1], sender.LocalEndPoint);
            Assert.Equal(SocketFlags.Truncated, r.SocketFlags);
        }
    }

    public sealed class ReceiveMessageFrom_Sync : ReceiveMessageFrom<SocketHelperArraySync>
    {
        public ReceiveMessageFrom_Sync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_SyncForceNonBlocking : ReceiveMessageFrom<SocketHelperSyncForceNonBlocking>
    {
        public ReceiveMessageFrom_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_Apm : ReceiveMessageFrom<SocketHelperApm>
    {
        public ReceiveMessageFrom_Apm(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void EndReceiveMessageFrom_NullAsyncResult_Throws_ArgumentNullException()
        {
            SocketFlags socketFlags = SocketFlags.None;
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            using Socket socket = CreateSocket();

            Assert.Throws<ArgumentNullException>(() => socket.EndReceiveMessageFrom(null, ref socketFlags, ref endpoint, out _));
        }

        [Fact]
        public void EndReceiveMessageFrom_UnrelatedAsyncResult_Throws_ArgumentException()
        {
            SocketFlags socketFlags = SocketFlags.None;
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            using Socket socket = CreateSocket();

            Assert.Throws<ArgumentException>(() => socket.EndReceiveMessageFrom(Task.CompletedTask, ref socketFlags, ref endpoint, out _));
        }

        [Fact]
        public void EndReceiveMessageFrom_NullEndPoint_Throws_ArgumentNullException()
        {
            SocketFlags socketFlags = SocketFlags.None;
            EndPoint validEndPoint = new IPEndPoint(IPAddress.Loopback, 1);
            EndPoint invalidEndPoint = null;
            using Socket socket = CreateSocket();
            socket.BindToAnonymousPort(IPAddress.Loopback);
            IAsyncResult iar = socket.BeginReceiveMessageFrom(new byte[1], 0, 1, SocketFlags.None, ref validEndPoint, null, null);

            Assert.Throws<ArgumentNullException>("endPoint", () => socket.EndReceiveMessageFrom(iar, ref socketFlags, ref invalidEndPoint, out _));
        }

        [Fact]
        public void EndReceiveMessageFrom_AddressFamilyDoesNotMatch_Throws_ArgumentException()
        {
            SocketFlags socketFlags = SocketFlags.None;
            EndPoint validEndPoint = new IPEndPoint(IPAddress.Loopback, 1);
            EndPoint invalidEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            using Socket socket = CreateSocket();
            socket.BindToAnonymousPort(IPAddress.Loopback);
            IAsyncResult iar = socket.BeginReceiveMessageFrom(new byte[1], 0, 1, SocketFlags.None, ref validEndPoint, null, null);

            Assert.Throws<ArgumentException>("endPoint", () => socket.EndReceiveMessageFrom(iar, ref socketFlags, ref invalidEndPoint, out _));
        }

        [Fact]
        public void BeginReceiveMessageFrom_RemoteEpIsReturnedWhenCompletedSynchronously()
        {
            EndPoint anyEp = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEp = anyEp;
            using Socket receiver = CreateSocket();
            receiver.BindToAnonymousPort(IPAddress.Loopback);
            using Socket sender = CreateSocket();
            sender.BindToAnonymousPort(IPAddress.Loopback);

            sender.SendTo(new byte[1], receiver.LocalEndPoint);

            IAsyncResult iar = receiver.BeginReceiveMessageFrom(new byte[1], 0, 1, SocketFlags.None, ref remoteEp, null, null);
            if (iar.CompletedSynchronously)
            {
                _output.WriteLine("Completed synchronously, updated endpoint.");
                Assert.Equal(sender.LocalEndPoint, remoteEp);
            }
            else
            {
                _output.WriteLine("Completed asynchronously, did not update endPoint");
                Assert.Equal(anyEp, remoteEp);
            }
        }
    }

    public sealed class ReceiveMessageFrom_Task : ReceiveMessageFrom<SocketHelperTask>
    {
        public ReceiveMessageFrom_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_CancellableTask : ReceiveMessageFrom<SocketHelperCancellableTask>
    {
        public ReceiveMessageFrom_CancellableTask(ITestOutputHelper output) : base(output) { }

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
                () => socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, dummy.LocalEndPoint, cts.Token).AsTask())
                .WaitAsync(CancellationTestTimeout);
            Assert.Equal(cts.Token, ex.CancellationToken);
        }
    }

    public sealed class ReceiveMessageFrom_Eap : ReceiveMessageFrom<SocketHelperEap>
    {
        public ReceiveMessageFrom_Eap(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ReceiveFromAsync_NullAsyncEventArgs_Throws_ArgumentNullException()
        {
            using Socket socket = CreateSocket();
            Assert.Throws<ArgumentNullException>(() => socket.ReceiveMessageFromAsync(null));
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 2)]
        public void ReceiveSentMessages_ReuseEventArgs_Success(bool ipv4, int bufferMode)
        {
            const int DatagramsToSend = 5;
            const int TimeoutMs = 30_000;

            AddressFamily family;
            IPAddress loopback, any;
            SocketOptionLevel level;
            if (ipv4)
            {
                family = AddressFamily.InterNetwork;
                loopback = IPAddress.Loopback;
                any = IPAddress.Any;
                level = SocketOptionLevel.IP;
            }
            else
            {
                family = AddressFamily.InterNetworkV6;
                loopback = IPAddress.IPv6Loopback;
                any = IPAddress.IPv6Any;
                level = SocketOptionLevel.IPv6;
            }

            using var receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
            using var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
            using var saea = new SocketAsyncEventArgs();
            var completed = new ManualResetEventSlim();
            saea.Completed += delegate { completed.Set(); };

            int port = receiver.BindToAnonymousPort(loopback);
            receiver.SetSocketOption(level, SocketOptionName.PacketInformation, true);
            sender.Bind(new IPEndPoint(loopback, 0));
            saea.RemoteEndPoint = new IPEndPoint(any, 0);

            Random random = new Random(0);
            byte[] sendBuffer = new byte[1024];
            random.NextBytes(sendBuffer);

            for (int i = 0; i < DatagramsToSend; i++)
            {
                byte[] receiveBuffer = new byte[1024];
                switch (bufferMode)
                {
                    case 0: // single buffer
                        saea.SetBuffer(receiveBuffer, 0, 1024);
                        break;
                    case 1: // single buffer in buffer list
                        saea.BufferList = new List<ArraySegment<byte>>
                        {
                            new ArraySegment<byte>(receiveBuffer)
                        };
                        break;
                    case 2: // multiple buffers in buffer list
                        saea.BufferList = new List<ArraySegment<byte>>
                        {
                            new ArraySegment<byte>(receiveBuffer, 0, 512),
                            new ArraySegment<byte>(receiveBuffer, 512, 512)
                        };
                        break;
                }

                bool pending = receiver.ReceiveMessageFromAsync(saea);
                sender.SendTo(sendBuffer, new IPEndPoint(loopback, port));
                if (pending) Assert.True(completed.Wait(TimeoutMs), "Expected operation to complete within timeout");
                completed.Reset();

                Assert.Equal(1024, saea.BytesTransferred);
                AssertExtensions.SequenceEqual(sendBuffer, receiveBuffer);
                Assert.Equal(sender.LocalEndPoint, saea.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, saea.ReceiveMessageFromPacketInfo.Address);
            }
        }
    }

    public sealed class ReceiveMessageFrom_SpanSync : ReceiveMessageFrom<SocketHelperSpanSync>
    {
        public ReceiveMessageFrom_SpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_SpanSyncForceNonBlocking : ReceiveMessageFrom<SocketHelperSpanSyncForceNonBlocking>
    {
        public ReceiveMessageFrom_SpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_MemoryArrayTask : ReceiveMessageFrom<SocketHelperMemoryArrayTask>
    {
        public ReceiveMessageFrom_MemoryArrayTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_MemoryNativeTask : ReceiveMessageFrom<SocketHelperMemoryNativeTask>
    {
        public ReceiveMessageFrom_MemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
