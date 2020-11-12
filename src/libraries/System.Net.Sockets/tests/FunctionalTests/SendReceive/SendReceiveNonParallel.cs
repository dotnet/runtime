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
    public abstract class SendReceiveNonParallel<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public SendReceiveNonParallel(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> LoopbackWithBool =>
            from addr in Loopbacks
            from b in new[] { false, true }
            select new object[] { addr[0], b };

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1712")]
        [OuterLoop]
        [Theory]
        [MemberData(nameof(LoopbackWithBool))]
        public async Task SendToRecvFrom_Datagram_UDP(IPAddress loopbackAddress, bool useClone)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 10000;
            const int TestTimeout = 30000;

            using var origLeft = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using var origRight = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            origLeft.BindToAnonymousPort(leftAddress);
            origRight.BindToAnonymousPort(rightAddress);

            using var left = useClone ? new Socket(origLeft.SafeHandle) : origLeft;
            using var right = useClone ? new Socket(origRight.SafeHandle) : origRight;

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
                    bool gotAck = await senderAck.WaitAsync(TestTimeout);
                    Assert.True(gotAck, $"{DateTime.Now}: Timeout waiting {TestTimeout} for senderAck in iteration {i}");
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

                    bool gotAck = await receiverAck.WaitAsync(AckTimeout);
                    Assert.True(gotAck, $"{DateTime.Now}: Timeout waiting {AckTimeout} for receiverAck in iteration {i} after sending {sent}. Receiver is in {leftThread.Status}");
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
}
