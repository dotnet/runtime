// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    [Collection(nameof(DisableParallelization))]
    public abstract class SendReceiveNonParallel<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public SendReceiveNonParallel(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> LoopbackWithBool =>
            from addr in Loopbacks
            from b in new[] { false, true }
            select new object[] { addr[0], b };

        [OuterLoop("Serial execution of all variants takes long")]
        [Theory]
        [MemberData(nameof(LoopbackWithBool))]
        public async Task SendToRecvFrom_Datagram_UDP(IPAddress loopbackAddress, bool useClone)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int ReceiverAckTimeout = 5000;
            const int SenderAckTimeout = 10000;

            using var origLeft = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using var origRight = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            origLeft.BindToAnonymousPort(leftAddress);
            origRight.BindToAnonymousPort(rightAddress);

            using var left = useClone ? new Socket(origLeft.SafeHandle) : origLeft;
            using var right = useClone ? new Socket(origRight.SafeHandle) : origRight;

            // Force non-blocking mode in ...SyncForceNonBlocking variants of the test: 
            ConfigureNonBlocking(left);
            ConfigureNonBlocking(right);

            var leftEndpoint = (IPEndPoint)left.LocalEndPoint;
            var rightEndpoint = (IPEndPoint)right.LocalEndPoint;

            var receiverAck = new SemaphoreSlim(0);
            var senderAck = new SemaphoreSlim(0);

            _output.WriteLine($"{DateTime.Now}: Sending data from {rightEndpoint} to {leftEndpoint}");

            var receivedChecksums = new uint?[DatagramsToSend];
            Task leftThread = Task.Run(async () =>
            {
                EndPoint remote = leftEndpoint.Create(leftEndpoint.Serialize());
                var recvBuffer = new byte[DatagramSize];
                for (int i = 0; i < DatagramsToSend; i++)
                {
                    SocketReceiveFromResult result = await ReceiveFromAsync(
                        left, new ArraySegment<byte>(recvBuffer), remote);
                    Assert.Equal(DatagramSize, result.ReceivedBytes);
                    Assert.Equal(rightEndpoint, result.RemoteEndPoint);

                    int datagramId = recvBuffer[0];
                    Assert.Null(receivedChecksums[datagramId]);
                    receivedChecksums[datagramId] = Fletcher32.Checksum(recvBuffer, 0, result.ReceivedBytes);

                    receiverAck.Release();
                    bool gotAck = await senderAck.WaitAsync(SenderAckTimeout);
                    Assert.True(gotAck, $"{DateTime.Now}: Timeout waiting {SenderAckTimeout} for senderAck in iteration {i}");
                }
            });

            var sentChecksums = new uint[DatagramsToSend];
            using (right)
            {
                var random = new Random();
                var sendBuffer = new byte[DatagramSize];
                for (int i = 0; i < DatagramsToSend; i++)
                {
                    random.NextBytes(sendBuffer);
                    sendBuffer[0] = (byte)i;

                    int sent = await SendToAsync(right, new ArraySegment<byte>(sendBuffer), leftEndpoint);

                    bool gotAck = await receiverAck.WaitAsync(ReceiverAckTimeout);
                    Assert.True(gotAck, $"{DateTime.Now}: Timeout waiting {ReceiverAckTimeout} for receiverAck in iteration {i} after sending {sent}. Receiver is in {leftThread.Status}");
                    senderAck.Release();

                    Assert.Equal(DatagramSize, sent);
                    sentChecksums[i] = Fletcher32.Checksum(sendBuffer, 0, sent);
                }
            }

            await leftThread;
            for (int i = 0; i < DatagramsToSend; i++)
            {
                Assert.NotNull(receivedChecksums[i]);
                Assert.Equal(sentChecksums[i], (uint)receivedChecksums[i]);
            }
        }
    }

    public sealed class SendReceiveNonParallel_Sync : SendReceiveNonParallel<SocketHelperArraySync>
    {
        public SendReceiveNonParallel_Sync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_SyncForceNonBlocking : SendReceiveNonParallel<SocketHelperSyncForceNonBlocking>
    {
        public SendReceiveNonParallel_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_Apm : SendReceiveNonParallel<SocketHelperApm>
    {
        public SendReceiveNonParallel_Apm(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_Task : SendReceiveNonParallel<SocketHelperTask>
    {
        public SendReceiveNonParallel_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_CancellableTask : SendReceiveNonParallel<SocketHelperCancellableTask>
    {
        public SendReceiveNonParallel_CancellableTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_Eap : SendReceiveNonParallel<SocketHelperEap>
    {
        public SendReceiveNonParallel_Eap(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_SpanSync : SendReceiveNonParallel<SocketHelperSpanSync>
    {
        public SendReceiveNonParallel_SpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_SpanSyncForceNonBlocking : SendReceiveNonParallel<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendReceiveNonParallel_SpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_MemoryArrayTask : SendReceiveNonParallel<SocketHelperMemoryArrayTask>
    {
        public SendReceiveNonParallel_MemoryArrayTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceiveNonParallel_MemoryNativeTask : SendReceiveNonParallel<SocketHelperMemoryNativeTask>
    {
        public SendReceiveNonParallel_MemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
