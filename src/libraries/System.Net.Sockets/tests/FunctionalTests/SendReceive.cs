// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class SendReceive : MemberDatas
    {
        private readonly ITestOutputHelper _log;

        public SendReceive(ITestOutputHelper output)
        {
            _log = output;
        }

        public abstract Task<Socket> AcceptAsync(Socket s);
        public abstract Task ConnectAsync(Socket s, EndPoint endPoint);
        public abstract Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<SocketReceiveFromResult> ReceiveFromAsync(
            Socket s, ArraySegment<byte> buffer, EndPoint endPoint);
        public abstract Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endpoint);
        public virtual bool GuaranteedSendOrdering => true;
        public virtual bool ValidatesArrayArguments => true;
        public virtual bool SupportsNonBlocking => true;

        [ActiveIssue(16715, TestPlatforms.AnyUnix)] // invalid array segments in buffer lists not handled properly
        [Fact]
        public async Task InvalidArguments_Throws()
        {
            var nullArray = new FakeArraySegment { Array = null, Offset = 0, Count = 0 }.ToActual();
            var offsetLow = new FakeArraySegment { Array = new byte[1], Offset = -1, Count = 0 }.ToActual();
            var offsetHigh = new FakeArraySegment { Array = new byte[1], Offset = 2, Count = 0 }.ToActual();
            var countLow = new FakeArraySegment { Array = new byte[1], Offset = 0, Count = -1 }.ToActual();
            var countHigh = new FakeArraySegment { Array = new byte[1], Offset = 1, Count = 2 }.ToActual();

            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (ValidatesArrayArguments)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => ReceiveAsync(s, nullArray));
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, offsetLow));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, offsetHigh));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, countLow));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, countHigh));
                
                if (ValidatesArrayArguments)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => ReceiveAsync(s, new List<ArraySegment<byte>> { nullArray }));
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, new List<ArraySegment<byte>> { offsetLow }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, new List<ArraySegment<byte>> { offsetHigh }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, new List<ArraySegment<byte>> { countLow }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ReceiveAsync(s, new List<ArraySegment<byte>> { countHigh }));

                if (ValidatesArrayArguments)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => SendAsync(s, nullArray));
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, offsetLow));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, offsetHigh));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, countLow));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, countHigh));

                if (ValidatesArrayArguments)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => SendAsync(s, new List<ArraySegment<byte>> { nullArray }));
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, new List<ArraySegment<byte>> { offsetLow }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, new List<ArraySegment<byte>> { offsetHigh }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, new List<ArraySegment<byte>> { countLow }));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SendAsync(s, new List<ArraySegment<byte>> { countHigh }));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task SendToRecvFrom_Datagram_UDP(IPAddress loopbackAddress)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

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

            var receiverAck = new SemaphoreSlim(0);
            var senderAck = new SemaphoreSlim(0);

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
                        Assert.True(await senderAck.WaitAsync(TestTimeout));
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

                    Assert.True(await receiverAck.WaitAsync(AckTimeout));
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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(LoopbacksAndBuffers))]
        public async Task SendRecv_Stream_TCP(IPAddress listenAt, bool useMultipleBuffers)
        {
            const int BytesToSend = 123456, ListenBacklog = 1, LingerTime = 1;
            int bytesReceived = 0, bytesSent = 0;
            Fletcher32 receivedChecksum = new Fletcher32(), sentChecksum = new Fletcher32();

            using (var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.BindToAnonymousPort(listenAt);
                server.Listen(ListenBacklog);

                Task serverProcessingTask = Task.Run(async () =>
                {
                    using (Socket remote = await AcceptAsync(server))
                    {
                        if (!useMultipleBuffers)
                        {
                            var recvBuffer = new byte[256];
                            for (;;)
                            {
                                int received = await ReceiveAsync(remote, new ArraySegment<byte>(recvBuffer));
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
                                new ArraySegment<byte>(new byte[64], 9, 33)};
                            for (;;)
                            {
                                int received = await ReceiveAsync(remote, recvBuffers);
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
                });

                EndPoint clientEndpoint = server.LocalEndPoint;
                using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, clientEndpoint);

                    var random = new Random();
                    if (!useMultipleBuffers)
                    {
                        var sendBuffer = new byte[512];
                        for (int sent = 0, remaining = BytesToSend; remaining > 0; remaining -= sent)
                        {
                            random.NextBytes(sendBuffer);
                            sent = await SendAsync(client, new ArraySegment<byte>(sendBuffer, 0, Math.Min(sendBuffer.Length, remaining)));
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
                        new ArraySegment<byte>(new byte[64], 9, 9)};
                        for (int sent = 0, toSend = BytesToSend; toSend > 0; toSend -= sent)
                        {
                            for (int i = 0; i < sendBuffers.Count; i++)
                            {
                                random.NextBytes(sendBuffers[i].Array);
                            }

                            sent = await SendAsync(client, sendBuffers);

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
                    client.Shutdown(SocketShutdown.Send);
                    await serverProcessingTask;
                }

                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task SendRecv_Stream_TCP_AlternateBufferAndBufferList(IPAddress listenAt)
        {
            const int BytesToSend = 123456;
            int bytesReceived = 0, bytesSent = 0;
            Fletcher32 receivedChecksum = new Fletcher32(), sentChecksum = new Fletcher32();

            using (var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.BindToAnonymousPort(listenAt);
                server.Listen(1);

                Task serverProcessingTask = Task.Run(async () =>
                {
                    using (Socket remote = await AcceptAsync(server))
                    {
                        var recvBuffer = new byte[256];
                        long iter = 0;
                        while (true)
                        {
                            var seg = new ArraySegment<byte>(recvBuffer);
                            int received = await (iter++ % 2 == 0 ?
                                ReceiveAsync(remote, seg) :
                                ReceiveAsync(remote, new List<ArraySegment<byte>> { seg }));
                            if (received == 0)
                            {
                                break;
                            }

                            bytesReceived += received;
                            receivedChecksum.Add(recvBuffer, 0, received);
                        }
                    }
                });

                EndPoint clientEndpoint = server.LocalEndPoint;
                using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, clientEndpoint);

                    var random = new Random();
                    var sendBuffer = new byte[512];
                    long iter = 0;
                    for (int sent = 0, remaining = BytesToSend; remaining > 0; remaining -= sent)
                    {
                        random.NextBytes(sendBuffer);
                        var seg = new ArraySegment<byte>(sendBuffer, 0, Math.Min(sendBuffer.Length, remaining));
                        sent = await (iter++ % 2 == 0 ?
                            SendAsync(client, seg) :
                            SendAsync(client, new List<ArraySegment<byte>> { seg }));
                        bytesSent += sent;
                        sentChecksum.Add(sendBuffer, 0, sent);
                    }

                    client.Shutdown(SocketShutdown.Send);
                    await serverProcessingTask;
                }

                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(LoopbacksAndBuffers))]
        public async Task SendRecv_Stream_TCP_MultipleConcurrentReceives(IPAddress listenAt, bool useMultipleBuffers)
        {
            using (var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.BindToAnonymousPort(listenAt);
                server.Listen(1);

                EndPoint clientEndpoint = server.LocalEndPoint;
                using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task clientConnect = ConnectAsync(client, clientEndpoint);
                    using (Socket remote = await AcceptAsync(server))
                    {
                        await clientConnect;

                        if (useMultipleBuffers)
                        {
                            byte[] buffer1 = new byte[1], buffer2 = new byte[1], buffer3 = new byte[1], buffer4 = new byte[1], buffer5 = new byte[1];

                            Task<int> receive1 = ReceiveAsync(client, new List<ArraySegment<byte>> { new ArraySegment<byte>(buffer1), new ArraySegment<byte>(buffer2) });
                            Task<int> receive2 = ReceiveAsync(client, new List<ArraySegment<byte>> { new ArraySegment<byte>(buffer3), new ArraySegment<byte>(buffer4) });
                            Task<int> receive3 = ReceiveAsync(client, new List<ArraySegment<byte>> { new ArraySegment<byte>(buffer5) });

                            await Task.WhenAll(
                                SendAsync(remote, new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 })),
                                receive1, receive2, receive3);

                            Assert.True(receive1.Result == 1 || receive1.Result == 2, $"Expected 1 or 2, got {receive1.Result}");
                            Assert.True(receive2.Result == 1 || receive2.Result == 2, $"Expected 1 or 2, got {receive2.Result}");
                            Assert.Equal(1, receive3.Result);

                            if (GuaranteedSendOrdering)
                            {
                                if (receive1.Result == 1 && receive2.Result == 1)
                                {
                                    Assert.Equal(1, buffer1[0]);
                                    Assert.Equal(0, buffer2[0]);
                                    Assert.Equal(2, buffer3[0]);
                                    Assert.Equal(0, buffer4[0]);
                                    Assert.Equal(3, buffer5[0]);
                                }
                                else if (receive1.Result == 1 && receive2.Result == 2)
                                {
                                    Assert.Equal(1, buffer1[0]);
                                    Assert.Equal(0, buffer2[0]);
                                    Assert.Equal(2, buffer3[0]);
                                    Assert.Equal(3, buffer4[0]);
                                    Assert.Equal(4, buffer5[0]);
                                }
                                else if (receive1.Result == 2 && receive2.Result == 1)
                                {
                                    Assert.Equal(1, buffer1[0]);
                                    Assert.Equal(2, buffer2[0]);
                                    Assert.Equal(3, buffer3[0]);
                                    Assert.Equal(0, buffer4[0]);
                                    Assert.Equal(4, buffer5[0]);
                                }
                                else // receive1.Result == 2 && receive2.Result == 2
                                {
                                    Assert.Equal(1, buffer1[0]);
                                    Assert.Equal(2, buffer2[0]);
                                    Assert.Equal(3, buffer3[0]);
                                    Assert.Equal(4, buffer4[0]);
                                    Assert.Equal(5, buffer5[0]);
                                }
                            }
                        }
                        else
                        {
                            var buffer1 = new ArraySegment<byte>(new byte[1]);
                            var buffer2 = new ArraySegment<byte>(new byte[1]);
                            var buffer3 = new ArraySegment<byte>(new byte[1]);

                            Task<int> receive1 = ReceiveAsync(client, buffer1);
                            Task<int> receive2 = ReceiveAsync(client, buffer2);
                            Task<int> receive3 = ReceiveAsync(client, buffer3);

                            await Task.WhenAll(
                                SendAsync(remote, new ArraySegment<byte>(new byte[] { 1, 2, 3 })),
                                receive1, receive2, receive3);

                            Assert.Equal(3, receive1.Result + receive2.Result + receive3.Result);

                            if (GuaranteedSendOrdering)
                            {
                                Assert.Equal(1, buffer1.Array[0]);
                                Assert.Equal(2, buffer2.Array[0]);
                                Assert.Equal(3, buffer3.Array[0]);
                            }
                        }
                    }
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(LoopbacksAndBuffers))]
        public async Task SendRecv_Stream_TCP_MultipleConcurrentSends(IPAddress listenAt, bool useMultipleBuffers)
        {
            using (var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] sendData = new byte[5000000];
                new Random(42).NextBytes(sendData);

                Func<byte[], int, int, byte[]> slice = (input, offset, count) =>
                {
                    var arr = new byte[count];
                    Array.Copy(input, offset, arr, 0, count);
                    return arr;
                };

                server.BindToAnonymousPort(listenAt);
                server.Listen(1);

                EndPoint clientEndpoint = server.LocalEndPoint;
                using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task clientConnect = ConnectAsync(client, clientEndpoint);
                    using (Socket remote = await AcceptAsync(server))
                    {
                        await clientConnect;

                        Task<int> send1, send2, send3;
                        if (useMultipleBuffers)
                        {
                            var bufferList1 = new List<ArraySegment<byte>> { new ArraySegment<byte>(slice(sendData, 0, 1000000)), new ArraySegment<byte>(slice(sendData, 1000000, 1000000)) };
                            var bufferList2 = new List<ArraySegment<byte>> { new ArraySegment<byte>(slice(sendData, 2000000, 1000000)), new ArraySegment<byte>(slice(sendData, 3000000, 1000000)) };
                            var bufferList3 = new List<ArraySegment<byte>> { new ArraySegment<byte>(slice(sendData, 4000000, 1000000)) };

                            send1 = SendAsync(client, bufferList1);
                            send2 = SendAsync(client, bufferList2);
                            send3 = SendAsync(client, bufferList3);
                        }
                        else
                        {
                            var buffer1 = new ArraySegment<byte>(slice(sendData, 0, 2000000));
                            var buffer2 = new ArraySegment<byte>(slice(sendData, 2000000, 2000000));
                            var buffer3 = new ArraySegment<byte>(slice(sendData, 4000000, 1000000));

                            send1 = SendAsync(client, buffer1);
                            send2 = SendAsync(client, buffer2);
                            send3 = SendAsync(client, buffer3);
                        }

                        int receivedTotal = 0;
                        int received;
                        var receiveBuffer = new byte[sendData.Length];
                        while (receivedTotal < receiveBuffer.Length)
                        {
                            if ((received = await ReceiveAsync(remote, new ArraySegment<byte>(receiveBuffer, receivedTotal, receiveBuffer.Length - receivedTotal))) == 0) break;
                            receivedTotal += received;
                        }
                        Assert.Equal(5000000, receivedTotal);
                        if (GuaranteedSendOrdering)
                        {
                            Assert.Equal(sendData, receiveBuffer);
                        }
                    }
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(LoopbacksAndBuffers))]
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
                        await ConnectAsync(client, clientEndpoint);

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
        public async Task SendRecv_0ByteReceive_Success()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener);
                await Task.WhenAll(
                    acceptTask,
                    ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        // Have the client do a 0-byte receive.  No data is available, so this should pend.
                        Task<int> receive = ReceiveAsync(client, new ArraySegment<byte>(Array.Empty<byte>()));
                        Assert.False(receive.IsCompleted);
                        Assert.Equal(0, client.Available);

                        // Have the server send 1 byte to the client.
                        Assert.Equal(1, server.Send(new byte[1], 0, 1, SocketFlags.None));
                        Assert.Equal(0, server.Available);

                        // The client should now wake up, getting 0 bytes with 1 byte available.
                        Assert.Equal(0, await receive);
                        Assert.Equal(1, client.Available); // Due to #13778, this sometimes fails on macOS

                        // Receive that byte
                        Assert.Equal(1, await ReceiveAsync(client, new ArraySegment<byte>(new byte[1])));
                        Assert.Equal(0, client.Available);
                    }
                }
            }
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        public async Task SendRecv_BlockingNonBlocking_LingerTimeout_Success(bool blocking, int lingerTimeout)
        {
            if (!SupportsNonBlocking) return;

            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Blocking = blocking;
                listener.Blocking = blocking;

                client.LingerState = new LingerOption(true, lingerTimeout);
                listener.LingerState = new LingerOption(true, lingerTimeout);

                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener);
                await Task.WhenAll(
                    acceptTask,
                    ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                {
                    server.Blocking = blocking;
                    server.LingerState = new LingerOption(true, lingerTimeout);

                    Task<int> receive = ReceiveAsync(client, new ArraySegment<byte>(new byte[1]));
                    Assert.Equal(1, await SendAsync(server, new ArraySegment<byte>(new byte[1])));
                    Assert.Equal(1, await receive);
                }
            }
        }

        [ActiveIssue(16716, TestPlatforms.OSX)] // SendBufferSize = 0 throws
        [Fact]
        public async Task SendRecv_NoBuffering_Success()
        {
            if (!SupportsNonBlocking) return;

            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener);
                await Task.WhenAll(
                    acceptTask,
                    ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));

                client.SendBufferSize = 0;

                using (Socket server = await acceptTask)
                {
                    var sendBuffer = new byte[5000000];
                    Task sendTask = SendAsync(client, new ArraySegment<byte>(sendBuffer));
                    Assert.False(sendTask.IsCompleted);

                    int totalReceived = 0;
                    var receiveBuffer = new ArraySegment<byte>(new byte[4096]);
                    while (totalReceived < sendBuffer.Length)
                    {
                        int received = await ReceiveAsync(server, receiveBuffer);
                        if (received <= 0) break;
                        totalReceived += received;
                    }
                    Assert.Equal(sendBuffer.Length, totalReceived);
                    await sendTask;
                }
            }
        }
    }

    public sealed class SendReceiveUdpClient : MemberDatas
    {
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public void SendToRecvFromAsync_Datagram_UDP_UdpClient(IPAddress loopbackAddress)
        {
            IPAddress leftAddress = loopbackAddress, rightAddress = loopbackAddress;

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
    }

    public sealed class SendReceiveListener : MemberDatas
    {
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
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

            Assert.True(Task.WaitAll(new[] { serverTask, clientTask }, TestTimeout),
                $"Time out waiting for serverTask ({serverTask.Status}) and clientTask ({clientTask.Status})");

            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }
    }

    public sealed class SendReceiveSync : SendReceive
    {
        public SendReceiveSync(ITestOutputHelper output) : base(output) { }
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => s.Accept());
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => s.Connect(endPoint));
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Receive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Run(() => s.Receive(bufferList, SocketFlags.None));
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() =>
            {
                int received = s.ReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, ref endPoint);
                return new SocketReceiveFromResult
                {
                    ReceivedBytes = received,
                    RemoteEndPoint = endPoint
                };
            });
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Run(() => s.Send(bufferList, SocketFlags.None));
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() => s.SendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endPoint));

        public override bool GuaranteedSendOrdering => false;
        public override bool SupportsNonBlocking => false;
    }

    public sealed class SendReceiveApm : SendReceive
    {
        public SendReceiveApm(ITestOutputHelper output) : base(output) { }
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Factory.FromAsync(s.BeginAccept, s.EndAccept, null);
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Factory.FromAsync(s.BeginConnect, s.EndConnect, endPoint, null);
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Factory.FromAsync((callback, state) =>
                s.BeginReceive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, callback, state),
                s.EndReceive, null);
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Factory.FromAsync(s.BeginReceive, s.EndReceive, bufferList, SocketFlags.None, null);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint)
        {
            var tcs = new TaskCompletionSource<SocketReceiveFromResult>();
            s.BeginReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, ref endPoint, iar =>
            {
                try
                {
                    int receivedBytes = s.EndReceiveFrom(iar, ref endPoint);
                    tcs.TrySetResult(new SocketReceiveFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = endPoint
                    });
                }
                catch (Exception e) { tcs.TrySetException(e); }
            }, null);
            return tcs.Task;
        }
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Factory.FromAsync((callback, state) =>
                s.BeginSend(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, callback, state),
                s.EndSend, null);
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Factory.FromAsync(s.BeginSend, s.EndSend, bufferList, SocketFlags.None, null);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginSendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endPoint, callback, state),
                s.EndSendTo, null);
    }

    public sealed class SendReceiveTask : SendReceive
    {
        public SendReceiveTask(ITestOutputHelper output) : base(output) { }
        public override Task<Socket> AcceptAsync(Socket s) =>
            s.AcceptAsync();
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            s.ConnectAsync(endPoint);
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync(buffer, SocketFlags.None);
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.ReceiveAsync(bufferList, SocketFlags.None);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync(buffer, SocketFlags.None);
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.SendAsync(bufferList, SocketFlags.None);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.SendToAsync(buffer, SocketFlags.None, endPoint);
    }

    public sealed class SendReceiveEap : SendReceive
    {
        public SendReceiveEap(ITestOutputHelper output) : base(output) { }

        public override bool ValidatesArrayArguments => false;

        public override Task<Socket> AcceptAsync(Socket s) =>
            InvokeAsync(s, e => e.AcceptSocket, e => s.AcceptAsync(e));
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            InvokeAsync(s, e => true, e =>
            {
                e.RemoteEndPoint = endPoint;
                return s.ConnectAsync(e);
            });
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                return s.ReceiveAsync(e);
            });
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.BufferList = bufferList;
                return s.ReceiveAsync(e);
            });
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            InvokeAsync(s, e => new SocketReceiveFromResult { ReceivedBytes = e.BytesTransferred, RemoteEndPoint = e.RemoteEndPoint }, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                e.RemoteEndPoint = endPoint;
                return s.ReceiveFromAsync(e);
            });
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                return s.SendAsync(e);
            });
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.BufferList = bufferList;
                return s.SendAsync(e);
            });
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                e.RemoteEndPoint = endPoint;
                return s.SendToAsync(e);
            });

        private static Task<TResult> InvokeAsync<TResult>(
            Socket s,
            Func<SocketAsyncEventArgs, TResult> getResult,
            Func<SocketAsyncEventArgs, bool> invoke)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var saea = new SocketAsyncEventArgs();
            EventHandler<SocketAsyncEventArgs> handler = (_, e) =>
            {
                if (e.SocketError == SocketError.Success) tcs.SetResult(getResult(e));
                else tcs.SetException(new SocketException((int)e.SocketError));
                saea.Dispose();
            };
            saea.Completed += handler;
            if (!invoke(saea)) handler(s, saea);
            return tcs.Task;
        }
    }

    public abstract class MemberDatas
    {
        public static readonly object[][] Loopbacks = new[]
        {
            new object[] { IPAddress.Loopback },
            new object[] { IPAddress.IPv6Loopback },
        };

        public static readonly object[][] LoopbacksAndBuffers = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };
    }

    internal struct FakeArraySegment
    {
        public byte[] Array;
        public int Offset;
        public int Count;

        public ArraySegment<byte> ToActual()
        {
            ArraySegmentWrapper wrapper = default(ArraySegmentWrapper);
            wrapper.Fake = this;
            return wrapper.Actual;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ArraySegmentWrapper
    {
        [FieldOffset(0)] public ArraySegment<byte> Actual;
        [FieldOffset(0)] public FakeArraySegment Fake;
    }
}
