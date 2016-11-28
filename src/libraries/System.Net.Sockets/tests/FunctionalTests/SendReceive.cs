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

        private static void SendToRecvFrom_Datagram_UDP(IPAddress leftAddress, IPAddress rightAddress)
        {
            // TODO #5185: Harden against packet loss
            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 1000;
            const int TestTimeout = 30000;

            var left = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            left.BindToAnonymousPort(leftAddress);

            var right = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            right.BindToAnonymousPort(rightAddress);

            var leftEndpoint = (IPEndPoint)left.LocalEndPoint;
            var rightEndpoint = (IPEndPoint)right.LocalEndPoint;

            var receiverAck = new ManualResetEventSlim();
            var senderAck = new ManualResetEventSlim();

            var receivedChecksums = new uint?[DatagramsToSend];
            var leftThread = new Thread(() =>
            {
                using (left)
                {
                    EndPoint remote = leftEndpoint.Create(leftEndpoint.Serialize());
                    var recvBuffer = new byte[DatagramSize];
                    for (int i = 0; i < DatagramsToSend; i++)
                    {
                        int received = left.ReceiveFrom(recvBuffer, SocketFlags.None, ref remote);
                        Assert.Equal(DatagramSize, received);
                        Assert.Equal(rightEndpoint, remote);

                        int datagramId = (int)recvBuffer[0];
                        Assert.Null(receivedChecksums[datagramId]);
                        receivedChecksums[datagramId] = Fletcher32.Checksum(recvBuffer, 0, received);

                        receiverAck.Set();
                        Assert.True(senderAck.Wait(AckTimeout));
                        senderAck.Reset();
                    }
                }
            });

            leftThread.Start();

            var sentChecksums = new uint[DatagramsToSend];
            using (right)
            {
                var random = new Random();
                var sendBuffer = new byte[DatagramSize];
                for (int i = 0; i < DatagramsToSend; i++)
                {
                    random.NextBytes(sendBuffer);
                    sendBuffer[0] = (byte)i;

                    int sent = right.SendTo(sendBuffer, SocketFlags.None, leftEndpoint);

                    Assert.True(receiverAck.Wait(AckTimeout));
                    receiverAck.Reset();
                    senderAck.Set();

                    Assert.Equal(DatagramSize, sent);
                    sentChecksums[i] = Fletcher32.Checksum(sendBuffer, 0, sent);
                }
            }

            Assert.True(leftThread.Join(TestTimeout));
            for (int i = 0; i < DatagramsToSend; i++)
            {
                Assert.NotNull(receivedChecksums[i]);
                Assert.Equal(sentChecksums[i], (uint)receivedChecksums[i]);
            }
        }

        private static void SendToRecvFromAPM_Datagram_UDP(IPAddress leftAddress, IPAddress rightAddress)
        {
            // TODO #5185: Harden against packet loss
            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 1000;
            const int TestTimeout = 30000;

            var left = new Socket(leftAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            left.BindToAnonymousPort(leftAddress);

            var right = new Socket(rightAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
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

                    left.ReceiveFromAPM(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, receiveRemote, receiveHandler);
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
                    right.SendToAPM(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, leftEndpoint, sendHandler);
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
		
        private static void SendRecv_Stream_TCP(IPAddress listenAt, bool useMultipleBuffers)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 10;
            const int TestTimeout = 30000;

            var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.BindToAnonymousPort(listenAt);

            server.Listen(ListenBacklog);

            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();
            var serverThread = new Thread(() =>
            {
                using (server)
                {
                    Socket remote = server.Accept();
                    Assert.NotNull(remote);

                    using (remote)
                    {
                        if (!useMultipleBuffers)
                        {
                            var recvBuffer = new byte[256];
                            for (;;)
                            {
                                int received = remote.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None);
                                if (received == 0)
                                {
                                    break;
                                }

                                bytesReceived += received;
                                receivedChecksum.Add(recvBuffer, 0, received);
                            }
                        }
                        else
                        {
                            var recvBuffers = new List<ArraySegment<byte>> {
                                new ArraySegment<byte>(new byte[123]),
                                new ArraySegment<byte>(new byte[256], 2, 100),
                                new ArraySegment<byte>(new byte[1], 0, 0),
                                new ArraySegment<byte>(new byte[64], 9, 33)
                            };

                            for (;;)
                            {
                                int received = remote.Receive(recvBuffers, SocketFlags.None);
                                if (received == 0)
                                {
                                    break;
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
                        }
                    }
                }
            });
            serverThread.Start();

            EndPoint clientEndpoint = server.LocalEndPoint;
            var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(clientEndpoint);

            int bytesSent = 0;
            var sentChecksum = new Fletcher32();
            using (client)
            {
                var random = new Random();

                if (!useMultipleBuffers)
                {
                    var sendBuffer = new byte[512];
                    for (int sent = 0, remaining = BytesToSend; remaining > 0; remaining -= sent)
                    {
                        random.NextBytes(sendBuffer);

                        sent = client.Send(sendBuffer, 0, Math.Min(sendBuffer.Length, remaining), SocketFlags.None);
                        bytesSent += sent;
                        sentChecksum.Add(sendBuffer, 0, sent);
                    }
                }
                else
                {
                    var sendBuffers = new List<ArraySegment<byte>> {
                        new ArraySegment<byte>(new byte[23]),
                        new ArraySegment<byte>(new byte[256], 2, 100),
                        new ArraySegment<byte>(new byte[1], 0, 0),
                        new ArraySegment<byte>(new byte[64], 9, 9)
                    };

                    for (int sent = 0, toSend = BytesToSend; toSend > 0; toSend -= sent)
                    {
                        for (int i = 0; i < sendBuffers.Count; i++)
                        {
                            random.NextBytes(sendBuffers[i].Array);
                        }

                        sent = client.Send(sendBuffers, SocketFlags.None);

                        bytesSent += sent;
                        for (int i = 0, remaining = sent; i < sendBuffers.Count && remaining > 0; i++)
                        {
                            ArraySegment<byte> buffer = sendBuffers[i];
                            int toAdd = Math.Min(buffer.Count, remaining);
                            sentChecksum.Add(buffer.Array, buffer.Offset, toAdd);
                            remaining -= toAdd;
                        }
                    }
                }

                client.LingerState = new LingerOption(true, LingerTime);
            }

            Assert.True(serverThread.Join(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }

        private static void SendRecvAPM_Stream_TCP(IPAddress listenAt, bool useMultipleBuffers)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 10;
            const int TestTimeout = 30000;

            var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.BindToAnonymousPort(listenAt);

            server.Listen(ListenBacklog);

            var serverFinished = new TaskCompletionSource<bool>();
            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();

            server.AcceptAPM(remote =>
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

                        remote.ReceiveAPM(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, recvHandler);
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

                        remote.ReceiveAPM(recvBuffers, SocketFlags.None, recvHandler);
                    };
                }

                recvHandler(0);
            });

            EndPoint clientEndpoint = server.LocalEndPoint;
            var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            int bytesSent = 0;
            var sentChecksum = new Fletcher32();

            client.ConnectAPM(clientEndpoint, () =>
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

                        random.NextBytes(sendBuffer);
                        client.SendAPM(sendBuffer, 0, Math.Min(sendBuffer.Length, remaining), SocketFlags.None, sendHandler);
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
                        client.SendAPM(sendBuffers, SocketFlags.None, sendHandler);
                    };
                }

                sendHandler(0);
            });

            Assert.True(serverFinished.Task.Wait(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendToRecvFrom_Single_Datagram_UDP_IPv6()
        {
            SendToRecvFrom_Datagram_UDP(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendToRecvFromAPM_Single_Datagram_UDP_IPv6()
        {
            SendToRecvFromAPM_Datagram_UDP(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendToRecvFrom_Single_Datagram_UDP_IPv4()
        {
            SendToRecvFrom_Datagram_UDP(IPAddress.Loopback, IPAddress.Loopback);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendToRecvFromAPM_Single_Datagram_UDP_IPv4()
        {
            SendToRecvFromAPM_Datagram_UDP(IPAddress.Loopback, IPAddress.Loopback);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecv_Multiple_Stream_TCP_IPv6()
        {
            SendRecv_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: true);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecvAPM_Multiple_Stream_TCP_IPv6()
        {
            SendRecvAPM_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: true);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecv_Single_Stream_TCP_IPv6()
        {
            SendRecv_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: false);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecvAPM_Single_Stream_TCP_IPv6()
        {
            SendRecvAPM_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: false);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecv_Multiple_Stream_TCP_IPv4()
        {
            SendRecv_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: true);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecvAPM_Multiple_Stream_TCP_IPv4()
        {
            SendRecvAPM_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: true);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecv_Single_Stream_TCP_IPv4()
        {
            SendRecv_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: false);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendRecvAPM_Single_Stream_TCP_IPv4()
        {
            SendRecvAPM_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: false);
        }

        public static readonly object[][] SendToRecvFromAsync_Datagram_UDP_Socket_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, IPAddress.IPv6Loopback },
            new object[] { IPAddress.Loopback, IPAddress.Loopback }
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendToRecvFromAsync_Datagram_UDP_Socket_MemberData))]
        public void SendToRecvFromAsync_Datagram_UDP(IPAddress leftAddress, IPAddress rightAddress)
        {
            // TODO #5185: harden against packet loss
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

        public static readonly object[][] SendToRecvFromAsync_Datagram_UDP_UdpClient_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, IPAddress.IPv6Loopback },
            new object[] { IPAddress.Loopback, IPAddress.Loopback }
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendToRecvFromAsync_Datagram_UDP_Socket_MemberData))]
        public void SendToRecvFromAsync_Datagram_UDP_UdpClient(IPAddress leftAddress, IPAddress rightAddress)
        {
            // TODO #5185: harden against packet loss
            const int DatagramSize = 256;
            const int DatagramsToSend = 256;
            const int AckTimeout = 1000;
            const int TestTimeout = 30000;

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

                        int sent = await right.SendAsync(sendBuffer, DatagramSize, leftEndpoint);

                        Assert.True(receiverAck.Wait(AckTimeout));
                        receiverAck.Reset();
                        senderAck.Set();

                        Assert.Equal(DatagramSize, sent);
                        sentChecksums[sentDatagrams] = Fletcher32.Checksum(sendBuffer, 0, sent);
                    }
                });

                Assert.True(Task.WaitAll(new[] { receiverTask, senderTask }, TestTimeout));
                for (int i = 0; i < DatagramsToSend; i++)
                {
                    Assert.NotNull(receivedChecksums[i]);
                    Assert.Equal(sentChecksums[i], (uint)receivedChecksums[i]);
                }
            }
        }

        public static readonly object[][] SendRecvAsync_Stream_TCP_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [ActiveIssue(13778, TestPlatforms.OSX)]
        [Fact]
        public static async Task SendRecvAsync_0ByteReceive_Success()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(
                    acceptTask, 
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                {
                    TaskCompletionSource<bool> tcs = null;

                    var ea = new SocketAsyncEventArgs();
                    ea.SetBuffer(Array.Empty<byte>(), 0, 0);
                    ea.Completed += delegate { tcs.SetResult(true); };

                    for (int i = 0; i < 3; i++)
                    {
                        tcs = new TaskCompletionSource<bool>();

                        // Have the client do a 0-byte receive.  No data is available, so this should pend.
                        Assert.True(client.ReceiveAsync(ea));
                        Assert.Equal(0, client.Available);

                        // Have the server send 1 byte to the client.
                        Assert.Equal(1, server.Send(new byte[1], 0, 1, SocketFlags.None));

                        // The client should now wake up, getting 0 bytes with 1 byte available.
                        await tcs.Task;
                        Assert.Equal(0, ea.BytesTransferred);
                        Assert.Equal(SocketError.Success, ea.SocketError);
                        Assert.Equal(1, client.Available); // Due to #13778, this sometimes fails on macOS

                        // Receive that byte
                        Assert.Equal(1, client.Receive(new byte[1]));
                        Assert.Equal(0, client.Available);
                    }
                }
            }
        }
    }
}
