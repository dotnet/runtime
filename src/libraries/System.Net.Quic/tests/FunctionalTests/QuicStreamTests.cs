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
    public abstract class QuicStreamTests<T> : QuicTestBase<T>
         where T : IQuicImplProviderFactory, new()
    {
        private static byte[] s_data = Encoding.UTF8.GetBytes("Hello world!");
        public QuicStreamTests(ITestOutputHelper output) : base(output) { }

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
                    await using QuicStream stream = await connection.AcceptStreamAsync(cts.Token);

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true, cts.Token);

                    await stream.ShutdownCompleted(cts.Token);
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    for (int i = 0; i < sendCount; i++)
                    {
                        await stream.WriteAsync(s_data, cts.Token);
                    }
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true, cts.Token);

                    byte[] buffer = new byte[expectedBytesCount];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(expectedBytesCount, bytesRead);
                    Assert.Equal(expected, buffer);

                    await stream.ShutdownCompleted(cts.Token);
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
        public async Task MultipleConcurrentStreamsOnSingleConnection()
        {
            const int count = 100;
            Task[] tasks = new Task[count];

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
            using (clientConnection)
            using (serverConnection)
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
                QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                ValueTask writeTask = clientStream.WriteAsync(Encoding.UTF8.GetBytes("PING"), endStream: true);
                ValueTask<QuicStream> acceptTask = serverConnection.AcceptStreamAsync();
                await new Task[] { writeTask.AsTask(), acceptTask.AsTask() }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
                QuicStream serverStream = acceptTask.Result;
                await serverStream.ReadAsync(buffer);
            }
        }

        [Fact]
        public async Task GetStreamIdWithoutStartWorks()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            using (clientConnection)
            using (serverConnection)
            {
                using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                Assert.Equal(0, clientStream.StreamId);

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
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[data.Length];
                    int bytesRead = await ReadAll(stream, buffer);
                    Assert.Equal(data.Length, bytesRead);
                    AssertExtensions.SequenceEqual(data, buffer);

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
                    AssertExtensions.SequenceEqual(data, buffer);

                    await stream.ShutdownCompleted();
                }
            );
        }

        [Fact]
        public async Task TestStreams()
        {
            using QuicListener listener = CreateQuicListener();
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(listener);
            using (clientConnection)
            using (serverConnection)
            {
                Assert.True(clientConnection.Connected);
                Assert.True(serverConnection.Connected);
                Assert.Equal(listener.ListenEndPoint, serverConnection.LocalEndPoint);
                Assert.Equal(listener.ListenEndPoint, clientConnection.RemoteEndPoint);
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
        public async Task Read_WriteAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1]);

                    await sem.WaitAsync();
                    clientStream.AbortWrite(ExpectedErrorCode);
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
                    clientStream.Shutdown();
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
            // aborting doesn't work properly on mock
            if (typeof(T) == typeof(MockProviderFactory))
            {
                return;
            }

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
            using (clientConnection)
            using (serverConnection)
            {
                byte[] buffer = new byte[1] { 42 };
                const int ExpectedErrorCode = 0xfffffff;

                QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                Task<QuicStream> t = serverConnection.AcceptStreamAsync().AsTask();
                await TaskTimeoutExtensions.WhenAllOrAnyFailed(clientStream.WriteAsync(buffer).AsTask(), t, PassingTestTimeoutMilliseconds);
                QuicStream serverStream = t.Result;
                Assert.Equal(1, await serverStream.ReadAsync(buffer));

                // streams are new established and in good shape.
                using (clientStream)
                using (serverStream)
                {
                    Task exTask = Assert.ThrowsAsync<QuicOperationAbortedException>(() => serverStream.ReadAsync(new byte[1]).AsTask());
                    Assert.False(exTask.IsCompleted);

                    serverStream.AbortRead(ExpectedErrorCode);

                    await exTask;
                }
            }
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

        [Fact]
        public async Task WriteAbortedWithoutWriting_ReadThrows()
        {
            const long expectedErrorCode = 1234;

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenUnidirectionalStream();
                    stream.AbortWrite(expectedErrorCode);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[1];

                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => ReadAll(stream, buffer));
                    Assert.Equal(expectedErrorCode, ex.ErrorCode);

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
                    await using QuicStream stream = connection.OpenBidirectionalStream();
                    stream.AbortRead(expectedErrorCode);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => WriteForever(stream));
                    Assert.Equal(expectedErrorCode, ex.ErrorCode);

                    // We should still return true from CanWrite, even though the write has been aborted.
                    Assert.True(stream.CanWrite);
                }
            );
        }

        [Fact]
        public async Task WritePreCanceled_Throws()
        {
            const long expectedErrorCode = 1234;

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenUnidirectionalStream();

                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], cts.Token).AsTask());

                    // next write would also throw
                    await Assert.ThrowsAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1]).AsTask());

                    // manual write abort is still required
                    stream.AbortWrite(expectedErrorCode);

                    await stream.ShutdownCompleted();
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[1024 * 1024];

                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => ReadAll(stream, buffer));

                    await stream.ShutdownCompleted();
                }
            );
        }

        [Fact]
        public async Task WriteCanceled_NextWriteThrows()
        {
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/55995")]
            if (typeof(T) == typeof(MockProviderFactory))
            {
                return;
            }

            const long expectedErrorCode = 1234;

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenUnidirectionalStream();

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
                    await Assert.ThrowsAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1]).AsTask());

                    // manual write abort is still required
                    stream.AbortWrite(expectedErrorCode);

                    await stream.ShutdownCompleted();
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

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

                    QuicStreamAbortedException ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => ReadUntilAborted());

                    await stream.ShutdownCompleted();
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
                    QuicStream stream = connection.OpenBidirectionalStream();
                    // Force stream to open on the wire
                    await stream.WriteAsync(buffer);
                    await sem.WaitAsync();

                    stream.Dispose();

                    // should not throw ODE on aborting
                    stream.AbortRead(1234);
                    stream.AbortWrite(5675);
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    Assert.Equal(1, await stream.ReadAsync(buffer));
                    sem.Release();

                    // client will abort both sides, so we will receive the final event
                    await stream.ShutdownCompleted();
                }
            );
        }

        [Fact]
        public async Task AbortAfterDispose_StreamCreationFlushedByDispose_Success()
        {
            await RunClientServer(
                clientFunction: connection =>
                {
                    QuicStream stream = connection.OpenBidirectionalStream();

                    // dispose will flush stream creation on the wire
                    stream.Dispose();

                    // should not throw ODE on aborting
                    stream.AbortRead(1234);
                    stream.AbortWrite(5675);

                    return Task.CompletedTask;
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    // client will abort both sides, so we will receive the final event
                    await stream.ShutdownCompleted();
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
                    await clientStream.WriteAsync(new byte[1], endStream: true);

                    // Wait for server to read data
                    await sem.WaitAsync();

                    clientStream.AbortRead(ExpectedErrorCode);
                },
                async serverStream =>
                {
                    var writeCompletionTask = ReleaseOnWriteCompletionAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writeCompletionTask.IsCompleted, "Server is still writing.");

                    // Tell client that data has been read and it can abort its reads.
                    sem.Release();

                    long sendAbortErrorCode = await waitForAbortTcs.Task;
                    Assert.Equal(ExpectedErrorCode, sendAbortErrorCode);

                    await writeCompletionTask;

                    async ValueTask ReleaseOnWriteCompletionAsync()
                    {
                        try
                        {
                            await serverStream.WaitForWriteCompletionAsync();
                            waitForAbortTcs.SetException(new Exception("WaitForWriteCompletionAsync didn't throw stream aborted."));
                        }
                        catch (QuicStreamAbortedException ex)
                        {
                            waitForAbortTcs.SetResult(ex.ErrorCode);
                        }
                        catch (Exception ex)
                        {
                            waitForAbortTcs.SetException(ex);
                        }
                    };
                });
        }


        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67612")]
        public async Task WriteAsync_LocalAbort_Throws()
        {
            if (IsMockProvider)
            {
                // Mock provider does not support aborting pending writes via AbortWrite
                return;
            }

            const int ExpectedErrorCode = 0xfffffff;

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                clientStream =>
                {
                    return Task.CompletedTask;
                },
                async serverStream =>
                {
                    // It may happen, that the WriteAsync call finishes early (before the AbortWrite 
                    // below), and we hit a check on the next iteration of the WriteForever.
                    // But in most cases it will still exercise aborting the outstanding write task.

                    var writeTask = WriteForever(serverStream, 1024 * 1024);
                    serverStream.AbortWrite(ExpectedErrorCode);

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => writeTask.WaitAsync(TimeSpan.FromSeconds(3)));
                });
        }

        [Fact]
        public async Task WaitForWriteCompletionAsync_ServerWriteAborted_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            TaskCompletionSource waitForAbortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], endStream: true);
                },
                async serverStream =>
                {
                    var writeCompletionTask = ReleaseOnWriteCompletionAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writeCompletionTask.IsCompleted, "Server is still writing.");

                    serverStream.AbortWrite(ExpectedErrorCode);

                    await waitForAbortTcs.Task;
                    await writeCompletionTask;

                    async ValueTask ReleaseOnWriteCompletionAsync()
                    {
                        try
                        {
                            await serverStream.WaitForWriteCompletionAsync();
                            waitForAbortTcs.SetException(new Exception("WaitForWriteCompletionAsync didn't throw stream aborted."));
                        }
                        catch (QuicOperationAbortedException)
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
        public async Task WaitForWriteCompletionAsync_ServerShutdown_Success()
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], endStream: true);

                    int readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);

                    readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                },
                async serverStream =>
                {
                    var writeCompletionTask = serverStream.WaitForWriteCompletionAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    await serverStream.WriteAsync(new byte[1]);

                    Assert.False(writeCompletionTask.IsCompleted, "Server is still writing.");

                    serverStream.Shutdown();

                    await writeCompletionTask;
                });
        }

        [Fact]
        public async Task WaitForWriteCompletionAsync_GracefulShutdown_Success()
        {
            await RunBidirectionalClientServer(
                async clientStream =>
                {
                    await clientStream.WriteAsync(new byte[1], endStream: true);

                    int readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, readCount);

                    readCount = await clientStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, readCount);
                },
                async serverStream =>
                {
                    var writeCompletionTask = serverStream.WaitForWriteCompletionAsync();

                    int received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await serverStream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    Assert.False(writeCompletionTask.IsCompleted, "Server is still writing.");

                    await serverStream.WriteAsync(new byte[1], endStream: true);

                    await writeCompletionTask;
                });
        }

        [Fact]
        public async Task WaitForWriteCompletionAsync_ConnectionClosed_Throws()
        {
            const int ExpectedErrorCode = 0xfffffff;

            using SemaphoreSlim sem = new SemaphoreSlim(0);
            TaskCompletionSource<long> waitForAbortTcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

            await RunClientServer(
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    var writeCompletionTask = ReleaseOnWriteCompletionAsync();

                    int received = await stream.ReadAsync(new byte[1]);
                    Assert.Equal(1, received);
                    received = await stream.ReadAsync(new byte[1]);
                    Assert.Equal(0, received);

                    // Signal that the server has read data
                    sem.Release();

                    long closeErrorCode = await waitForAbortTcs.Task;
                    Assert.Equal(ExpectedErrorCode, closeErrorCode);

                    await writeCompletionTask;

                    async ValueTask ReleaseOnWriteCompletionAsync()
                    {
                        try
                        {
                            await stream.WaitForWriteCompletionAsync();
                            waitForAbortTcs.SetException(new Exception("WaitForWriteCompletionAsync didn't throw connection aborted."));
                        }
                        catch (QuicConnectionAbortedException ex)
                        {
                            waitForAbortTcs.SetResult(ex.ErrorCode);
                        }
                    };
                },
                clientFunction: async connection =>
                {
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    await stream.WriteAsync(new byte[1], endStream: true);

                    await stream.WaitForWriteCompletionAsync();

                    // Wait for the server to read data before closing the connection
                    await sem.WaitAsync();

                    await connection.CloseAsync(ExpectedErrorCode);
                }
            );
        }
    }

    [ConditionalClass(typeof(QuicTestBase<MockProviderFactory>), nameof(QuicTestBase<MockProviderFactory>.IsSupported))]
    public sealed class QuicStreamTests_MockProvider : QuicStreamTests<MockProviderFactory>
    {
        public QuicStreamTests_MockProvider(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    [Collection(nameof(DisableParallelization))]
    public sealed class QuicStreamTests_MsQuicProvider : QuicStreamTests<MsQuicProviderFactory>
    {
        public QuicStreamTests_MsQuicProvider(ITestOutputHelper output) : base(output) { }
    }
}
