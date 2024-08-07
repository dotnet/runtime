// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [Collection(nameof(QuicTestCollection))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    public sealed class QuicConnectionTests : QuicTestBase
    {
        const int ExpectedErrorCode = 1234;
        public static IEnumerable<object[]> LocalAddresses = Configuration.Sockets.LocalAddresses();

        public QuicConnectionTests(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory]
        [MemberData(nameof(LocalAddresses))]
        public async Task TestConnect(IPAddress address)
        {
            await using QuicListener listener = await CreateQuicListener(address);
            Assert.Equal(address, listener.LocalEndPoint.Address);

            var options = CreateQuicClientOptions(listener.LocalEndPoint);
            ValueTask<QuicConnection> connectTask = CreateQuicConnection(options);
            ValueTask<QuicConnection> acceptTask = listener.AcceptConnectionAsync();

            await new Task[] { connectTask.AsTask(), acceptTask.AsTask() }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
            await using QuicConnection serverConnection = acceptTask.Result;
            await using QuicConnection clientConnection = connectTask.Result;

            Assert.Equal(listener.LocalEndPoint, serverConnection.LocalEndPoint);
            Assert.Equal(listener.LocalEndPoint, clientConnection.RemoteEndPoint);
            if (PlatformDetection.IsWindows && address.IsIPv6LinkLocal)
            {
                // https://github.com/microsoft/msquic/issues/3813
                Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);
            }
            Assert.Equal(ApplicationProtocol.ToString(), clientConnection.NegotiatedApplicationProtocol.ToString());
            Assert.Equal(ApplicationProtocol.ToString(), serverConnection.NegotiatedApplicationProtocol.ToString());
            Assert.Equal(options.ClientAuthenticationOptions.TargetHost, clientConnection.TargetHostName);
            Assert.Equal(options.ClientAuthenticationOptions.TargetHost, serverConnection.TargetHostName);
        }

        private static async Task<QuicStream> OpenAndUseStreamAsync(QuicConnection c)
        {
            QuicStream s = await c.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

            // This will pend
            await s.ReadAsync(new byte[1]);

            return s;
        }

        [Fact]
        public async Task CloseAsync_WithPendingAcceptAndConnect_PendingAndSubsequentThrowOperationAbortedException()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await sync.WaitAsync();
                },
                async serverConnection =>
                {
                    // Pend operations before the client closes.
                    Task<QuicStream> acceptTask = serverConnection.AcceptInboundStreamAsync().AsTask();
                    Assert.False(acceptTask.IsCompleted);
                    Task<QuicStream> connectTask = OpenAndUseStreamAsync(serverConnection);
                    Assert.False(connectTask.IsCompleted);

                    await serverConnection.CloseAsync(ExpectedErrorCode);

                    sync.Release();

                    // Pending ops should fail
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => acceptTask);
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, () => connectTask);

                    // Subsequent attempts should fail
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await serverConnection.AcceptInboundStreamAsync());
                    await Assert.ThrowsAsync<QuicException>(() => OpenAndUseStreamAsync(serverConnection));
                });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public async Task CloseAsync_InvalidCode_Throws(long errorCode)
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                clientConnection =>
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => clientConnection.CloseAsync(errorCode));
                    sync.Release();
                    return Task.CompletedTask;
                },
                async serverConnection =>
                {
                    await sync.WaitAsync();
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public async Task CloseAsync_PendingOpenStream_Throws(bool? localClose)
        {
            byte[] data = new byte[10];

            await using QuicListener listener = await CreateQuicListener(changeServerOptions: localClose is null ? options => options.IdleTimeout = TimeSpan.FromSeconds(10) : null);

            // Allow client to accept a stream, one will be accepted and another will be pending while we close the server connection.
            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.MaxInboundBidirectionalStreams = 1;
            await using QuicConnection clientConnection = await CreateQuicConnection(clientOptions);

            await using QuicConnection serverConnection = await listener.AcceptConnectionAsync();

            // Put one stream into server stream queue.
            QuicStream queuedStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            await queuedStream.WriteAsync(data.AsMemory(), completeWrites: true);

            // Open one stream to the client that is allowed.
            QuicStream firstStream = await serverConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            await firstStream.WriteAsync(data.AsMemory(), completeWrites: true);

            // Try to open another stream which should wait on capacity.
            ValueTask<QuicStream> secondStreamTask = serverConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            Assert.False(secondStreamTask.IsCompleted);

            // Close the connection, second stream task should complete with appropriate error.
            if (localClose is true)
            {
                await serverConnection.CloseAsync(123);
                await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await secondStreamTask);

                // Try to open yet another stream which should fail because of already closed connection.
                await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await serverConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional));
            }
            else if (localClose is false)
            {
                await clientConnection.CloseAsync(456);
                QuicException ex1 = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await secondStreamTask);
                Assert.Equal(456, ex1.ApplicationErrorCode);

                // Try to open yet another stream which should fail because of already closed connection.
                QuicException ex2 = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional));
                Assert.Equal(456, ex2.ApplicationErrorCode);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(15));

                QuicException ex1 = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionIdle, async () => await secondStreamTask);
                Assert.Equal(1, ex1.TransportErrorCode);

                // Try to open yet another stream which should fail because of already closed connection.
                QuicException ex2 = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionIdle, async () => await serverConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional));
                Assert.Equal(1, ex2.TransportErrorCode);
            }
        }

        [Fact]
        public async Task Dispose_WithPendingAcceptAndConnect_PendingAndSubsequentThrowOperationAbortedException()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await sync.WaitAsync();
                },
                async serverConnection =>
                {
                    // Pend operations before the client closes.
                    Task<QuicStream> acceptTask = serverConnection.AcceptInboundStreamAsync().AsTask();
                    Assert.False(acceptTask.IsCompleted);
                    Task<QuicStream> connectTask = OpenAndUseStreamAsync(serverConnection);
                    Assert.False(connectTask.IsCompleted);

                    await serverConnection.DisposeAsync();

                    sync.Release();

                    // Pending ops should fail
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask);
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await connectTask);

                    // Subsequent attempts should fail
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await serverConnection.AcceptInboundStreamAsync());
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await OpenAndUseStreamAsync(serverConnection));
                });
        }

        [Fact]
        public async Task DisposeAfterCloseCanceled()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    await Assert.ThrowsAsync<OperationCanceledException>(async () => await clientConnection.CloseAsync(ExpectedErrorCode, cts.Token));
                    await clientConnection.DisposeAsync();
                    sync.Release();
                },
                async serverConnection =>
                {
                    await sync.WaitAsync();
                    await serverConnection.DisposeAsync();
                });
        }

        [Fact]
        public async Task DisposeAfterCloseTaskStored()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    var cts = new CancellationTokenSource();
                    var task = clientConnection.CloseAsync(0).AsTask();
                    await clientConnection.DisposeAsync();
                    sync.Release();
                },
                async serverConnection =>
                {
                    await sync.WaitAsync();
                    await serverConnection.DisposeAsync();
                });
        }

        [Fact]
        public async Task GetStreamCapacity_OpenCloseStream_CountsCorrectly()
        {
            SemaphoreSlim streamsAvailableFired = new SemaphoreSlim(0);
            int bidiIncrement = -1, unidiIncrement = -1;

            var clientOptions = CreateQuicClientOptions(new IPEndPoint(0, 0));
            clientOptions.StreamCapacityCallback = (connection, args) =>
            {
                bidiIncrement = args.BidirectionalIncrement;
                unidiIncrement = args.UnidirectionalIncrement;
                streamsAvailableFired.Release();
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions);
            await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundBidirectionalStreams, bidiIncrement);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiIncrement);

            var clientStreamBidi = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            await clientStreamBidi.DisposeAsync();
            var serverStreamBidi = await serverConnection.AcceptInboundStreamAsync();
            await serverStreamBidi.DisposeAsync();

            // STREAMS_AVAILABLE event comes asynchronously, give it a chance to propagate
            await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
            Assert.Equal(1, bidiIncrement);
            Assert.Equal(0, unidiIncrement);

            var clientStreamUnidi = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
            await clientStreamUnidi.DisposeAsync();
            var serverStreamUnidi = await serverConnection.AcceptInboundStreamAsync();
            await serverStreamUnidi.DisposeAsync();

            // STREAMS_AVAILABLE event comes asynchronously, give it a chance to propagate
            await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
            Assert.Equal(0, bidiIncrement);
            Assert.Equal(1, unidiIncrement);

            await clientConnection.DisposeAsync();
            await serverConnection.DisposeAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetStreamCapacity_OpenCloseStreamIntoNegative_CountsCorrectly(bool unidirectional)
        {
            SemaphoreSlim streamsAvailableFired = new SemaphoreSlim(0);
            int bidiIncrement = -1, unidiIncrement = -1;
            int bidiTotal = 0;
            int unidiTotal = 0;

            var clientOptions = CreateQuicClientOptions(new IPEndPoint(0, 0));
            clientOptions.StreamCapacityCallback = (connection, args) =>
            {
                Interlocked.Exchange(ref bidiIncrement, args.BidirectionalIncrement);
                Interlocked.Exchange(ref unidiIncrement, args.UnidirectionalIncrement);
                Interlocked.Add(ref bidiTotal, args.BidirectionalIncrement);
                Interlocked.Add(ref unidiTotal, args.UnidirectionalIncrement);
                streamsAvailableFired.Release();
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions);
            await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundBidirectionalStreams, bidiIncrement);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiIncrement);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundBidirectionalStreams, bidiTotal);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiTotal);

            // Open # of streams up to the capacity.
            List<QuicStream> clientStreams = (await Task.WhenAll(Enumerable.Range(0, unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams)
                                                                           .Select(i => clientConnection.OpenOutboundStreamAsync(unidirectional ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional).AsTask())))
                                                                           .ToList();
            // Open another # of streams up to 2x capacity all together.
            CancellationTokenSource cts = new CancellationTokenSource();
            List<Task<QuicStream>> pendingClientStreams = Enumerable.Range(0, unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams)
                                                                    .Select(i => clientConnection.OpenOutboundStreamAsync(unidirectional ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional, cts.Token).AsTask())
                                                                    .ToList();
            foreach (var task in pendingClientStreams)
            {
                Assert.False(task.IsCompleted);
            }
            Assert.False(streamsAvailableFired.CurrentCount > 0);

            // Dispose streams to release capacity up to 0 (nothing gets reported yet).
            foreach (var clientStream in clientStreams)
            {
                await clientStream.DisposeAsync();
                await (await serverConnection.AcceptInboundStreamAsync()).DisposeAsync();
            }
            clientStreams.Clear();
            Assert.False(streamsAvailableFired.CurrentCount > 0);

            // All the pending streams should get accepted now.
            clientStreams.AddRange(await Task.WhenAll(pendingClientStreams));

            // Disposing the pending streams now should lead to stream capacity increments.
            bool first = true; // The stream capacity is cumulatively reported only after the STREAMS_AVAILABLE reached over 0.
            foreach (var clientStream in clientStreams)
            {
                await clientStream.DisposeAsync();
                await (await serverConnection.AcceptInboundStreamAsync()).DisposeAsync();
                await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
                Assert.Equal(unidirectional ? 0 : (first ? QuicDefaults.DefaultServerMaxInboundBidirectionalStreams + 1 : 1), bidiIncrement);
                Assert.Equal(unidirectional ? (first ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams + 1 : 1) : 0, unidiIncrement);
                first = false;
            }
            Assert.False(streamsAvailableFired.CurrentCount > 0);
            Assert.Equal(unidirectional ? QuicDefaults.DefaultServerMaxInboundBidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams * 3, bidiTotal);
            Assert.Equal(unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams * 3 : QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiTotal);

            await clientConnection.DisposeAsync();
            await serverConnection.DisposeAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetStreamCapacity_OpenCloseStreamCanceledIntoNegative_CountsCorrectly(bool unidirectional)
        {
            SemaphoreSlim streamsAvailableFired = new SemaphoreSlim(0);
            int bidiIncrement = -1, unidiIncrement = -1;
            int bidiTotal = 0;
            int unidiTotal = 0;

            var clientOptions = CreateQuicClientOptions(new IPEndPoint(0, 0));
            clientOptions.StreamCapacityCallback = (connection, args) =>
            {
                Interlocked.Exchange(ref bidiIncrement, args.BidirectionalIncrement);
                Interlocked.Exchange(ref unidiIncrement, args.UnidirectionalIncrement);
                Interlocked.Add(ref bidiTotal, args.BidirectionalIncrement);
                Interlocked.Add(ref unidiTotal, args.UnidirectionalIncrement);
                streamsAvailableFired.Release();
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions);
            await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundBidirectionalStreams, bidiIncrement);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiIncrement);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundBidirectionalStreams, bidiTotal);
            Assert.Equal(QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiTotal);

            // Open # of streams up to the capacity.
            List<QuicStream> clientStreams = (await Task.WhenAll(Enumerable.Range(0, unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams)
                                                                           .Select(i => clientConnection.OpenOutboundStreamAsync(unidirectional ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional).AsTask())))
                                                                           .ToList();
            // Open another # of streams up to 2x capacity all together.
            CancellationTokenSource cts = new CancellationTokenSource();
            List<Task<QuicStream>> pendingClientStreams = Enumerable.Range(0, unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams)
                                                                    .Select(i => clientConnection.OpenOutboundStreamAsync(unidirectional ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional, cts.Token).AsTask())
                                                                    .ToList();
            foreach (var task in pendingClientStreams)
            {
                Assert.False(task.IsCompleted);
            }
            Assert.False(streamsAvailableFired.CurrentCount > 0);

            // Cancel pending streams if requested.
            cts.Cancel();

            // Dispose streams to release capacity up to 0 (nothing gets reported yet).
            foreach (var clientStream in clientStreams)
            {
                await clientStream.DisposeAsync();
                await (await serverConnection.AcceptInboundStreamAsync()).DisposeAsync();
            }
            clientStreams.Clear();
            Assert.False(streamsAvailableFired.CurrentCount > 0);

            // Pending streams should get cancelled and disposing the streams now should lead to stream capacity increments.
            bool first = true; // The stream capacity is cumulatively reported only after the STREAMS_AVAILABLE reached over 0.
            foreach (var cancelledStream in pendingClientStreams)
            {
                Assert.True(cancelledStream.IsCanceled);
                await (await serverConnection.AcceptInboundStreamAsync()).DisposeAsync();
                await streamsAvailableFired.WaitAsync().WaitAsync(PassingTestTimeout);
                Assert.Equal(unidirectional ? 0 : (first ? QuicDefaults.DefaultServerMaxInboundBidirectionalStreams + 1 : 1), bidiIncrement);
                Assert.Equal(unidirectional ? (first ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams + 1 : 1) : 0, unidiIncrement);
                first = false;
            }
            Assert.False(streamsAvailableFired.CurrentCount > 0);
            Assert.Equal(unidirectional ? QuicDefaults.DefaultServerMaxInboundBidirectionalStreams : QuicDefaults.DefaultServerMaxInboundBidirectionalStreams * 3, bidiTotal);
            Assert.Equal(unidirectional ? QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams * 3 : QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams, unidiTotal);

            await clientConnection.DisposeAsync();
            await serverConnection.DisposeAsync();
        }

        [Fact]
        public async Task GetStreamCapacity_SumInvariant()
        {
            int maxStreamIndex = 0;
            const int Limit = 5;

            var clientOptions = CreateQuicClientOptions(new IPEndPoint(0, 0));
            clientOptions.StreamCapacityCallback = (connection, args) =>
            {
                Interlocked.Add(ref maxStreamIndex, args.BidirectionalIncrement);
            };

            var listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ConnectionOptionsCallback = (_, _, _) =>
            {
                var options = CreateQuicServerOptions();
                options.MaxInboundBidirectionalStreams = Limit;
                return ValueTask.FromResult(options);
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listenerOptions);

            Assert.Equal(Limit, maxStreamIndex);

            Queue<(QuicStream client, QuicStream server)> streams = new();

            for (int i = 0; i < Limit; i++)
            {
                QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                await clientStream.WriteAsync(new byte[1]);
                QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                streams.Enqueue((clientStream, serverStream));
            }

            Queue<Task<QuicStream>> tasks = new();
            // enqueue more stream creations
            for (int i = 0; i < Limit; i++)
            {
                var newClientStreamTask = clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Assert.False(newClientStreamTask.IsCompleted, "Stream creation should not be completed synchronously");
                tasks.Enqueue(newClientStreamTask.AsTask());
            }

            // dispose streams
            while (streams.Count > 0)
            {
                var (clientStream, serverStream) = streams.Dequeue();
                await clientStream.DisposeAsync();
                await serverStream.DisposeAsync();

                if (tasks.TryDequeue(out var task))
                {
                    clientStream = await task;
                    await clientStream.WriteAsync(new byte[1]);
                    serverStream = await serverConnection.AcceptInboundStreamAsync();
                    streams.Enqueue((clientStream, serverStream));
                }
            }

            // give time to update the count
            await Task.Delay(1000);

            // by now, we opened and closed 2 * Limit, and expect a budget of 'Limit' more
            Assert.Equal(3 * Limit, maxStreamIndex);

            await clientConnection.DisposeAsync();
            await serverConnection.DisposeAsync();
        }

        [Fact]
        public async Task ConnectionClosedByPeer_WithPendingAcceptAndConnect_PendingAndSubsequentThrowConnectionAbortedException()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await sync.WaitAsync();

                    await clientConnection.CloseAsync(ExpectedErrorCode);

                    sync.Release();
                },
                async serverConnection =>
                {
                    // Pend operations before the client closes.
                    Task<QuicStream> acceptTask = serverConnection.AcceptInboundStreamAsync().AsTask();
                    Assert.False(acceptTask.IsCompleted);
                    Task<QuicStream> connectTask = OpenAndUseStreamAsync(serverConnection);
                    Assert.False(connectTask.IsCompleted);

                    sync.Release();

                    // Pending ops should fail
                    QuicException ex;

                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, () => acceptTask);
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, () => connectTask);
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);

                    await sync.WaitAsync();

                    // Subsequent attempts should fail
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, () => serverConnection.AcceptInboundStreamAsync().AsTask());
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, () => OpenAndUseStreamAsync(serverConnection));
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
                });
        }

        private static async Task DoWrites(QuicStream writer, int writeCount)
        {
            for (int i = 0; i < writeCount; i++)
            {
                await writer.WriteAsync(new byte[1]);
            }
        }

        private static async Task DoReads(QuicStream reader, int readCount)
        {
            for (int i = 0; i < readCount; i++)
            {
                int bytesRead = await reader.ReadAsync(new byte[1]);
                Assert.Equal(1, bytesRead);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task CloseAsync_WithOpenStream_LocalAndPeerStreamsFailWithQuicOperationAbortedException(int writesBeforeClose)
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.CloseAsync(ExpectedErrorCode);

                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.ReadAsync(new byte[1]));
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // Since the peer did the abort, we should receive the abort error code in the exception.
                    QuicException ex;
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.ReadAsync(new byte[1]));
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.WriteAsync(new byte[1]));
                    Assert.Equal(ExpectedErrorCode, ex.ApplicationErrorCode);
                });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task Dispose_WithoutClose_ConnectionClosesWithDefault(int writesBeforeClose)
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();

            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.DisposeAsync();

                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.ReadAsync(new byte[1]));
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // Since the peer did the abort, we should receive the abort error code in the exception.
                    QuicException ex;
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.ReadAsync(new byte[1]));
                    Assert.Equal(DefaultCloseErrorCodeClient, ex.ApplicationErrorCode);
                    ex = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.WriteAsync(new byte[1]));
                    Assert.Equal(DefaultCloseErrorCodeClient, ex.ApplicationErrorCode);
                }, listenerOptions: listenerOptions);
        }

        [OuterLoop("Depends on IdleTimeout")]
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task Dispose_WithOpenLocalStream_LocalStreamFailsWithQuicOperationAbortedException(int writesBeforeClose)
        {
            // Set a short idle timeout so that after we dispose the connection, the peer will discover the connection is dead before too long.
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();

            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.DisposeAsync();

                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.ReadAsync(new byte[1]));
                    await AssertThrowsQuicExceptionAsync(QuicError.OperationAborted, async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // The client has done an abortive shutdown of the connection, which means we are not notified that the connection has closed.
                    // But the connection idle timeout should kick in and eventually we will get exceptions.
                    await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.ReadAsync(new byte[1]));
                    await AssertThrowsQuicExceptionAsync(QuicError.ConnectionAborted, async () => await serverStream.WriteAsync(new byte[1]));
                }, listenerOptions: listenerOptions);
        }

        [Fact]
        public async Task AcceptAsync_NoCapacity_Throws()
        {
            await RunClientServer(
                async clientConnection =>
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await clientConnection.AcceptInboundStreamAsync());
                },
                _ => Task.CompletedTask);
        }

        [Fact]
        public async Task AcceptStreamAsync_ConnectionDisposed_Throws()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            // One task issues before the disposal.
            ValueTask<QuicStream> acceptTask1 = serverConnection.AcceptInboundStreamAsync();
            await serverConnection.DisposeAsync();
            // Another task issued after the disposal.
            ValueTask<QuicStream> acceptTask2 = serverConnection.AcceptInboundStreamAsync();

            var accept1Exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask1);
            var accept2Exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask2);

            await clientConnection.DisposeAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Connect_PeerCertificateDisposed(bool useGetter)
        {
            await using QuicListener listener = await CreateQuicListener();

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            X509Certificate? peerCertificate = null;
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                peerCertificate = certificate;
                return true;
            };

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(clientOptions);
            ValueTask<QuicConnection> acceptTask = listener.AcceptConnectionAsync();

            await new Task[] { connectTask.AsTask(), acceptTask.AsTask() }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
            await using QuicConnection serverConnection = acceptTask.Result;
            QuicConnection clientConnection = connectTask.Result;

            Assert.NotNull(peerCertificate);
            if (useGetter)
            {
                Assert.Equal(peerCertificate, clientConnection.RemoteCertificate);
            }
            // Dispose connection, if we touched RemoteCertificate (useGetter), the cert should not be disposed; otherwise, it should be disposed.
            await clientConnection.DisposeAsync();
            if (useGetter)
            {
                Assert.NotEqual(IntPtr.Zero, peerCertificate.Handle);
            }
            else
            {
                Assert.Equal(IntPtr.Zero, peerCertificate.Handle);
            }
            peerCertificate.Dispose();
        }

        [Fact]
        public async Task Connection_AwaitsStream_ConnectionSurvivesGC()
        {
            const byte data = 0xDC;

            TaskCompletionSource<IPEndPoint> listenerEndpointTcs = new TaskCompletionSource<IPEndPoint>();
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    await using var listener = await CreateQuicListener();
                    listenerEndpointTcs.SetResult(listener.LocalEndPoint);
                    await using var connection = await listener.AcceptConnectionAsync();
                    await using var stream = await connection.AcceptInboundStreamAsync();
                    var buffer = new byte[1];
                    Assert.Equal(1, await stream.ReadAsync(buffer));
                    Assert.Equal(data, buffer[0]);
                }).WaitAsync(TimeSpan.FromSeconds(5)),
                Task.Run(async () =>
                {
                    var endpoint = await listenerEndpointTcs.Task;
                    await using var connection = await CreateQuicConnection(endpoint);
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                    GC.Collect();
                    await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
                    await stream.WriteAsync(new byte[1] { data }, completeWrites: true);
                }).WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConnectAsync_InvalidName_ThrowsSocketException(bool sameTargetHost)
        {
            string name = $"{Guid.NewGuid().ToString("N")}.microsoft.com.";
            var options = new QuicClientConnectionOptions()
            {
                DefaultStreamErrorCode = DefaultStreamErrorCodeClient,
                DefaultCloseErrorCode = DefaultCloseErrorCodeClient,
                RemoteEndPoint = new DnsEndPoint(name, 10000),
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions(sameTargetHost ? name : "localhost")
            };

            SocketException ex = await Assert.ThrowsAsync<SocketException>(() => QuicConnection.ConnectAsync(options).AsTask());
            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);
        }

        [Fact]
        public void ConnectAsync_MissingName_ThrowsInvalidArgument()
        {
            var options = new QuicClientConnectionOptions()
            {
                DefaultStreamErrorCode = DefaultStreamErrorCodeClient,
                DefaultCloseErrorCode = DefaultCloseErrorCodeClient,
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };

            Assert.Throws<ArgumentNullException>(() => QuicConnection.ConnectAsync(options));
        }

        [Fact]
        public void ConnectAsync_MissingDefaults_ThrowsInvalidArgument()
        {
            var options = new QuicClientConnectionOptions()
            {
                RemoteEndPoint = new DnsEndPoint("localhost", 10000),
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => QuicConnection.ConnectAsync(options));
        }
    }
}
