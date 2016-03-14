// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class SendReceive
    {
        private readonly ITestOutputHelper _log;

        public SendReceive(ITestOutputHelper output)
        {
            _log = output;
        }

        public static readonly object[][] SendToRecvFromAsync_Datagram_UDP_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, IPAddress.IPv6Loopback },
            new object[] { IPAddress.Loopback, IPAddress.Loopback }
        };

        [Theory]
        [MemberData(nameof(SendToRecvFromAsync_Datagram_UDP_MemberData))]
        public void SendToRecvFromAsync_Datagram_UDP(IPAddress leftAddress, IPAddress rightAddress)
        {
            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 1000;
            const int TestTimeout = 30000;

            var left = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            var leftEventArgs = new SocketAsyncEventArgs();
            left.BindToAnonymousPort(leftAddress);

            var right = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            var rightEventArgs = new SocketAsyncEventArgs();
            right.BindToAnonymousPort(rightAddress);

            var leftEndpoint = (IPEndPoint)left.LocalEndPoint;
            var rightEndpoint = (IPEndPoint)right.LocalEndPoint;

            var receiverAck = new ManualResetEventSlim();
            var senderAck = new ManualResetEventSlim();

            EndPoint receiveRemote = leftEndpoint.Create(leftEndpoint.Serialize());
            var receiverFinished = new TaskCompletionSource<bool>();
            var receivedChecksums = new uint?[DatagramsToSend];
            var receiveBuffer = new byte[DatagramSize];
            int receivedDatagrams = -1;

            Action<int, EndPoint> receiveHandler = null;
            receiveHandler = (received, remote) =>
            {
                try
                {
                    if (receivedDatagrams != -1)
                    {
                        Assert.Equal(DatagramSize, received);
                        Assert.Equal(rightEndpoint, remote);

                        int datagramId = (int)receiveBuffer[0];
                        Assert.Null(receivedChecksums[datagramId]);

                        receivedChecksums[datagramId] = Fletcher32.Checksum(receiveBuffer, 0, received);

                        receiverAck.Set();
                        Assert.True(senderAck.Wait(AckTimeout));
                        senderAck.Reset();

                        receivedDatagrams++;
                        if (receivedDatagrams == DatagramsToSend)
                        {
                            left.Dispose();
                            receiverFinished.SetResult(true);
                            return;
                        }
                    }
                    else
                    {
                        receivedDatagrams = 0;
                    }

                    left.ReceiveFromAsync(leftEventArgs, receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, receiveRemote, receiveHandler);
                }
                catch (Exception ex)
                {
                    receiverFinished.SetException(ex);
                }
            };

            receiveHandler(0, null);

            var random = new Random();
            var senderFinished = new TaskCompletionSource<bool>();
            var sentChecksums = new uint[DatagramsToSend];
            var sendBuffer = new byte[DatagramSize];
            int sentDatagrams = -1;

            Action<int> sendHandler = null;
            sendHandler = sent =>
            {
                try
                {
                    if (sentDatagrams != -1)
                    {
                        Assert.True(receiverAck.Wait(AckTimeout));
                        receiverAck.Reset();
                        senderAck.Set();

                        Assert.Equal(DatagramSize, sent);
                        sentChecksums[sentDatagrams] = Fletcher32.Checksum(sendBuffer, 0, sent);

                        sentDatagrams++;
                        if (sentDatagrams == DatagramsToSend)
                        {
                            right.Dispose();
                            senderFinished.SetResult(true);
                            return;
                        }
                    }
                    else
                    {
                        sentDatagrams = 0;
                    }

                    random.NextBytes(sendBuffer);
                    sendBuffer[0] = (byte)sentDatagrams;
                    right.SendToAsync(rightEventArgs, sendBuffer, 0, sendBuffer.Length, SocketFlags.None, leftEndpoint, sendHandler);
                }
                catch (Exception ex)
                {
                    senderFinished.SetException(ex);
                }
            };

            sendHandler(0);

            Assert.True(receiverFinished.Task.Wait(TestTimeout));
            Assert.True(senderFinished.Task.Wait(TestTimeout));

            for (int i = 0; i < DatagramsToSend; i++)
            {
                Assert.NotNull(receivedChecksums[i]);
                Assert.Equal(sentChecksums[i], (uint)receivedChecksums[i]);
            }
        }

        public static readonly object[][] SendRecvAsync_Stream_TCP_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };

        [Theory]
        [MemberData(nameof(SendRecvAsync_Stream_TCP_MemberData))]
        public void SendRecvAsync_Stream_TCP(IPAddress listenAt, bool useMultipleBuffers)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 60;
            const int TestTimeout = 30000;

            var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.BindToAnonymousPort(listenAt);

            server.Listen(ListenBacklog);

            var serverFinished = new TaskCompletionSource<bool>();
            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();

            var serverEventArgs = new SocketAsyncEventArgs();
            server.AcceptAsync(serverEventArgs, remote =>
            {
                Action<int> recvHandler = null;
                bool first = true;

                if (!useMultipleBuffers)
                {
                    var recvBuffer = new byte[256];
                    recvHandler = received => 
                    {
                        if (!first)
                        {
                            if (received == 0)
                            {
                                remote.Dispose();
                                server.Dispose();
                                serverFinished.SetResult(true);
                                return;
                            }

                            bytesReceived += received;
                            receivedChecksum.Add(recvBuffer, 0, received);
                        }
                        else
                        {
                            first = false;
                        }

                        remote.ReceiveAsync(serverEventArgs, recvBuffer, 0, recvBuffer.Length, SocketFlags.None, recvHandler);
                    };
                }
                else
                {
                    var recvBuffers = new List<ArraySegment<byte>> {
                        new ArraySegment<byte>(new byte[123]),
                        new ArraySegment<byte>(new byte[256], 2, 100),
                        new ArraySegment<byte>(new byte[1], 0, 0),
                        new ArraySegment<byte>(new byte[64], 9, 33)
                    };

                    recvHandler = received =>
                    {
                        if (!first)
                        {
                            if (received == 0)
                            {
                                remote.Dispose();
                                server.Dispose();
                                serverFinished.SetResult(true);
                                return;
                            }

                            bytesReceived += received;
                            for (int i = 0, remaining = received; i < recvBuffers.Count && remaining > 0; i++)
                            {
                                ArraySegment<byte> buffer = recvBuffers[i];
                                int toAdd = Math.Min(buffer.Count, remaining);
                                receivedChecksum.Add(buffer.Array, buffer.Offset, toAdd);
                                remaining -= toAdd;
                            }
                        }
                        else
                        {
                            first = false;
                        }

                        remote.ReceiveAsync(serverEventArgs, recvBuffers, SocketFlags.None, recvHandler);
                    };
                }

                recvHandler(0);
            });

            EndPoint clientEndpoint = server.LocalEndPoint;
            var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            int bytesSent = 0;
            var sentChecksum = new Fletcher32();

            var clientEventArgs = new SocketAsyncEventArgs();
            client.ConnectAsync(clientEventArgs, clientEndpoint, () =>
            {
                Action<int> sendHandler = null;
                var random = new Random();
                var remaining = BytesToSend;
                bool first = true;

                if (!useMultipleBuffers)
                {
                    var sendBuffer = new byte[512];
                    sendHandler = sent =>
                    {
                        if (!first)
                        {
                            bytesSent += sent;
                            sentChecksum.Add(sendBuffer, 0, sent);

                            remaining -= sent;
                            Assert.True(remaining >= 0);
                            if (remaining == 0)
                            {
                                client.LingerState = new LingerOption(true, LingerTime);
                                client.Dispose();
                                return;
                            }
                        }
                        else
                        {
                            first = false;
                        }

                        random.NextBytes(sendBuffer);
                        client.SendAsync(clientEventArgs, sendBuffer, 0, Math.Min(sendBuffer.Length, remaining), SocketFlags.None, sendHandler);
                    };
                }
                else
                {
                    var sendBuffers = new List<ArraySegment<byte>> {
                        new ArraySegment<byte>(new byte[23]),
                        new ArraySegment<byte>(new byte[256], 2, 100),
                        new ArraySegment<byte>(new byte[1], 0, 0),
                        new ArraySegment<byte>(new byte[64], 9, 9)
                    };

                    sendHandler = sent =>
                    {
                        if (!first)
                        {
                            bytesSent += sent;
                            for (int i = 0, r = sent; i < sendBuffers.Count && r > 0; i++)
                            {
                                ArraySegment<byte> buffer = sendBuffers[i];
                                int toAdd = Math.Min(buffer.Count, r);
                                sentChecksum.Add(buffer.Array, buffer.Offset, toAdd);
                                r -= toAdd;
                            }

                            remaining -= sent;
                            if (remaining <= 0)
                            {
                                client.LingerState = new LingerOption(true, LingerTime);
                                client.Dispose();
                                return;
                            }
                        }
                        else
                        {
                            first = false;
                        }

                        for (int i = 0; i < sendBuffers.Count; i++)
                        {
                            random.NextBytes(sendBuffers[i].Array);
                        }

                        client.SendAsync(clientEventArgs, sendBuffers, SocketFlags.None, sendHandler);
                    };
                }

                sendHandler(0);
            });

            Assert.True(serverFinished.Task.Wait(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }

        public static readonly object[][] SendRecvAsync_TcpListener_TcpClient_MemberData = new[]
        {
            new object[] { IPAddress.Loopback },
            new object[] { IPAddress.IPv6Loopback },
        };

        [Theory]
        [MemberData(nameof(SendRecvAsync_TcpListener_TcpClient_MemberData))]
        public void SendRecvAsync_TcpListener_TcpClient(IPAddress listenAt)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 10;
            const int TestTimeout = 30000;

            var listener = new TcpListener(listenAt, 0);
            listener.Start(ListenBacklog);

            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();
            Task serverTask = Task.Run(async () =>
            {
                using (TcpClient remote = await listener.AcceptTcpClientAsync())
                using (NetworkStream stream = remote.GetStream())
                {
                    var recvBuffer = new byte[256];
                    for (;;)
                    {
                        int received = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length);
                        if (received == 0)
                        {
                            break;
                        }

                        bytesReceived += received;
                        receivedChecksum.Add(recvBuffer, 0, received);
                    }
                }
            });

            int bytesSent = 0;
            var sentChecksum = new Fletcher32();
            Task clientTask = Task.Run(async () =>
            {
                var clientEndpoint = (IPEndPoint)listener.LocalEndpoint;

                using (var client = new TcpClient(clientEndpoint.AddressFamily))
                {
                    await client.ConnectAsync(clientEndpoint.Address, clientEndpoint.Port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        var random = new Random();
                        var sendBuffer = new byte[512];
                        for (int remaining = BytesToSend, sent = 0; remaining > 0; remaining -= sent)
                        {
                            random.NextBytes(sendBuffer);

                            sent = Math.Min(sendBuffer.Length, remaining);
                            await stream.WriteAsync(sendBuffer, 0, sent);

                            bytesSent += sent;
                            sentChecksum.Add(sendBuffer, 0, sent);
                        }

                        client.LingerState = new LingerOption(true, LingerTime);
                    }
                }
            });

            Assert.True(Task.WaitAll(new[] { serverTask, clientTask }, TestTimeout));

            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }

        public static readonly object[][] SendRecvPollSync_TcpListener_TcpClient_MemberData = new[]
        {
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
        };

        [Theory]
        [MemberData(nameof(SendRecvPollSync_TcpListener_TcpClient_MemberData))]
        public void SendRecvPollSync_TcpListener_Socket(IPAddress listenAt, bool pollBeforeOperation)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int TestTimeout = 30000;

            var listener = new TcpListener(listenAt, 0);
            listener.Start(ListenBacklog);
            try
            {
                int bytesReceived = 0;
                var receivedChecksum = new Fletcher32();
                Task serverTask = Task.Run(async () =>
                {
                    using (Socket remote = await listener.AcceptSocketAsync())
                    {
                        var recvBuffer = new byte[256];
                        while (true)
                        {
                            if (pollBeforeOperation)
                            {
                                Assert.True(remote.Poll(-1, SelectMode.SelectRead), "Read poll before completion should have succeeded");
                            }
                            int received = remote.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None);
                            if (received == 0)
                            {
                                Assert.True(remote.Poll(0, SelectMode.SelectRead), "Read poll after completion should have succeeded");
                                break;
                            }

                            bytesReceived += received;
                            receivedChecksum.Add(recvBuffer, 0, received);
                        }
                    }
                });

                int bytesSent = 0;
                var sentChecksum = new Fletcher32();
                Task clientTask = Task.Run(async () =>
                {
                    var clientEndpoint = (IPEndPoint)listener.LocalEndpoint;

                    using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                    {
                        await client.ConnectAsync(clientEndpoint.Address, clientEndpoint.Port);

                        if (pollBeforeOperation)
                        {
                            Assert.False(client.Poll(TestTimeout, SelectMode.SelectRead), "Expected writer's read poll to fail after timeout");
                        }

                        var random = new Random();
                        var sendBuffer = new byte[512];
                        for (int remaining = BytesToSend, sent = 0; remaining > 0; remaining -= sent)
                        {
                            random.NextBytes(sendBuffer);

                            if (pollBeforeOperation)
                            {
                                Assert.True(client.Poll(-1, SelectMode.SelectWrite), "Write poll should have succeeded");
                            }
                            sent = client.Send(sendBuffer, 0, Math.Min(sendBuffer.Length, remaining), SocketFlags.None);

                            bytesSent += sent;
                            sentChecksum.Add(sendBuffer, 0, sent);
                        }
                    }
                });

                Assert.True(Task.WaitAll(new[] { serverTask, clientTask }, TestTimeout), "Wait timed out");

                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
