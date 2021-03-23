// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public abstract class QuicStreamTests<T> : QuicTestBase<T>
         where T : IQuicImplProviderFactory, new()
    {
        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task BasicTest()
        {
            using QuicListener listener = CreateQuicListener();

            for (int i = 0; i < 100; i++)
            {
                Task listenTask = Task.Run(async () =>
                {
                    using QuicConnection connection = await listener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await stream.ReadAsync(buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.True(s_data.Span.SequenceEqual(buffer));

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream.ShutdownWriteCompleted();

                    await connection.CloseAsync(errorCode: 0);
                });

                Task clientTask = Task.Run(async () =>
                {
                    using QuicConnection connection = CreateQuicConnection(listener.ListenEndPoint);
                    await connection.ConnectAsync();
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    await stream.WriteAsync(s_data, endStream: true);

                    byte[] memory = new byte[12];
                    int bytesRead = await stream.ReadAsync(memory);

                    Assert.Equal(s_data.Length, bytesRead);
                    // TODO this failed once...
                    Assert.True(s_data.Span.SequenceEqual(memory));
                    await stream.ShutdownWriteCompleted();

                    await connection.CloseAsync(errorCode: 0);
                });

                await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 10000);
            }
        }

        [Fact]
        public async Task MultipleReadsAndWrites()
        {
            using QuicListener listener = CreateQuicListener();

            for (int j = 0; j < 100; j++)
            {
                Task listenTask = Task.Run(async () =>
                {
                    // Connection isn't being accepted, interesting.
                    using QuicConnection connection = await listener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    byte[] buffer = new byte[s_data.Length];

                    while (true)
                    {
                        int bytesRead = await stream.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        Assert.Equal(s_data.Length, bytesRead);
                        Assert.True(s_data.Span.SequenceEqual(buffer));
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);
                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync(errorCode: 0);
                });

                Task clientTask = Task.Run(async () =>
                {
                    using QuicConnection connection = CreateQuicConnection(listener.ListenEndPoint);
                    await connection.ConnectAsync();
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    for (int i = 0; i < 5; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }

                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    byte[] memory = new byte[12];
                    while (true)
                    {
                        int res = await stream.ReadAsync(memory);
                        if (res == 0)
                        {
                            break;
                        }
                        Assert.True(s_data.Span.SequenceEqual(memory));
                    }

                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync(errorCode: 0);
                });

                await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 1000000);

            }
        }

        [Fact]
        public async Task MultipleStreamsOnSingleConnection()
        {
            using QuicListener listener = CreateQuicListener();

            Task listenTask = Task.Run(async () =>
            {
                {
                    using QuicConnection connection = await listener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    await using QuicStream stream2 = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[s_data.Length];

                    while (true)
                    {
                        int bytesRead = await stream.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        Assert.Equal(s_data.Length, bytesRead);
                        Assert.True(s_data.Span.SequenceEqual(buffer));
                    }

                    while (true)
                    {
                        int bytesRead = await stream2.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        Assert.True(s_data.Span.SequenceEqual(buffer));
                    }

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream.ShutdownWriteCompleted();

                    await stream2.WriteAsync(s_data, endStream: true);
                    await stream2.ShutdownWriteCompleted();

                    await connection.CloseAsync(errorCode: 0);
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using QuicConnection connection = CreateQuicConnection(listener.ListenEndPoint);
                await connection.ConnectAsync();
                await using QuicStream stream = connection.OpenBidirectionalStream();
                await using QuicStream stream2 = connection.OpenBidirectionalStream();

                await stream.WriteAsync(s_data, endStream: true);
                await stream.ShutdownWriteCompleted();
                await stream2.WriteAsync(s_data, endStream: true);
                await stream2.ShutdownWriteCompleted();

                byte[] memory = new byte[12];
                while (true)
                {
                    int res = await stream.ReadAsync(memory);
                    if (res == 0)
                    {
                        break;
                    }
                    Assert.True(s_data.Span.SequenceEqual(memory));
                }

                while (true)
                {
                    int res = await stream2.ReadAsync(memory);
                    if (res == 0)
                    {
                        break;
                    }
                    Assert.True(s_data.Span.SequenceEqual(memory));
                }

                await connection.CloseAsync(errorCode: 0);
            });

            await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 60000);
        }

        [Fact]
        public async Task GetStreamIdWithoutStartWorks()
        {
            using QuicListener listener = CreateQuicListener();
            using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);

            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await clientTask;

            using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
            Assert.Equal(0, clientStream.StreamId);
        }

        [Fact]
        public async Task LargeDataSentAndReceived()
        {
            byte[] data = Enumerable.Range(0, 64 * 1024).Select(x => (byte)x).ToArray();
            const int NumberOfWrites = 256;       // total sent = 16M

            using QuicListener listener = CreateQuicListener();

            for (int j = 0; j < 100; j++)
            {
                Task listenTask = Task.Run(async () =>
                {
                    using QuicConnection connection = await listener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    byte[] buffer = new byte[data.Length];

                    for (int i = 0; i < NumberOfWrites; i++)
                    {
                        int totalBytesRead = 0;
                        while (totalBytesRead < data.Length)
                        {
                            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead));
                            Assert.NotEqual(0, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        Assert.Equal(data.Length, totalBytesRead);
                        Assert.True(data.AsSpan().SequenceEqual(buffer));
                    }

                    for (int i = 0; i < NumberOfWrites; i++)
                    {
                        await stream.WriteAsync(data);
                    }

                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync(errorCode: 0);
                });

                Task clientTask = Task.Run(async () =>
                {
                    using QuicConnection connection = CreateQuicConnection(listener.ListenEndPoint);
                    await connection.ConnectAsync();
                    await using QuicStream stream = connection.OpenBidirectionalStream();
                    byte[] buffer = new byte[data.Length];

                    for (int i = 0; i < NumberOfWrites; i++)
                    {
                        await stream.WriteAsync(data);
                    }

                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    for (int i = 0; i < NumberOfWrites; i++)
                    {
                        int totalBytesRead = 0;
                        while (totalBytesRead < data.Length)
                        {
                            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead));
                            Assert.NotEqual(0, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        Assert.Equal(data.Length, totalBytesRead);
                        Assert.True(data.AsSpan().SequenceEqual(buffer));
                    }

                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync(errorCode: 0);
                });

                await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 1000000);
            }
        }

        [Fact]
        public async Task TestStreams()
        {
            using QuicListener listener = CreateQuicListener();
            IPEndPoint listenEndPoint = listener.ListenEndPoint;

            using QuicConnection clientConnection = CreateQuicConnection(listenEndPoint);

            Assert.False(clientConnection.Connected);
            Assert.Equal(listenEndPoint, clientConnection.RemoteEndPoint);

            ValueTask connectTask = clientConnection.ConnectAsync();
            QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await connectTask;

            Assert.True(clientConnection.Connected);
            Assert.True(serverConnection.Connected);
            Assert.Equal(listenEndPoint, serverConnection.LocalEndPoint);
            Assert.Equal(listenEndPoint, clientConnection.RemoteEndPoint);
            Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);

            await CreateAndTestBidirectionalStream(clientConnection, serverConnection);
            await CreateAndTestBidirectionalStream(serverConnection, clientConnection);
            await CreateAndTestUnidirectionalStream(serverConnection, clientConnection);
            await CreateAndTestUnidirectionalStream(clientConnection, serverConnection);
            await clientConnection.CloseAsync(errorCode: 0);
        }

        private static async Task CreateAndTestBidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            using QuicStream s1 = c1.OpenBidirectionalStream();
            Assert.True(s1.CanRead);
            Assert.True(s1.CanWrite);

            ValueTask writeTask = s1.WriteAsync(s_data);

            using QuicStream s2 = await c2.AcceptStreamAsync();
            await ReceiveDataAsync(s_data, s2);
            await writeTask;
            await TestBidirectionalStream(s1, s2);
        }

        private static async Task CreateAndTestUnidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            using QuicStream s1 = c1.OpenUnidirectionalStream();

            Assert.False(s1.CanRead);
            Assert.True(s1.CanWrite);

            ValueTask writeTask = s1.WriteAsync(s_data);

            using QuicStream s2 = await c2.AcceptStreamAsync();
            await ReceiveDataAsync(s_data, s2);
            await writeTask;
            await TestUnidirectionalStream(s1, s2);
        }

        private static async Task TestBidirectionalStream(QuicStream s1, QuicStream s2)
        {
            Assert.True(s1.CanRead);
            Assert.True(s1.CanWrite);
            Assert.True(s2.CanRead);
            Assert.True(s2.CanWrite);
            Assert.Equal(s1.StreamId, s2.StreamId);

            await SendAndReceiveDataAsync(s_data, s1, s2);
            await SendAndReceiveDataAsync(s_data, s2, s1);
            await SendAndReceiveDataAsync(s_data, s2, s1);
            await SendAndReceiveDataAsync(s_data, s1, s2);

            await SendAndReceiveEOFAsync(s1, s2);
            await SendAndReceiveEOFAsync(s2, s1);
        }

        private static async Task TestUnidirectionalStream(QuicStream s1, QuicStream s2)
        {
            Assert.False(s1.CanRead);
            Assert.True(s1.CanWrite);
            Assert.True(s2.CanRead);
            Assert.False(s2.CanWrite);
            Assert.Equal(s1.StreamId, s2.StreamId);

            await SendAndReceiveDataAsync(s_data, s1, s2);
            await SendAndReceiveDataAsync(s_data, s1, s2);

            await SendAndReceiveEOFAsync(s1, s2);
        }

        private static async Task SendAndReceiveDataAsync(ReadOnlyMemory<byte> data, QuicStream s1, QuicStream s2)
        {
            await s1.WriteAsync(data);
            await ReceiveDataAsync(data, s2);
        }

        private static async Task ReceiveDataAsync(ReadOnlyMemory<byte> data, QuicStream s)
        {
            Memory<byte> readBuffer = new byte[data.Length];

            int bytesRead = 0;
            while (bytesRead < data.Length)
            {
                bytesRead += await s.ReadAsync(readBuffer.Slice(bytesRead));
            }

            Assert.True(data.Span.SequenceEqual(readBuffer.Span));
        }

        private static async Task SendAndReceiveEOFAsync(QuicStream s1, QuicStream s2)
        {
            byte[] readBuffer = new byte[1];

            await s1.WriteAsync(Memory<byte>.Empty, endStream: true);
            await s1.ShutdownWriteCompleted();

            int bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);

            // Another read should still give EOF
            bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);
        }

        [Theory]
        [MemberData(nameof(ReadWrite_Random_Success_Data))]
        public async Task ReadWrite_Random_Success(int readSize, int writeSize)
        {
            byte[] testBuffer = new byte[8192];
            new Random().NextBytes(testBuffer);

            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = clientConnection.OpenUnidirectionalStream();

                    ReadOnlyMemory<byte> sendBuffer = testBuffer;
                    while (sendBuffer.Length != 0)
                    {
                        ReadOnlyMemory<byte> chunk = sendBuffer.Slice(0, Math.Min(sendBuffer.Length, writeSize));
                        await clientStream.WriteAsync(chunk);
                        sendBuffer = sendBuffer.Slice(chunk.Length);
                    }

                    clientStream.Shutdown();
                    await clientStream.ShutdownWriteCompleted();
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();

                    byte[] receiveBuffer = new byte[testBuffer.Length];
                    int totalBytesRead = 0;

                    while (totalBytesRead != receiveBuffer.Length)
                    {
                        int bytesRead = await serverStream.ReadAsync(receiveBuffer.AsMemory(totalBytesRead, Math.Min(receiveBuffer.Length - totalBytesRead, readSize)));
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        totalBytesRead += bytesRead;
                    }

                    Assert.True(receiveBuffer.AsSpan().SequenceEqual(testBuffer));
                });
        }

        public static IEnumerable<object[]> ReadWrite_Random_Success_Data()
        {
            IEnumerable<int> sizes = Enumerable.Range(1, 8).Append(2048).Append(8192);

            return
                from readSize in sizes
                from writeSize in sizes
                select new object[] { readSize, writeSize };
        }

        [Fact]
        public async Task Read_StreamAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener();
                ValueTask<QuicConnection> serverConnectionTask = listener.AcceptConnectionAsync();

                using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                await clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await serverConnectionTask;

                await using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                await clientStream.WriteAsync(new byte[1]);

                await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                await serverStream.ReadAsync(new byte[1]);

                clientStream.AbortWrite(ExpectedErrorCode);

                byte[] buffer = new byte[100];
                QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => serverStream.ReadAsync(buffer).AsTask());
                Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
            }).TimeoutAfter(millisecondsTimeout: 5_000);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/32050")]
        [Fact]
        public async Task Read_ConnectionAborted_Throws()
        {
            const int ExpectedErrorCode = 1234;

            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener();
                ValueTask<QuicConnection> serverConnectionTask = listener.AcceptConnectionAsync();

                using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                await clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await serverConnectionTask;

                await using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                await clientStream.WriteAsync(new byte[1]);

                await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                await serverStream.ReadAsync(new byte[1]);

                await clientConnection.CloseAsync(ExpectedErrorCode);

                byte[] buffer = new byte[100];
                QuicConnectionAbortedException ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => serverStream.ReadAsync(buffer).AsTask());
                Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
            }).TimeoutAfter(millisecondsTimeout: 5_000);
        }
    }

    public sealed class QuicStreamTests_MockProvider : QuicStreamTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicStreamTests_MsQuicProvider : QuicStreamTests<MsQuicProviderFactory> { }
}
