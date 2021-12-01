// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public sealed class SendReceiveUdpClient : MemberDatas
    {
        [OuterLoop]
        [Theory]
        [MemberData(nameof(LoopbacksAndUseMemory))]
        public async Task SendToRecvFromAsync_Datagram_UDP_UdpClient(IPAddress loopbackAddress, bool useMemoryOverload)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 20000;
            const int TestTimeout = 60000;

            using (var left = new UdpClient(new IPEndPoint(leftAddress, 0)))
            using (var right = new UdpClient(new IPEndPoint(rightAddress, 0)))
            {
                var leftEndpoint = (IPEndPoint)left.Client.LocalEndPoint;
                var rightEndpoint = (IPEndPoint)right.Client.LocalEndPoint;

                var receiverAck = new ManualResetEventSlim();
                var senderAck = new ManualResetEventSlim();

                var receivedChecksums = new uint?[DatagramsToSend];
                int receivedDatagrams = 0;

                Task receiverTask = Task.Run(async () =>
                {
                    for (; receivedDatagrams < DatagramsToSend; receivedDatagrams++)
                    {
                        UdpReceiveResult result = await left.ReceiveAsync();

                        receiverAck.Set();
                        Assert.True(senderAck.Wait(AckTimeout));
                        senderAck.Reset();

                        Assert.Equal(DatagramSize, result.Buffer.Length);
                        Assert.Equal(rightEndpoint, result.RemoteEndPoint);

                        int datagramId = (int)result.Buffer[0];
                        Assert.Null(receivedChecksums[datagramId]);

                        receivedChecksums[datagramId] = Fletcher32.Checksum(result.Buffer, 0, result.Buffer.Length);
                    }
                });

                var sentChecksums = new uint[DatagramsToSend];
                int sentDatagrams = 0;

                Task senderTask = Task.Run(async () =>
                {
                    var random = new Random();
                    var sendBuffer = new byte[DatagramSize];

                    for (; sentDatagrams < DatagramsToSend; sentDatagrams++)
                    {
                        random.NextBytes(sendBuffer);
                        sendBuffer[0] = (byte)sentDatagrams;

                        int sent = useMemoryOverload ? await right.SendAsync(new ReadOnlyMemory<byte>(sendBuffer), leftEndpoint) : await right.SendAsync(sendBuffer, DatagramSize, leftEndpoint);

                        Assert.True(receiverAck.Wait(AckTimeout));
                        receiverAck.Reset();
                        senderAck.Set();

                        Assert.Equal(DatagramSize, sent);
                        sentChecksums[sentDatagrams] = Fletcher32.Checksum(sendBuffer, 0, sent);
                    }
                });

                await (new[] { receiverTask, senderTask }).WhenAllOrAnyFailed(TestTimeout);
                for (int i = 0; i < DatagramsToSend; i++)
                {
                    Assert.NotNull(receivedChecksums[i]);
                    Assert.Equal(sentChecksums[i], (uint)receivedChecksums[i]);
                }
            }
        }

        public static readonly object[][] LoopbacksAndUseMemory = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };
    }
}
