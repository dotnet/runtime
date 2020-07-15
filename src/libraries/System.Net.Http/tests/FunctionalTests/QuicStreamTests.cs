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
    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public class QuicStreamTests : MsQuicTestBase
    {
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
}
