// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public abstract class QuicStreamTests<T> : QuicTestBase<T>
         where T : IQuicImplProviderFactory, new()
    {
        private static byte[] s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task BasicTest()
        {
            await RunClientServer(
                iterations: 100,
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await ReadAll(stream, buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream.ShutdownCompleted();
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    await stream.WriteAsync(s_data, endStream: true);

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await ReadAll(stream, buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    await stream.ShutdownCompleted();
                }
            );
        }

        [Fact]
        public async Task MultipleReadsAndWrites()
        {
            const int sendCount = 5;
            int expectedBytesCount = s_data.Length * sendCount;
            byte[] expected = new byte[expectedBytesCount];
            Memory<byte> m = expected;
            for (int i = 0; i < sendCount; i++)
            {
                s_data.CopyTo(m);
                m = m[s_data.Length..];
            }

            await RunClientServer(
                iterations: 100,
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    await stream.ShutdownCompleted();
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    await stream.ShutdownCompleted();
                }
            );
        }

        [Fact]
        public async Task MultipleStreamsOnSingleConnection()
        {
            await RunClientServer(
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    await using QuicStream stream2 = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    byte[] buffer2 = new byte[s_data.Length];

                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    int bytesRead2 = await ReadAll(stream2, buffer2);
                    Assert.Equal(s_data.Length, bytesRead2);
                    Assert.Equal(s_data, buffer2);

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream2.WriteAsync(s_data, endStream: true);

                    await stream.ShutdownCompleted();
                    await stream2.ShutdownCompleted();
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();
                    await using QuicStream stream2 = connection.OpenBidirectionalStream();

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream2.WriteAsync(s_data, endStream: true);

                    byte[] buffer = new byte[s_data.Length];
                    byte[] buffer2 = new byte[s_data.Length];

                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    int bytesRead2 = await ReadAll(stream2, buffer2);
                    Assert.Equal(s_data.Length, bytesRead2);
                    Assert.Equal(s_data, buffer2);

                    await stream.ShutdownCompleted();
                    await stream2.ShutdownCompleted();
                }
            );
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

            // TODO: stream that is opened by client but left unaccepted by server may cause AccessViolationException in its Finalizer
            // explicitly closing the connections seems to help, but the problem should still be investigated, we should have a meaningful
            // exception instead of AccessViolationException
            await clientConnection.CloseAsync(0);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/52047")]
        [Fact]
        public async Task LargeDataSentAndReceived()
        {
            const int writeSize = 64 * 1024;
            const int NumberOfWrites = 256;       // total sent = 16M
            byte[] data = Enumerable.Range(0, writeSize * NumberOfWrites).Select(x => (byte)x).ToArray();

            await RunClientServer(
                iterations: 5,
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertArrayEqual(data, buffer);

                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    await stream.ShutdownCompleted();
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertArrayEqual(data, buffer);

                    await stream.ShutdownCompleted();
                }
            );
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
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
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

            await s1.ShutdownCompleted();
            await s2.ShutdownCompleted();
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

            await s1.ShutdownCompleted();
            await s2.ShutdownCompleted();
        }

        private static async Task SendAndReceiveDataAsync(byte[] data, QuicStream s1, QuicStream s2)
        {
            await s1.WriteAsync(data);
            await ReceiveDataAsync(data, s2);
        }

        private static async Task ReceiveDataAsync(byte[] data, QuicStream s)
        {
            byte[] readBuffer = new byte[data.Length];

            int bytesRead = 0;
            while (bytesRead < data.Length)
            {
                bytesRead += await s.ReadAsync(readBuffer.AsMemory(bytesRead));
            }

            Assert.Equal(data.Length, bytesRead);
            Assert.Equal(s_data, readBuffer);
        }

        private static async Task SendAndReceiveEOFAsync(QuicStream s1, QuicStream s2)
        {
            byte[] readBuffer = new byte[1];

            await s1.WriteAsync(Memory<byte>.Empty, endStream: true);

            int bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);

            // Another read should still give EOF
            bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/52047")]
        [Theory]
        [MemberData(nameof(ReadWrite_Random_Success_Data))]
        public async Task ReadWrite_Random_Success(int readSize, int writeSize)
        {
            byte[] testBuffer = new byte[8192];
            Random.Shared.NextBytes(testBuffer);

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

                    await clientStream.WriteAsync(Memory<byte>.Empty, endStream: true);
                    await clientStream.ShutdownCompleted();
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();

                    byte[] receiveBuffer = new byte[testBuffer.Length];
                    int totalBytesRead = 0;

                    while (true) // TODO: if you don't read until 0-byte read, ShutdownCompleted sometimes may not trigger - why?
                    {
                        Memory<byte> recieveChunkBuffer = receiveBuffer.AsMemory(totalBytesRead, Math.Min(receiveBuffer.Length - totalBytesRead, readSize));
                        int bytesRead = await serverStream.ReadAsync(recieveChunkBuffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        totalBytesRead += bytesRead;
                    }

                    Assert.Equal(testBuffer.Length, totalBytesRead);
                    AssertArrayEqual(testBuffer, receiveBuffer);

                    await serverStream.ShutdownCompleted();
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
            }).WaitAsync(TimeSpan.FromSeconds(5));
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
            }).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    public sealed class QuicStreamTests_MockProvider : QuicStreamTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicStreamTests_MsQuicProvider : QuicStreamTests<MsQuicProviderFactory> { }
}
