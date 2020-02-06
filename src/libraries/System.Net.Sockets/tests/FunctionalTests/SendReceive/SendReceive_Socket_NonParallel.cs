// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests.SendReceive
{
    [Collection(nameof(NoParallelTests))]
    public abstract class SendReceive_Socket_NonParallel<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public SendReceive_Socket_NonParallel(ITestOutputHelper output) : base(output) {}

        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task SendToRecvFrom_Datagram_UDP(IPAddress loopbackAddress)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 10000;
            const int TestTimeout = 30000;

            var left = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            left.BindToAnonymousPort(leftAddress);

            var right = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            right.BindToAnonymousPort(rightAddress);

            var leftEndpoint = (IPEndPoint)left.LocalEndPoint;
            var rightEndpoint = (IPEndPoint)right.LocalEndPoint;

            var receiverAck = new SemaphoreSlim(0);
            var senderAck = new SemaphoreSlim(0);

            _output.WriteLine($"{DateTime.Now}: Sending data from {rightEndpoint} to {leftEndpoint}");

            var receivedChecksums = new uint?[DatagramsToSend];
            Task leftThread = Task.Run(async () =>
            {
                using (left)
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

    public class SendReceive_Socket_NonParallel
    {
        public class Sync
        {
            public sealed class Array : SendReceive_Socket_NonParallel<SocketHelperArraySync>
            {
                public Array(ITestOutputHelper output) : base(output) { }
            }

            public sealed class Array_ForceNonBlocking : SendReceive_Socket_NonParallel<SocketHelperSyncForceNonBlocking>
            {
                public Array_ForceNonBlocking(ITestOutputHelper output) : base(output) {}
            }

            public sealed class Span : SendReceive_Socket_NonParallel<SocketHelperSpanSync>
            {
                public Span(ITestOutputHelper output) : base(output) { }
            }

            public sealed class Span_ForceNonBlocking : SendReceive_Socket_NonParallel<SocketHelperSpanSyncForceNonBlocking>
            {
                public Span_ForceNonBlocking(ITestOutputHelper output) : base(output) { }
            }
        }

        public class Async
        {
            public sealed class Apm : SendReceive_Socket_NonParallel<SocketHelperApm>
            {
                public Apm(ITestOutputHelper output) : base(output) {}
            }

            public sealed class Task : SendReceive_Socket_NonParallel<SocketHelperTask>
            {
                public Task(ITestOutputHelper output) : base(output) {}
            }

            public sealed class Eap : SendReceive_Socket_NonParallel<SocketHelperEap>
            {
                public Eap(ITestOutputHelper output) : base(output) {}
            }

            public sealed class MemoryArrayTask : SendReceive_Socket_NonParallel<SocketHelperMemoryArrayTask>
            {
                public MemoryArrayTask(ITestOutputHelper output) : base(output) { }
            }

            public sealed class MemoryNativeTask : SendReceive_Socket_NonParallel<SocketHelperMemoryNativeTask>
            {
                public MemoryNativeTask(ITestOutputHelper output) : base(output) { }
            }
        }
    }


















}
