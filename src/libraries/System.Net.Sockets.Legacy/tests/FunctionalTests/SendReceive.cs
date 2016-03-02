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

        [Fact]
        public void SendToRecvFrom_Single_Datagram_UDP_IPv6()
        {
            SendToRecvFrom_Datagram_UDP(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [ActiveIssue(3610)]
        [Fact]
        public void SendToRecvFromAPM_Single_Datagram_UDP_IPv6()
        {
            SendToRecvFromAPM_Datagram_UDP(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void SendToRecvFrom_Single_Datagram_UDP_IPv4()
        {
            SendToRecvFrom_Datagram_UDP(IPAddress.Loopback, IPAddress.Loopback);
        }

        [ActiveIssue(3610)]
        [Fact]
        public void SendToRecvFromAPM_Single_Datagram_UDP_IPv4()
        {
            SendToRecvFromAPM_Datagram_UDP(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void SendRecv_Multiple_Stream_TCP_IPv6()
        {
            SendRecv_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: true);
        }

        [Fact]
        public void SendRecvAPM_Multiple_Stream_TCP_IPv6()
        {
            SendRecvAPM_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: true);
        }

        [Fact]
        public void SendRecv_Single_Stream_TCP_IPv6()
        {
            SendRecv_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: false);
        }

        [Fact]
        public void SendRecvAPM_Single_Stream_TCP_IPv6()
        {
            SendRecvAPM_Stream_TCP(IPAddress.IPv6Loopback, useMultipleBuffers: false);
        }

        [Fact]
        public void SendRecv_Multiple_Stream_TCP_IPv4()
        {
            SendRecv_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: true);
        }

        [Fact]
        public void SendRecvAPM_Multiple_Stream_TCP_IPv4()
        {
            SendRecvAPM_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: true);
        }

        [Fact]
        public void SendRecv_Single_Stream_TCP_IPv4()
        {
            SendRecv_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: false);
        }

        [Fact]
        public void SendRecvAPM_Single_Stream_TCP_IPv4()
        {
            SendRecvAPM_Stream_TCP(IPAddress.Loopback, useMultipleBuffers: false);
        }
    }
}
