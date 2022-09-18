// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported))]
    public sealed class QuicStreamTests : QuicTestBase
    {
        private static byte[] s_data = "Hello world!"u8.ToArray();
        public QuicStreamTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task BasicTest()
        {
            await RunClientServer(
                iterations: 100,
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await ReadAll(stream, buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    await stream.WriteAsync(s_data, completeWrites: true);
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    await stream.WriteAsync(s_data, completeWrites: true);

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
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(PassingTestTimeout);
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
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync(cts.Token);

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, completeWrites: true, cts.Token);
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data, cts.Token);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, completeWrites: true, cts.Token);

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
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();
                    await using QuicStream stream2 = await connection.AcceptInboundStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    byte[] buffer2 = new byte[s_data.Length];

                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.Equal(s_data, buffer);

                    int bytesRead2 = await ReadAll(stream2, buffer2);
                    Assert.Equal(s_data.Length, bytesRead2);
                    Assert.Equal(s_data, buffer2);

                    await stream.WriteAsync(s_data, completeWrites: true);
                    await stream2.WriteAsync(s_data, completeWrites: true);
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await using QuicStream stream2 = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    await stream.WriteAsync(s_data, completeWrites: true);
                    await stream2.WriteAsync(s_data, completeWrites: true);

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
        public async Task MultipleConcurrentStreamsOnSingleConnection()
        {
            const int count = 100;
            Task[] tasks = new Task[count];

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
            await using (clientConnection)
            await using (serverConnection)
            {
                for (int i = 0; i < count; i++)
                {
                    tasks[i] = MakeStreams(clientConnection, serverConnection);
                }
                await tasks.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
            }

            static async Task MakeStreams(QuicConnection clientConnection, QuicConnection serverConnection)
            {
                byte[] buffer = new byte[64];

                await using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Task writeTask = clientStream.WriteAsync("PING"u8.ToArray(), completeWrites: true).AsTask();
                Task<QuicStream> acceptTask = serverConnection.AcceptInboundStreamAsync().AsTask();
                await new Task[] { writeTask, acceptTask }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);

                await using QuicStream serverStream = acceptTask.Result;
                await serverStream.ReadAsync(buffer);
            }
        }

        [Fact]
        public async Task GetStreamIdWithoutStartWorks()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            await using (clientConnection)
            await using (serverConnection)
            {
                await using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Assert.Equal(0, clientStream.Id);

                // TODO: stream that is opened by client but left unaccepted by server may cause AccessViolationException in its Finalizer
                // explicitly closing the connections seems to help, but the problem should still be investigated, we should have a meaningful
                // exception instead of AccessViolationException
                await clientConnection.CloseAsync(0);
            }
        }

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
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertExtensions.SequenceEqual(data, buffer);

                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, completeWrites: true);
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    for (int pos = 0; pos < data.Length; pos += writeSize)
                    {
                        await stream.WriteAsync(data[pos..(pos + writeSize)]);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, completeWrites: true);

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertExtensions.SequenceEqual(data, buffer);
                }
            );
        }

        [Fact]
        public async Task TestStreams()
        {
            await using QuicListener listener = await CreateQuicListener();
            var clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.MaxInboundBidirectionalStreams = 1;
            clientOptions.MaxInboundUnidirectionalStreams = 1;
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listener);
            await using (clientConnection)
            await using (serverConnection)
            {
                Assert.Equal(listener.LocalEndPoint, serverConnection.LocalEndPoint);
                Assert.Equal(listener.LocalEndPoint, clientConnection.RemoteEndPoint);
                Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);

                await CreateAndTestBidirectionalStream(clientConnection, serverConnection);
                await CreateAndTestBidirectionalStream(serverConnection, clientConnection);
                await CreateAndTestUnidirectionalStream(serverConnection, clientConnection);
                await CreateAndTestUnidirectionalStream(clientConnection, serverConnection);
                await clientConnection.CloseAsync(errorCode: 0);
            }
        }

        private static async Task CreateAndTestBidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            await using QuicStream s1 = await c1.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            Assert.True(s1.CanRead);
            Assert.True(s1.CanWrite);

            ValueTask writeTask = s1.WriteAsync(s_data);

            await using QuicStream s2 = await c2.AcceptInboundStreamAsync();
            await ReceiveDataAsync(s_data, s2);
            await writeTask;
            await TestBidirectionalStream(s1, s2);
        }

        private static async Task CreateAndTestUnidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            await using QuicStream s1 = await c1.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);

            Assert.False(s1.CanRead);
            Assert.True(s1.CanWrite);

            ValueTask writeTask = s1.WriteAsync(s_data);

            await using QuicStream s2 = await c2.AcceptInboundStreamAsync();
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
            Assert.Equal(s1.Id, s2.Id);

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
            Assert.Equal(s1.Id, s2.Id);

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

            await s1.WriteAsync(Memory<byte>.Empty, completeWrites: true);

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

            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);

                    ReadOnlyMemory<byte> sendBuffer = testBuffer;
                    while (sendBuffer.Length != 0)
                    {
                        ReadOnlyMemory<byte> chunk = sendBuffer.Slice(0, Math.Min(sendBuffer.Length, writeSize));
                        await clientStream.WriteAsync(chunk);
                        sendBuffer = sendBuffer.Slice(chunk.Length);
                    }

                    await clientStream.WriteAsync(Memory<byte>.Empty, completeWrites: true);
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();

                    byte[] receiveBuffer = new byte[testBuffer.Length];
                    int totalBytesRead = 0;

                    while (true) // TODO: if you don't read until 0-byte read, ShutdownCompleted sometimes may not trigger - why?
                    {
                        Memory<byte> receiveChunkBuffer = receiveBuffer.AsMemory(totalBytesRead, Math.Min(receiveBuffer.Length - totalBytesRead, readSize));
                        int bytesRead = await serverStream.ReadAsync(receiveChunkBuffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        totalBytesRead += bytesRead;
                    }

                    Assert.Equal(testBuffer.Length, totalBytesRead);
                    AssertExtensions.SequenceEqual(testBuffer, receiveBuffer);
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
                    clientStream.Abort(QuicAbortDirection.Write, ExpectedErrorCode);
                },
                async serverStream =>
                {
                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);

                    sem.Release();

                    byte[] buffer = new byte[100];
                    QuicException ex = await AssertThrowsQuicExceptionAsync(QuicError.StreamAborted, () => serverStream.ReadAsync(buffer).AsTask());
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
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
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
            await using (clientConnection)
            await using (serverConnection)
            {
                byte[] buffer = new byte[1] { 42 };
                const int ExpectedErrorCode = 0xfffffff;

                QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Task<QuicStream> t = serverConnection.AcceptInboundStreamAsync().AsTask();
                await TaskTimeoutExtensions.WhenAllOrAnyFailed(clientStream.WriteAsync(buffer).AsTask(), t, PassingTestTimeoutMilliseconds);
                QuicStream serverStream = t.Result;
                Assert.Equal(1, await serverStream.ReadAsync(buffer));

                // streams are new established and in good shape.
                using (clientStream)
                using (serverStream)
                {
                    Task exTask = AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => serverStream.ReadAsync(new byte[1]).AsTask());
                    Assert.False(exTask.IsCompleted);

                    serverStream.Abort(QuicAbortDirection.Read, ExpectedErrorCode);

                    await exTask;
                }
            }
        }

        [Fact]
        public async Task WriteAbortedWithoutWriting_ReadThrows()
        {
            const long expectedErrorCode = 1234;

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
                    stream.Abort(QuicAbortDirection.Write, expectedErrorCode);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    byte[] buffer = new byte[1];

                    QuicException ex = await AssertThrowsQuicExceptionAsync(QuicError.StreamAborted, () => ReadAll(stream, buffer));
                    Assert.Equal(expectedErrorCode, ex.ApplicationErrorCode);

                    // We should still return true from CanRead, even though the read has been aborted.
                    Assert.True(stream.CanRead);
                }
            );
        }

        [Fact]
        public async Task ReadAbortedWithoutReading_WriteThrows()
        {
            const long expectedErrorCode = 1234;

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    stream.Abort(QuicAbortDirection.Read, expectedErrorCode);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    QuicException ex = await AssertThrowsQuicExceptionAsync(QuicError.StreamAborted, () => WriteForever(stream));
                    Assert.Equal(expectedErrorCode, ex.ApplicationErrorCode);

                    // We should still return true from CanWrite, even though the write has been aborted.
                    Assert.True(stream.CanWrite);
                }
            );
        }

        [Fact]
        public async Task WritePreCanceled_Throws()
        {
            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);

                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], cts.Token).AsTask());

                    // aborting write causes the write direction to throw on subsequent operations
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => stream.WriteAsync(new byte[1]).AsTask());
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    byte[] buffer = new byte[1024 * 1024];

                    QuicException ex = await AssertThrowsQuicExceptionAsync(QuicError.StreamAborted, () => ReadAll(stream, buffer));
                    Assert.Equal(DefaultStreamErrorCodeClient, ex.ApplicationErrorCode);
                }
            );
        }

        [Fact]
        public async Task WriteCanceled_NextWriteThrows()
        {
            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);

                    CancellationTokenSource cts = new CancellationTokenSource(500);

                    async Task WriteUntilCanceled()
                    {
                        var buffer = new byte[64 * 1024];
                        while (true)
                        {
                            await stream.WriteAsync(buffer, cancellationToken: cts.Token);
                        }
                    }

                    // a write would eventually be canceled
                    await Assert.ThrowsAsync<OperationCanceledException>(() => WriteUntilCanceled().WaitAsync(TimeSpan.FromSeconds(3)));

                    // next write would also throw
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => stream.WriteAsync(new byte[1]).AsTask());
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    async Task ReadUntilAborted()
                    {
                        var buffer = new byte[1024];
                        while (true)
                        {
                            int res = await stream.ReadAsync(buffer);
                            if (res == 0)
                            {
                                break;
                            }
                        }
                    }

                    QuicException ex = await AssertThrowsQuicExceptionAsync(QuicError.StreamAborted, () => ReadUntilAborted());
                    Assert.Equal(DefaultStreamErrorCodeClient, ex.ApplicationErrorCode);
                }
            );
        }

        [Fact]
        public async Task AbortAfterDispose_ProperlyOpenedStream_Success()
        {
            byte[] buffer = new byte[1] { 42 };
            var sem = new SemaphoreSlim(0);

            await RunClientServer(
                clientFunction: async connection =>
                {
                    QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    // Force stream to open on the wire
                    await stream.WriteAsync(buffer);
                    await sem.WaitAsync();

                    await stream.DisposeAsync();

                    // should not throw ODE on aborting
                    stream.Abort(QuicAbortDirection.Read, 1234);
                    stream.Abort(QuicAbortDirection.Write, 5675);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();
                    Assert.Equal(1, await stream.ReadAsync(buffer));
                    sem.Release();

                    // client will abort both sides, so we will receive the final event
                }
            );
        }

        [Fact]
        public async Task AbortAfterDispose_StreamCreationFlushedByDispose_Success()
        {
            await RunClientServer(
                clientFunction: async connection =>
                {
                    QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    // dispose will flush stream creation on the wire
                    await stream.DisposeAsync();

                    // should not throw ODE on aborting
                    stream.Abort(QuicAbortDirection.Read, 1234);
                    stream.Abort(QuicAbortDirection.Write, 5675);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    // client will abort both sides, so we will receive the final event
                }
            );
        }

        [Fact]
        public async Task WaitForWriteCompletionAsync_ClientReadAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            TaskCompletionSource<long> waitForAbortTcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
            SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], completeWrites: true);

                    // Wait for server to read data
                    await sem.WaitAsync();

                    clientStream.Abort(QuicAbortDirection.Read, ExpectedErrorCode);
                },
                async serverStream =>
                {
                    var writesClosedTask = ReleaseOnWritesClosedAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writesClosedTask.IsCompleted, "Server is still writing.");

                    // Tell client that data has been read and it can abort its reads.
                    sem.Release();

                    long sendAbortErrorCode = await waitForAbortTcs.Task;
                    Assert.Equal(ExpectedErrorCode, sendAbortErrorCode);

                    await writesClosedTask;

                    async ValueTask ReleaseOnWritesClosedAsync()
                    {
                        try
                        {
                            await serverStream.WritesClosed;
                            waitForAbortTcs.SetException(new Exception("WaitForWriteCompletionAsync didn't throw stream aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted)
                        {
                            waitForAbortTcs.SetResult(ex.ApplicationErrorCode.Value);
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }


        [Fact]
        public async Task WriteAsync_LocalAbort_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;
            SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                clientStream =>
                {
                    return sem.WaitAsync();
                },
                async serverStream =>
                {
                    // It may happen, that the WriteAsync call finishes early (before the AbortWrite
                    // below), and we hit a check on the next iteration of the WriteForever.
                    // But in most cases it will still exercise aborting the outstanding write task.

                    var writeTask = WriteForever(serverStream, 1024 * 1024);
                    serverStream.Abort(QuicAbortDirection.Write, ExpectedErrorCode);

                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => writeTask.WaitAsync(TimeSpan.FromSeconds(3)));
                    sem.Release();
                });
        }

        [Fact]
        public async Task WaitForWritesClosedAsync_ServerWriteAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;
            SemaphoreSlim sem = new SemaphoreSlim(0);

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], completeWrites: true);
                    await sem.WaitAsync();
                },
                async serverStream =>
                {
                    var writesClosedTask = ReleaseOnWritesClosedAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writesClosedTask.IsCompleted, "Server is still writing.");

                    serverStream.Abort(QuicAbortDirection.Write, ExpectedErrorCode);
                    sem.Release();

                    await waitForAbortTcs.Task;
                    await writesClosedTask;

                    async ValueTask ReleaseOnWritesClosedAsync()
                    {
                        try
                        {
                            await serverStream.WritesClosed;
                            waitForAbortTcs.SetException(new Exception("WaitForWriteCompletionAsync didn't throw operation aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.OperationAborted)
                        {
                            waitForAbortTcs.SetResult();
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }

        [Fact]
        public async Task WaitForReadsClosedAsync_ServerReadAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;
            SemaphoreSlim sem = new SemaphoreSlim(0);

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    Assert.Equal(1, await clientStream.ReadAsync(new byte[1]));
                    await clientStream.ReadsClosed;
                    Assert.Equal(0, await clientStream.ReadAsync(new byte[1]));
                    await sem.WaitAsync();
                },
                async serverStream =>
                {
                    var readsClosedTask = ReleaseOnReadsClosedAsync();

                    await serverStream.WriteAsync(new byte[1], completeWrites: true);
                    await serverStream.WritesClosed;

                    Assert.False(readsClosedTask.IsCompleted, "Server is still reading.");

                    serverStream.Abort(QuicAbortDirection.Read, ExpectedErrorCode);
                    sem.Release();

                    await waitForAbortTcs.Task;
                    await readsClosedTask;

                    async ValueTask ReleaseOnReadsClosedAsync()
                    {
                        try
                        {
                            await serverStream.ReadsClosed;
                            waitForAbortTcs.SetException(new Exception("ReadsClosed didn't throw operation aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.OperationAborted)
                        {
                            waitForAbortTcs.SetResult();
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }

        [Fact]
        public async Task WaitForWritesClosedAsync_ClientReadAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;
            SemaphoreSlim sem = new SemaphoreSlim(0);

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], completeWrites: true);
                    await sem.WaitAsync();
                    clientStream.Abort(QuicAbortDirection.Read, ExpectedErrorCode);
                },
                async serverStream =>
                {
                    var writesClosedTask = ReleaseOnWritesClosedAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writesClosedTask.IsCompleted, "Server is still writing.");

                    sem.Release();

                    await waitForAbortTcs.Task;
                    await writesClosedTask;

                    async ValueTask ReleaseOnWritesClosedAsync()
                    {
                        try
                        {
                            await serverStream.WritesClosed;
                            waitForAbortTcs.SetException(new Exception("WritesClosed didn't throw stream aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == ExpectedErrorCode)
                        {
                            waitForAbortTcs.SetResult();
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }

        [Fact]
        public async Task WaitForReadsClosedAsync_ClientWriteAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;
            SemaphoreSlim sem = new SemaphoreSlim(0);

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    Assert.Equal(1, await clientStream.ReadAsync(new byte[1]));
                    await clientStream.ReadsClosed;
                    Assert.Equal(0, await clientStream.ReadAsync(new byte[1]));
                    await sem.WaitAsync();
                    clientStream.Abort(QuicAbortDirection.Write, ExpectedErrorCode);
                },
                async serverStream =>
                {
                    var readsClosedTask = ReleaseOnReadsClosedAsync();

                    await serverStream.WriteAsync(new byte[1], completeWrites: true);
                    await serverStream.WritesClosed;

                    Assert.False(readsClosedTask.IsCompleted, "Server is still reading.");

                    sem.Release();

                    await waitForAbortTcs.Task;
                    await readsClosedTask;

                    async ValueTask ReleaseOnReadsClosedAsync()
                    {
                        try
                        {
                            await serverStream.ReadsClosed;
                            waitForAbortTcs.SetException(new Exception("ReadsClosed didn't throw stream aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == ExpectedErrorCode)
                        {
                            waitForAbortTcs.SetResult();
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }

        [Fact]
        public async Task WaitForWritesClosedAsync_ServerShutdown_Success()
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], completeWrites: true);

                    int readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);

                    readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                },
                async serverStream =>
                {
                    var writesClosedTask = serverStream.WritesClosed;

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    await serverStream.WriteAsync(new byte[1]);

                    Assert.False(writesClosedTask.IsCompleted, "Server is still writing.");

                    serverStream.CompleteWrites();

                    await writesClosedTask;
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WaitForReadsClosedAsync_ClientCompleteWrites_Success(bool extraCall)
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    Assert.Equal(1, await clientStream.ReadAsync(new byte[1]));
                    await clientStream.ReadsClosed;
                    Assert.Equal(0, await clientStream.ReadAsync(new byte[1]));

                    await clientStream.WriteAsync(new byte[1], completeWrites: !extraCall);
                    if (extraCall)
                    {
                        clientStream.CompleteWrites();
                    }
                },
                async serverStream =>
                {
                    var readsClosedTask = serverStream.ReadsClosed;

                    await serverStream.WriteAsync(new byte[1], completeWrites: true);
                    await serverStream.WritesClosed;

                    Assert.False(readsClosedTask.IsCompleted, "Server is still reading.");

                    var readCount = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);
                    readCount = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                    Assert.True(readsClosedTask.IsCompletedSuccessfully);
                });
        }

        [Fact]
        public async Task WaitForWritesClosedAsync_GracefulShutdown_Success()
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], completeWrites: true);

                    int readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);

                    readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                },
                async serverStream =>
                {
                    var writesClosedTask = serverStream.WritesClosed;

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writesClosedTask.IsCompleted, "Server is still writing.");

                    await serverStream.WriteAsync(new byte[1], completeWrites: true);

                    await writesClosedTask;
                });
        }

        [Fact]
        public async Task WaitForReadsClosedAsync_GracefulShutdown_Success()
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    Assert.Equal(1, await clientStream.ReadAsync(new byte[1]));
                    await clientStream.ReadsClosed;
                    Assert.Equal(0, await clientStream.ReadAsync(new byte[1]));

                    await clientStream.WriteAsync(new byte[1]);
                    // Let DisposeAsync gracefully shutdown the write side.
                },
                async serverStream =>
                {
                    var readsClosedTask = serverStream.ReadsClosed;

                    await serverStream.WriteAsync(new byte[1], completeWrites: true);
                    await serverStream.WritesClosed;

                    Assert.False(readsClosedTask.IsCompleted, "Server is still reading.");

                    var readCount = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);
                    readCount = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                    Assert.True(readsClosedTask.IsCompletedSuccessfully);
                });
        }

        [Fact]
        public async Task WaitForWritesClosedAsync_ConnectionClosed_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);
            TaskCompletionSource<long> waitForAbortTcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunClientServer(
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    var writesClosedTask = ReleaseOnWritesClosedAsync();

                    int received = await stream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await stream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    // Signal that the server has read data
                    sem.Release();

                    long closeErrorCode = await waitForAbortTcs.Task;
                    Assert.Equal(ExpectedErrorCode, closeErrorCode);

                    await writesClosedTask;

                    async ValueTask ReleaseOnWritesClosedAsync()
                    {
                        try
                        {
                            await stream.WritesClosed;
                            waitForAbortTcs.SetException(new Exception("WritesClosed didn't throw connection aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.ConnectionAborted)
                        {
                            waitForAbortTcs.SetResult(ex.ApplicationErrorCode.Value);
                        }
                    };
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    await stream.WriteAsync(new byte[1], completeWrites: true);

                    await stream.WritesClosed;

                    // Wait for the server to read data before closing the connection
                    await sem.WaitAsync();

                    await connection.CloseAsync(ExpectedErrorCode);
                }
            );
        }

        [Fact]
        public async Task WaitForReadsClosedAsync_ConnectionClosed_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);
            TaskCompletionSource<long> waitForAbortTcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunClientServer(
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();

                    var readsClosedTask = ReleaseOnReadsClosedAsync();

                    await stream.WriteAsync(new byte[1], completeWrites: true);

                    // Signal that the server has read data
                    sem.Release();

                    long closeErrorCode = await waitForAbortTcs.Task;
                    Assert.Equal(ExpectedErrorCode, closeErrorCode);

                    await readsClosedTask;

                    async ValueTask ReleaseOnReadsClosedAsync()
                    {
                        try
                        {
                            await stream.ReadsClosed;
                            waitForAbortTcs.SetException(new Exception("ReadsClosed didn't throw connection aborted."));
                        }
                        catch (QuicException ex) when (ex.QuicError == QuicError.ConnectionAborted)
                        {
                            waitForAbortTcs.SetResult(ex.ApplicationErrorCode.Value);
                            QuicException readEx = await Assert.ThrowsAsync<QuicException>(async () => await stream.ReadAsync(new byte[1]));
                            Assert.Equal(ex, readEx);
                        }
                    };
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    await stream.WriteAsync(new byte[1]);

                    Assert.Equal(1, await stream.ReadAsync(new byte[1]));
                    await stream.ReadsClosed;
                    Assert.Equal(0, await stream.ReadAsync(new byte[1]));

                    // Wait for the server to write data before closing the connection
                    await sem.WaitAsync();

                    await connection.CloseAsync(ExpectedErrorCode);
                }
            );
        }
    }
}
