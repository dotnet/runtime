// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Net.Sockets.Tests
{
    public abstract class SendReceive<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public SendReceive(ITestOutputHelper output) : base(output) {}

        [Theory]
        [InlineData(null, 0, 0)] // null array
        [InlineData(1, -1, 0)] // offset low
        [InlineData(1, 2, 0)] // offset high
        [InlineData(1, 0, -1)] // count low
        [InlineData(1, 1, 2)] // count high
        public async Task InvalidArguments_Throws(int? length, int offset, int count)
        {
            if (!ValidatesArrayArguments) return;

            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Type expectedExceptionType = length == null ? typeof(ArgumentNullException) : typeof(ArgumentOutOfRangeException);

                var validBuffer = new ArraySegment<byte>(new byte[1]);
                var invalidBuffer = new FakeArraySegment { Array = length != null ? new byte[length.Value] : null, Offset = offset, Count = count }.ToActual();

                await Assert.ThrowsAsync(expectedExceptionType, () => ReceiveAsync(s, invalidBuffer));
                await Assert.ThrowsAsync(expectedExceptionType, () => ReceiveAsync(s, new List<ArraySegment<byte>> { invalidBuffer }));
                await Assert.ThrowsAsync(expectedExceptionType, () => ReceiveAsync(s, new List<ArraySegment<byte>> { validBuffer, invalidBuffer }));

                await Assert.ThrowsAsync(expectedExceptionType, () => SendAsync(s, invalidBuffer));
                await Assert.ThrowsAsync(expectedExceptionType, () => SendAsync(s, new List<ArraySegment<byte>> { invalidBuffer }));
                await Assert.ThrowsAsync(expectedExceptionType, () => SendAsync(s, new List<ArraySegment<byte>> { validBuffer, invalidBuffer }));
            }
        }

        [OuterLoop]
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
                            while (true)
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
                            while (true)
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

        [OuterLoop]
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task SendRecv_Stream_TCP_LargeMultiBufferSends(IPAddress listenAt)
        {
            using (var listener = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(listenAt);
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener);
                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await acceptTask)
                {
                    var sentChecksum = new Fletcher32();
                    var rand = new Random();
                    int bytesToSend = 0;
                    var buffers = new List<ArraySegment<byte>>();
                    const int NumBuffers = 5;
                    for (int i = 0; i < NumBuffers; i++)
                    {
                        var sendBuffer = new byte[12345678];
                        rand.NextBytes(sendBuffer);
                        bytesToSend += sendBuffer.Length - i; // trim off a few bytes to test offset/count
                        sentChecksum.Add(sendBuffer, i, sendBuffer.Length - i);
                        buffers.Add(new ArraySegment<byte>(sendBuffer, i, sendBuffer.Length - i));
                    }

                    Task<int> sendTask = SendAsync(client, buffers);

                    var receivedChecksum = new Fletcher32();
                    int bytesReceived = 0;
                    byte[] recvBuffer = new byte[1024];
                    while (bytesReceived < bytesToSend)
                    {
                        int received = await ReceiveAsync(server, new ArraySegment<byte>(recvBuffer));
                        if (received <= 0)
                        {
                            break;
                        }
                        bytesReceived += received;
                        receivedChecksum.Add(recvBuffer, 0, received);
                    }

                    Assert.Equal(bytesToSend, await sendTask);
                    Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
                }
            }
        }

        [OuterLoop]
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
                        byte[] recvBuffer1 = new byte[256], recvBuffer2 = new byte[256];
                        long iter = 0;
                        while (true)
                        {
                            ArraySegment<byte> seg1 = new ArraySegment<byte>(recvBuffer1), seg2 = new ArraySegment<byte>(recvBuffer2);
                            int received;
                            switch (iter++ % 3)
                            {
                                case 0: // single buffer
                                    received = await ReceiveAsync(remote, seg1);
                                    break;
                                case 1: // buffer list with a single buffer
                                    received = await ReceiveAsync(remote, new List<ArraySegment<byte>> { seg1 });
                                    break;
                                default: // buffer list with multiple buffers
                                    received = await ReceiveAsync(remote, new List<ArraySegment<byte>> { seg1, seg2 });
                                    break;
                            }
                            if (received == 0)
                            {
                                break;
                            }

                            bytesReceived += received;
                            receivedChecksum.Add(recvBuffer1, 0, Math.Min(received, recvBuffer1.Length));
                            if (received > recvBuffer1.Length)
                            {
                                receivedChecksum.Add(recvBuffer2, 0, received - recvBuffer1.Length);
                            }
                        }
                    }
                });

                EndPoint clientEndpoint = server.LocalEndPoint;
                using (var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, clientEndpoint);

                    var random = new Random();
                    byte[] sendBuffer1 = new byte[512], sendBuffer2 = new byte[512];
                    long iter = 0;
                    for (int sent = 0, remaining = BytesToSend; remaining > 0; remaining -= sent)
                    {
                        random.NextBytes(sendBuffer1);
                        random.NextBytes(sendBuffer2);
                        int amountFromSendBuffer1 = Math.Min(sendBuffer1.Length, remaining);
                        switch (iter++ % 3)
                        {
                            case 0: // single buffer
                                sent = await SendAsync(client, new ArraySegment<byte>(sendBuffer1, 0, amountFromSendBuffer1));
                                break;
                            case 1: // buffer list with a single buffer
                                sent = await SendAsync(client, new List<ArraySegment<byte>>
                                {
                                    new ArraySegment<byte>(sendBuffer1, 0, amountFromSendBuffer1)
                                });
                                break;
                            default: // buffer list with multiple buffers
                                sent = await SendAsync(client, new List<ArraySegment<byte>>
                                {
                                    new ArraySegment<byte>(sendBuffer1, 0, amountFromSendBuffer1),
                                    new ArraySegment<byte>(sendBuffer2, 0, Math.Min(sendBuffer2.Length, remaining - amountFromSendBuffer1)),
                                });
                                break;
                        }

                        bytesSent += sent;
                        sentChecksum.Add(sendBuffer1, 0, Math.Min(sent, sendBuffer1.Length));
                        if (sent > sendBuffer1.Length)
                        {
                            sentChecksum.Add(sendBuffer2, 0, sent - sendBuffer1.Length);
                        }
                    }

                    client.Shutdown(SocketShutdown.Send);
                    await serverProcessingTask;
                }

                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }
        }

        [OuterLoop]
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

        [OuterLoop]
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
                            AssertExtensions.SequenceEqual(sendData, receiveBuffer);
                        }
                    }
                }
            }
        }

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows8x))]
        [MemberData(nameof(LoopbacksAndBuffers))]
        public async Task SendRecvPollSync_TcpListener_Socket(IPAddress listenAt, bool pollBeforeOperation)
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

                _output?.WriteLine($"{DateTime.Now} Starting listener at {listener.LocalEndpoint}");
                Task serverTask = Task.Run(async () =>
                {
                    using (Socket remote = await listener.AcceptSocketAsync())
                    {
                        var recvBuffer = new byte[256];
                        int count = 0;

                        while (true)
                        {
                            if (pollBeforeOperation)
                            {
                                Assert.True(remote.Poll(-1, SelectMode.SelectRead), "Read poll before completion should have succeeded");
                            }
                            int received = remote.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None);
                            count++;
                            if (received == 0)
                            {
                                Assert.True(remote.Poll(0, SelectMode.SelectRead), "Read poll after completion should have succeeded");
                                _output?.WriteLine($"{DateTime.Now} Received 0 bytes. Stopping receiving loop after {count} iterations.");
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

                        client.Shutdown(SocketShutdown.Send);
                    }
                });

                await (new[] { serverTask, clientTask }).WhenAllOrAnyFailed(TestTimeout);

                if (bytesSent != bytesReceived)
                {
                    _output?.WriteLine($"{DateTime.Now} Test received only {bytesReceived} bytes from {bytesSent}. Client task is {clientTask.Status}, Server task is {serverTask.Status}");
                }

                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task Send_0ByteSend_Success()
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
                        // Zero byte send should be a no-op
                        int bytesSent = await SendAsync(client, new ArraySegment<byte>(Array.Empty<byte>()));
                        Assert.Equal(0, bytesSent);

                        // Socket should still be usable
                        await SendAsync(client, new byte[] { 99 });
                        byte[] buffer = new byte[10];
                        int bytesReceived = await ReceiveAsync(server, buffer);
                        Assert.Equal(1, bytesReceived);
                        Assert.Equal(99, buffer[0]);
                    }
                }
            }
        }

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
                        Assert.Equal(1, client.Available);

                        // We should be able to do another 0-byte receive that completes immediateliy
                        Assert.Equal(0, await ReceiveAsync(client, new ArraySegment<byte>(new byte[1], 0, 0)));
                        Assert.Equal(1, client.Available);

                        // Then receive the byte
                        Assert.Equal(1, await ReceiveAsync(client, new ArraySegment<byte>(new byte[1])));
                        Assert.Equal(0, client.Available);
                    }
                }
            }
        }

        [Fact]
        public async Task Send_0ByteSendTo_Success()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                for (int i = 0; i < 3; i++)
                {
                    // Send empty packet then real data.
                    int bytesSent = await SendToAsync(
                        client, new ArraySegment<byte>(Array.Empty<byte>()), server.LocalEndPoint!);
                    Assert.Equal(0, bytesSent);

                    await SendToAsync(client, new byte[] { 99 }, server.LocalEndPoint);

                    // Read empty packet
                    byte[] buffer = new byte[10];
                    SocketReceiveFromResult result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(0, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);

                    // Read real packet.
                    result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(1, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);
                    Assert.Equal(99, buffer[0]);
                }
            }
        }

        [Fact]
        public async Task Receive0ByteReturns_WhenPeerDisconnects()
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
                    // Have the client do a 0-byte receive.  No data is available, so this should pend.
                    Task<int> receive = ReceiveAsync(client, new ArraySegment<byte>(Array.Empty<byte>()));
                    Assert.False(receive.IsCompleted, $"Task should not have been completed, was {receive.Status}");

                    // Disconnect the client
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();

                    // The client should now wake up
                    Assert.Equal(0, await receive);
                }
            }
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        public async Task SendRecv_BlockingNonBlocking_LingerTimeout_Success(bool blocking, int lingerTimeout)
        {
            if (UsesSync) return;

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

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD, "SendBufferSize, ReceiveBufferSize = 0 not supported on BSD like stacks.")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task SendRecv_NoBuffering_Success()
        {
            if (UsesSync) return;

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
                    client.SendBufferSize = 0;
                    server.ReceiveBufferSize = 0;

                    var sendBuffer = new byte[10000];
                    Task sendTask = SendAsync(client, new ArraySegment<byte>(sendBuffer));

                    int totalReceived = 0;
                    var receiveBuffer = new ArraySegment<byte>(new byte[4096]);
                    while (totalReceived < sendBuffer.Length)
                    {
                        int received = await ReceiveAsync(server, receiveBuffer);
                        if (received <= 0) break;
                        totalReceived += received;
                    }
                    await sendTask;
                    Assert.Equal(sendBuffer.Length, totalReceived);
                }
            }
        }

        [OuterLoop]
        [Fact]
        public async Task SendRecv_DisposeDuringPendingReceive_ThrowsSocketException()
        {
            if (UsesSync) return; // if sync, can't guarantee call will have been initiated by time of disposal

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
                    Task receiveTask = ReceiveAsync(client, new ArraySegment<byte>(new byte[1]));
                    Assert.False(receiveTask.IsCompleted, "Receive should be pending");

                    client.Dispose();

                    var se = await Assert.ThrowsAsync<SocketException>(() => receiveTask);
                    Assert.True(
                        se.SocketErrorCode == SocketError.OperationAborted || se.SocketErrorCode == SocketError.ConnectionAborted,
                        $"Expected {nameof(SocketError.OperationAborted)} or {nameof(SocketError.ConnectionAborted)}, got {se.SocketErrorCode}");
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void SocketSendReceiveBufferSize_SetZero_ThrowsSocketException()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketException e;
                e = Assert.Throws<SocketException>(() => socket.SendBufferSize = 0);
                Assert.Equal(SocketError.InvalidArgument, e.SocketErrorCode);

                e = Assert.Throws<SocketException>(() => socket.ReceiveBufferSize = 0);
                Assert.Equal(SocketError.InvalidArgument, e.SocketErrorCode);
            }
        }

        [Fact]
        public async Task SendAsync_ConcurrentDispose_SucceedsOrThrowsAppropriateException()
        {
            if (UsesSync) return;

            for (int i = 0; i < 20; i++) // run multiple times to attempt to force various interleavings
            {
                (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair();
                using (client)
                using (server)
                using (var b = new Barrier(2))
                {
                    Task dispose = Task.Factory.StartNew(() =>
                    {
                        b.SignalAndWait();
                        client.Dispose();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    Task send = Task.Factory.StartNew(() =>
                    {
                        b.SignalAndWait();
                        SendAsync(client, new ArraySegment<byte>(new byte[1])).GetAwaiter().GetResult();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    await dispose;
                    Exception error = await Record.ExceptionAsync(() => send);
                    if (error != null)
                    {
                        Assert.True(
                            error is ObjectDisposedException ||
                            error is SocketException ||
                            (error is SEHException && PlatformDetection.IsInAppContainer),
                            error.ToString());
                    }
                }
            }
        }

        [Fact]
        public async Task ReceiveAsync_ConcurrentDispose_SucceedsOrThrowsAppropriateException()
        {
            if (UsesSync) return;

            for (int i = 0; i < 20; i++) // run multiple times to attempt to force various interleavings
            {
                (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair();
                using (client)
                using (server)
                using (var b = new Barrier(2))
                {
                    Task dispose = Task.Factory.StartNew(() =>
                    {
                        b.SignalAndWait();
                        client.Dispose();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    Task send = Task.Factory.StartNew(() =>
                    {
                        SendAsync(server, new ArraySegment<byte>(new byte[1])).GetAwaiter().GetResult();
                        b.SignalAndWait();
                        ReceiveAsync(client, new ArraySegment<byte>(new byte[1])).GetAwaiter().GetResult();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    await dispose;
                    Exception error = await Record.ExceptionAsync(() => send);
                    if (error != null)
                    {
                        Assert.True(
                            error is ObjectDisposedException ||
                            error is SocketException ||
                            (error is SEHException && PlatformDetection.IsInAppContainer),
                            error.ToString());
                    }
                }
            }
        }

        public static readonly TheoryData<IPAddress, bool> UdpReceiveGetsCanceledByDispose_Data = new TheoryData<IPAddress, bool>
        {
            { IPAddress.Loopback, true },
            { IPAddress.IPv6Loopback, true },
            { IPAddress.Loopback.MapToIPv6(), true },
            { IPAddress.Loopback, false },
            { IPAddress.IPv6Loopback, false },
            { IPAddress.Loopback.MapToIPv6(), false }
        };

        [Theory]
        [MemberData(nameof(UdpReceiveGetsCanceledByDispose_Data))]
        [SkipOnPlatform(TestPlatforms.OSX, "Not supported on OSX.")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task UdpReceiveGetsCanceledByDispose(IPAddress address, bool owning)
        {
            // Aborting sync operations for non-owning handles is not supported on Unix.
            if (!owning && UsesSync && !PlatformDetection.IsWindows)
            {
                return;
            }

            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                using SafeSocketHandle? owner = ReplaceWithNonOwning(ref socket, owning);

                if (address.IsIPv4MappedToIPv6) socket.DualMode = true;
                socket.BindToAnonymousPort(address);
                ConfigureNonBlocking(socket);

                Task receiveTask = ReceiveAsync(socket, new ArraySegment<byte>(new byte[1]));

                // Wait a little so the operation is started.
                await Task.Delay(msDelay);
                msDelay *= 2;
                Task disposeTask = Task.Run(() => socket.Dispose());

                await Task.WhenAny(disposeTask, receiveTask)
                          .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                await disposeTask;

                SocketError? localSocketError = null;

                try
                {
                    await receiveTask;
                }
                catch (SocketException se)
                {
                    localSocketError = se.SocketErrorCode;
                }
                catch (ObjectDisposedException)
                {
                    Assert.Fail("Dispose happened before the operation, retry.");
                }

                if (UsesSync)
                {
                    Assert.Equal(SocketError.Interrupted, localSocketError);
                }
                else
                {
                    Assert.Equal(SocketError.OperationAborted, localSocketError);
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        public static readonly TheoryData<bool, bool, bool, bool> TcpReceiveSendGetsCanceledByDispose_Data = new TheoryData<bool, bool, bool, bool>
        {
            { true, false, false, true },
            { true, false, true, true },
            { true, true, false, true },
            { false, false, false, true },
            { false, false, true, true },
            { false, true, false, true },
            { true, false, false, false },
            { true, false, true, false },
            { true, true, false, false },
            { false, false, false, false },
            { false, false, true, false },
            { false, true, false, false },
        };

        [Theory]
        [MemberData(nameof(TcpReceiveSendGetsCanceledByDispose_Data))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public async Task TcpReceiveSendGetsCanceledByDispose(bool receiveOrSend, bool ipv6Server, bool dualModeClient, bool owning)
        {
            // Aborting sync operations for non-owning handles is not supported on Unix.
            if (!owning && UsesSync && !PlatformDetection.IsWindows)
            {
                return;
            }

            // RHEL7 kernel has a bug preventing close(AF_UNKNOWN) to succeed with IPv6 sockets.
            // In this case Dispose will trigger a graceful shutdown, which means that receive will succeed on socket2.
            // This bug is fixed in kernel 3.10.0-1160.25+.
            // TODO: Remove this, once CI machines are updated to a newer kernel.
            bool mayShutdownGraceful = UsesSync && PlatformDetection.IsRedHatFamily7 && receiveOrSend && (ipv6Server || dualModeClient);

            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, the peer won't see a ConnectionReset SocketException and we won't
            // see a SocketException either.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair(ipv6Server, dualModeClient);
                using SafeSocketHandle? owner = ReplaceWithNonOwning(ref socket1, owning);

                using (socket2)
                {
                    Task socketOperation;
                    if (receiveOrSend)
                    {
                        socketOperation = ReceiveAsync(socket1, new ArraySegment<byte>(new byte[1]));
                    }
                    else
                    {
                        socketOperation = Task.Run(() =>
                        {
                            var buffer = new ArraySegment<byte>(new byte[4096]);
                            while (true)
                            {
                                SendAsync(socket1, buffer)
                                    .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout))
                                    .GetAwaiter().GetResult();
                            }
                        });
                    }

                    // Wait a little so the operation is started.
                    await Task.Delay(msDelay);
                    msDelay *= 2;
                    Task disposeTask = Task.Run(() => socket1.Dispose());

                    await Task.WhenAny(disposeTask, socketOperation)
                              .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                    await disposeTask
                              .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));

                    SocketError? localSocketError = null;
                    try
                    {
                        await socketOperation
                              .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                    }
                    catch (SocketException se)
                    {
                        localSocketError = se.SocketErrorCode;
                    }
                    catch (ObjectDisposedException)
                    {
                        Assert.Fail("Dispose happened before the operation, retry.");
                    }

                    if (UsesSync)
                    {
                        Assert.Equal(SocketError.ConnectionAborted, localSocketError);
                    }
                    else
                    {
                        Assert.Equal(SocketError.OperationAborted, localSocketError);
                    }

                    owner?.Dispose();

                    // On OSX, we're unable to unblock the on-going socket operations and
                    // perform an abortive close.
                    if (!(UsesSync && PlatformDetection.IsOSXLike))
                    {
                        SocketError? peerSocketError = null;
                        var receiveBuffer = new ArraySegment<byte>(new byte[4096]);
                        while (true)
                        {
                            try
                            {
                                int received = await ReceiveAsync(socket2, receiveBuffer)
                                                     .WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                                if (received == 0)
                                {
                                    break;
                                }
                            }
                            catch (SocketException se)
                            {
                                peerSocketError = se.SocketErrorCode;
                                break;
                            }
                        }

                        try
                        {
                            Assert.Equal(SocketError.ConnectionReset, peerSocketError);
                        }
                        catch when (mayShutdownGraceful)
                        {
                            Assert.Null(peerSocketError);
                        }
                    }
                }
            }, maxAttempts: 8, retryWhen: e => e is XunitException);
        }

        [Fact]
        public async Task TcpPeerReceivesFinOnShutdownWithPendingData()
        {
            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the send is started, no data is sent.
            int msDelay = 100;
            byte[] hugeBuffer = new byte[100_000_000];
            byte[] receiveBuffer = new byte[1024];
            await RetryHelper.ExecuteAsync(async () =>
            {
                (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
                using (socket1)
                using (socket2)
                {
                    // socket1: send a huge amount of data, then Shutdown and Dispose before the peer starts reading.
                    Task sendTask = SendAsync(socket1, hugeBuffer);
                    // Wait a little so the operation is started.
                    await Task.Delay(msDelay);
                    msDelay *= 2;
                    socket1.Shutdown(SocketShutdown.Both);
                    socket1.Dispose();

                    // socket2: read until FIN.
                    int receivedTotal = 0;
                    int received;
                    do
                    {
                        received = await ReceiveAsync(socket2, receiveBuffer);
                        receivedTotal += received;
                    } while (received != 0);

                    Assert.NotEqual(0, receivedTotal);
                }
            }, maxAttempts: 10);
        }
    }

    public sealed class SendReceive_Sync : SendReceive<SocketHelperArraySync>
    {
        public SendReceive_Sync(ITestOutputHelper output) : base(output) { }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void BlockingRead_DoesntRequireAnotherThreadPoolThread()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Set the max number of worker threads to a low value.
                ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
                ThreadPool.SetMaxThreads(Environment.ProcessorCount, completionPortThreads);

                // Create twice that many socket pairs, for good measure.
                (Socket, Socket)[] socketPairs = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(_ => SocketTestExtensions.CreateConnectedSocketPair()).ToArray();
                try
                {
                    // Ensure that on Unix all of the first socket in each pair are configured for sync-over-async.
                    foreach ((Socket, Socket) pair in socketPairs)
                    {
                        pair.Item1.ForceNonBlocking(force: true);
                    }

                    // Queue a work item for each first socket to do a blocking receive.
                    Task[] receives =
                        (from pair in socketPairs
                         select Task.Factory.StartNew(() => pair.Item1.Receive(new byte[1]), CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default))
                         .ToArray();

                    // Give a bit of time for the pool to start executing the receives.  It's possible this won't be enough,
                    // in which case the test we could get a false negative on the test, but we won't get spurious failures.
                    Thread.Sleep(1000);

                    // Now send to each socket.
                    foreach ((Socket, Socket) pair in socketPairs)
                    {
                        pair.Item2.Send(new byte[1]);
                    }

                    // And wait for all the receives to complete.
                    Assert.True(Task.WaitAll(receives, 60_000), "Expected all receives to complete within timeout");
                }
                finally
                {
                    foreach ((Socket, Socket) pair in socketPairs)
                    {
                        pair.Item1.Dispose();
                        pair.Item2.Dispose();
                    }
                }
            }).Dispose();
        }
    }

    public sealed class SendReceive_SyncForceNonBlocking : SendReceive<SocketHelperSyncForceNonBlocking>
    {
        public SendReceive_SyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendReceive_Apm : SendReceive<SocketHelperApm>
    {
        public SendReceive_Apm(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendReceive_Task : SendReceive<SocketHelperTask>
    {
        public SendReceive_Task(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendReceive_Eap : SendReceive<SocketHelperEap>
    {
        public SendReceive_Eap(ITestOutputHelper output) : base(output) {}
    }

    public sealed class SendReceive_SpanSync : SendReceive<SocketHelperSpanSync>
    {
        public SendReceive_SpanSync(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Send_0ByteSend_Span_Success()
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
                        // Zero byte send should be a no-op
                        int bytesSent = client.Send(ReadOnlySpan<byte>.Empty, SocketFlags.None);
                        Assert.Equal(0, bytesSent);

                        // Socket should still be usable
                        await SendAsync(client, new byte[] { 99 });
                        byte[] buffer = new byte[10];
                        int bytesReceived = await ReceiveAsync(server, buffer);
                        Assert.Equal(1, bytesReceived);
                        Assert.Equal(99, buffer[0]);
                    }
                }
            }
        }

        [Fact]
        public async Task Send_0ByteSendTo_Span_Success()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                for (int i = 0; i < 3; i++)
                {
                    // Send empty packet then real data.
                    int bytesSent = client.SendTo(ReadOnlySpan<byte>.Empty, server.LocalEndPoint!);
                    Assert.Equal(0, bytesSent);

                    client.SendTo(new byte[] { 99 }, server.LocalEndPoint);

                    // Read empty packet
                    byte[] buffer = new byte[10];
                    SocketReceiveFromResult result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(0, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);

                    // Read real packet.
                    result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(1, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);
                    Assert.Equal(99, buffer[0]);
                }
            }
        }

    }

    public sealed class SendReceive_SpanSyncForceNonBlocking : SendReceive<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendReceive_SpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendReceive_MemoryArrayTask : SendReceive<SocketHelperMemoryArrayTask>
    {
        public SendReceive_MemoryArrayTask(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Send_0ByteSend_Memory_Success()
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
                        // Zero byte send should be a no-op and complete immediately
                        Task<int> sendTask = client.SendAsync(ReadOnlyMemory<byte>.Empty, SocketFlags.None).AsTask();
                        Assert.True(sendTask.IsCompleted);
                        Assert.Equal(0, await sendTask);

                        // Socket should still be usable
                        await SendAsync(client, new byte[] { 99 });
                        byte[] buffer = new byte[10];
                        int bytesReceived = await ReceiveAsync(server, buffer);
                        Assert.Equal(1, bytesReceived);
                        Assert.Equal(99, buffer[0]);
                    }
                }
            }
        }

        [Fact]
        public async Task Send_0ByteSendTo_Memory_Success()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                for (int i = 0; i < 3; i++)
                {
                    // Send empty packet then real data.
                    int bytesSent = await client.SendToAsync(
                        ReadOnlyMemory<byte>.Empty, SocketFlags.None, server.LocalEndPoint!);
                    Assert.Equal(0, bytesSent);

                    await client.SendToAsync(new byte[] { 99 }, SocketFlags.None, server.LocalEndPoint);

                    // Read empty packet
                    byte[] buffer = new byte[10];
                    SocketReceiveFromResult result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(0, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);

                    // Read real packet.
                    result = await ReceiveFromAsync(server, buffer, new IPEndPoint(IPAddress.Any, 0));

                    Assert.Equal(1, result.ReceivedBytes);
                    Assert.Equal(client.LocalEndPoint, result.RemoteEndPoint);
                    Assert.Equal(99, buffer[0]);
                }
            }
        }

        [Fact]
        public async Task Precanceled_Throws()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.SendAsync((ReadOnlyMemory<byte>)new byte[0], SocketFlags.None, cts.Token));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReceiveAsync((Memory<byte>)new byte[0], SocketFlags.None, cts.Token));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_CanceledDuringOperation_Throws(bool ipv6)
        {
            const int CancelAfter = 200; // ms
            const int NumOfSends = 100;
            const int SendBufferSize = 1024;

            (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair(ipv6);
            byte[] buffer = new byte[1024 * 64];
            using (client)
            using (server)
            {
                client.SendBufferSize = SendBufferSize;
                CancellationTokenSource cts = new CancellationTokenSource();

                List<Task> tasks = new List<Task>();

                // After flooding the socket with a high number of send tasks,
                // we assume some of them won't complete before the "CancelAfter" period expires.
                for (int i=0; i < NumOfSends; i++)
                {
                    var task = client.SendAsync(buffer, SocketFlags.None, cts.Token).AsTask();
                    tasks.Add(task);
                }

                cts.CancelAfter(CancelAfter);

                // We shall see at least one cancelletion amongst all the scheduled sends:
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(tasks));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveAsync_CanceledDuringOperation_Throws(bool ipv6)
        {
            (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair(ipv6);
            using (client)
            using (server)
            {
                for (int len = 0; len < 2; len++)
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    ValueTask<int> vt = server.ReceiveAsync((Memory<byte>)new byte[len], SocketFlags.None, cts.Token);
                    Assert.False(vt.IsCompleted);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
                }

                // Make sure subsequent operations aren't canceled.
                await server.SendAsync((ReadOnlyMemory<byte>)new byte[1], SocketFlags.None);
                Assert.Equal(1, await client.ReceiveAsync((Memory<byte>)new byte[10], SocketFlags.None));
            }
        }

        [Fact]
        public async Task CanceledOneOfMultipleReceives_Udp_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                var cts = new CancellationTokenSource();

                // Create three UDP receives, only one of which we'll cancel.
                byte[] buffer1 = new byte[1], buffer2 = new byte[1], buffer3 = new byte[1];
                ValueTask<int> r1 = client.ReceiveAsync(buffer1.AsMemory(), SocketFlags.None, cts.Token);
                ValueTask<int> r2 = client.ReceiveAsync(buffer2.AsMemory(), SocketFlags.None);
                ValueTask<int> r3 = client.ReceiveAsync(buffer3.AsMemory(), SocketFlags.None);

                // Cancel one of them, and validate it's been canceled.
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await r1);
                Assert.Equal(0, buffer1[0]);

                // Send data to complete the others, and validate they complete successfully.
                using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    server.SendTo(new byte[1] { 42 }, client.LocalEndPoint);
                    server.SendTo(new byte[1] { 43 }, client.LocalEndPoint);
                }

                Assert.Equal(1, await r2);
                Assert.Equal(1, await r3);
                Assert.True(
                    (buffer2[0] == 42 && buffer3[0] == 43) ||
                    (buffer2[0] == 43 && buffer3[0] == 42),
                    $"buffer2[0]={buffer2[0]}, buffer3[0]={buffer3[0]}");
            }
        }

        [Fact]
        public async Task DisposedSocket_ThrowsOperationCanceledException()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    server.Shutdown(SocketShutdown.Both);
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.SendAsync((ReadOnlyMemory<byte>)new byte[0], SocketFlags.None, cts.Token));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReceiveAsync((Memory<byte>)new byte[0], SocketFlags.None, cts.Token));
                }
            }
        }

        [Fact]
        public async Task BlockingAsyncContinuations_OperationsStillCompleteSuccessfully()
        {
            if (UsesSync) return;
            if (Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1") return;

            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    await Task.Run(async delegate // escape the xunit sync context / task scheduler
                    {
                        const int SendDelayMs = 100;

                        Task sendTask = Task.Delay(SendDelayMs)
                            .ContinueWith(_ => server.SendAsync(new byte[1], SocketFlags.None))
                            .Unwrap();
                        await client.ReceiveAsync(new byte[1], SocketFlags.None);
                        sendTask.GetAwaiter().GetResult(); // should have already completed

                        // We may now be executing here as part of the continuation invoked synchronously
                        // when the client ReceiveAsync task was completed. Validate that if socket callbacks block
                        // (undesirably), other operations on that socket can still be processed.
                        var mre = new ManualResetEventSlim();
                        sendTask = Task.Delay(SendDelayMs)
                            .ContinueWith(_ => server.SendAsync(new byte[1], SocketFlags.None))
                            .Unwrap();
                        Task receiveTask = client
                            .ReceiveAsync(new byte[1], SocketFlags.None)
                            .ContinueWith(t => { mre.Set(); return t; }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                            .Unwrap();
                        mre.Wait(); // block waiting for other operations on this socket to complete

                        sendTask.GetAwaiter().GetResult();
                        await sendTask;
                        await receiveTask;
                    });
                }
            }
        }
    }

    public sealed class SendReceive_MemoryNativeTask : SendReceive<SocketHelperMemoryNativeTask>
    {
        public SendReceive_MemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
