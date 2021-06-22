// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static byte[] s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task BasicTest()
        {
            await RunBidirectionalClientServer(
                iterations: 100,
                serverFunction: async stream =>
                {
                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await ReadAll(stream, buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    await stream.WriteAsync(s_data, endStream: true);
                },
                clientFunction: async stream =>
                {
                    await stream.WriteAsync(s_data, endStream: true);

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await ReadAll(stream, buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);
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

            await RunBidirectionalClientServer(
                iterations: 100,
                serverFunction: async stream =>
                {
                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);
                },
                clientFunction: async stream =>
                {
                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);
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
                }
            );
        }

        [Fact]
        public async Task GetStreamIdWithoutStartWorks()
        {
            using SemaphoreSlim sem = new SemaphoreSlim(0);
            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                    Assert.Equal(0, clientStream.StreamId);
                    sem.Release();

                },
                async serverConnection =>
                {
                    await sem.WaitAsync();

                    // TODO: stream that is opened by client but left unaccepted by server may cause AccessViolationException in its Finalizer
                    // explicitly closing the connections seems to help, but the problem should still be investigated, we should have a meaningful
                    // exception instead of AccessViolationException
                    await serverConnection.CloseAsync(0);
                });
        }

        [Fact]
        public async Task LargeDataSentAndReceived()
        {
            const int writeSize = 64 * 1024;
            const int NumberOfWrites = 256;       // total sent = 16M
            byte[] data = Enumerable.Range(0, writeSize * NumberOfWrites).Select(x => (byte)x).ToArray();

            await RunBidirectionalClientServer(
                iterations: 5,
                serverFunction: async stream =>
                {
                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertArrayEqual(data, buffer);

                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);
                },
                clientFunction: async stream =>
                {
                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertArrayEqual(data, buffer);
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

        [Theory]
        [MemberData(nameof(ReadWrite_Random_Success_Data))]
        public async Task ReadWrite_Random_Success(int readSize, int writeSize)
        {
            byte[] testBuffer = new byte[8192];
            Random.Shared.NextBytes(testBuffer);

            await RunUnidirectionalClientServer(
                async clientStream =>
                {
                    ReadOnlyMemory<byte> sendBuffer = testBuffer;
                    while (sendBuffer.Length != 0)
                    {
                        ReadOnlyMemory<byte> chunk = sendBuffer.Slice(0, Math.Min(sendBuffer.Length, writeSize));
                        await clientStream.WriteAsync(chunk);
                        sendBuffer = sendBuffer.Slice(chunk.Length);
                    }

                    clientStream.CompleteWrites();
                },
                async serverStream =>
                {
                    byte[] receiveBuffer = new byte[testBuffer.Length];
                    int totalBytesRead = 0;

                    while (true)
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
        public async Task Read_WriteAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1]);

                    await sem.WaitAsync();
                    clientStream.Abort(ExpectedErrorCode, QuicAbortDirection.Write);
                },
                async serverStream =>
                {
                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);

                    sem.Release();

                    byte[] buffer = new byte[100];
                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => serverStream.ReadAsync(buffer).AsTask());
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
                });
        }

        [Fact]
        public async Task Read_SynchronousCompletion_Success()
        {
            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1]);
                    sem.Release();
                    clientStream.CompleteWrites();
                    sem.Release();
                },
                async serverStream =>
                {
                    await sem.WaitAsync();
                    await Task.Delay(1000);

                    ValueTask<int> task = serverStream.ReadAsync(new byte[1]);
                    Assert.True(task.IsCompleted);

                    int received = await task;
                    Assert.Equal(1, received);

                    await sem.WaitAsync();
                    await Task.Delay(1000);

                    task = serverStream.ReadAsync(new byte[1]);
                    Assert.True(task.IsCompleted);

                    received = await task;
                    Assert.Equal(0, received);
                });
        }

        [Fact]
        public async Task ReadOutstanding_ReadAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await sem.WaitAsync();
                },
                async serverStream =>
                {
                    Task exTask = Assert.ThrowsAsync<QuicOperationAbortedException>(() => serverStream.ReadAsync(new byte[1]).AsTask());

                    Assert.False(exTask.IsCompleted);

                    serverStream.Abort(ExpectedErrorCode, QuicAbortDirection.Read);

                    await exTask;

                    sem.Release();
                });
        }

        [Fact]
        public async Task Read_ConcurrentReads_Throws()
        {
            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await sem.WaitAsync();
                },
                async serverStream =>
                {
                    ValueTask<int> readTask = serverStream.ReadAsync(new byte[1]);
                    Assert.False(readTask.IsCompleted);

                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await serverStream.ReadAsync(new byte[1]));

                    sem.Release();

                    int res = await readTask;
                    Assert.Equal(0, res);
                });
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

        [Fact]
        public async Task CloseAsync_Cancelled_Then_CloseAsync_Success()
        {
            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    // Make sure the first task throws an OCE.

                    using var cts = new CancellationTokenSource(500);

                    OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    {
                        await clientStream.CloseAsync(cts.Token);
                    });

                    Assert.Equal(cts.Token, oce.CancellationToken);

                    // Release before closing the stream, to allow the server to close its write stream.

                    sem.Release();
                },
                async serverStream =>
                {
                    // Wait before closing the stream, which would otherwise cause the client's CloseAsync to finish.

                    await sem.WaitAsync();
                });
        }

        // This tests the pattern needed to safely control shutdown of a QuicStream.
        // 1. Normal stream usage happens inside try.
        // 2. Call Abort(Both) in the catch.
        // 3. Call Close() with a cancellation token in the finally.
        // 4. If that Close() fails, call Abort(Immediate).
        //
        // This is important to avoid a DoS if the peer doesn't shutdown their sends but otherwise leaves the connection open.
        // TODO: we should rework the API to make this a lot more foolproof.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QuicStream_ClosePattern_Success(bool abortive)
        {
            while (!Debugger.IsAttached)
            {
                Console.WriteLine($"Attach to process {Process.GetCurrentProcess().Id}.");
                await Task.Delay(100);
            }

            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    // Don't shutdown client side until server side has 100% completed.
                    await sem.WaitAsync();

                    // Wait for server's aborts to reach us.
                    await Task.Delay(500);

                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(async () =>
                    {
                        await clientStream.WriteAsync(new byte[1]);
                    });

                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);

                    ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(async () =>
                    {
                        await clientStream.ReadAsync(new byte[1]);
                    });

                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
                },
                async serverStream =>
                {
                    try
                    {
                        // All the usual stream usage happens inside a try block.
                        // Just a dummy throw here to demonstrate the pattern...

                        if (abortive)
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        // Abort here. The CloseAsync that follows will still wait for an ACK of the shutdown.
                        serverStream.Abort(ExpectedErrorCode, QuicAbortDirection.Both);
                    }
                    finally
                    {
                        // Call CloseAsync() with a cancellation token to allow it to time out when peer doesn't shutdown.

                        using var shutdownCts = new CancellationTokenSource(500);
                        try
                        {
                            await serverStream.CloseAsync(shutdownCts.Token);
                        }
                        catch
                        {
                            // Abort (possibly again, which will ignore error code and not queue any new I/O).
                            // This time, Immediate is used which will cause CloseAsync() to not wait for a shutdown ACK.
                            serverStream.Abort(ExpectedErrorCode, QuicAbortDirection.Immediate);
                        }
                    }

                    // Either the CloseAsync above worked, in which case this is a no-op,
                    // or the stream has been re-aborted with Immediate, in which case this will complete "immediately" but not synchronously.
                    await serverStream.CloseAsync();

                    // Only allow the other side to close its stream after the dispose completes.
                    sem.Release();
                }, millisecondsTimeout: 1_000_000_000);
        }
    }

    public sealed class QuicStreamTests_MockProvider : QuicStreamTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicStreamTests_MsQuicProvider : QuicStreamTests<MsQuicProviderFactory> { }
}
