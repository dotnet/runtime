// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported))]
    public sealed class QuicConnectionTests : QuicTestBase
    {
        const int ExpectedErrorCode = 1234;

        public QuicConnectionTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task TestConnect()
        {
            await using QuicListener listener = await CreateQuicListener();

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(listener.LocalEndPoint);
            ValueTask<QuicConnection> acceptTask = listener.AcceptConnectionAsync();

            await new Task[] { connectTask.AsTask(), acceptTask.AsTask() }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
            await using QuicConnection serverConnection = acceptTask.Result;
            await using QuicConnection clientConnection = connectTask.Result;

            Assert.Equal(listener.LocalEndPoint, serverConnection.LocalEndPoint);
            Assert.Equal(listener.LocalEndPoint, clientConnection.RemoteEndPoint);
            Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);
            Assert.Equal(ApplicationProtocol.ToString(), clientConnection.NegotiatedApplicationProtocol.ToString());
            Assert.Equal(ApplicationProtocol.ToString(), serverConnection.NegotiatedApplicationProtocol.ToString());
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
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask);
                    // TODO: This may not always throw QuicOperationAbortedException due to a data race with MsQuic worker threads
                    // (CloseAsync may be processed before OpenStreamAsync as it is scheduled to the front of the operation queue)
                    // To be revisited once we standartize on exceptions.
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/55619")]
                    await Assert.ThrowsAnyAsync<QuicException>(() => connectTask);

                    // Subsequent attempts should fail
                    // TODO: Which exception is correct?
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverConnection.AcceptInboundStreamAsync());
                    await Assert.ThrowsAnyAsync<QuicException>(() => OpenAndUseStreamAsync(serverConnection));
                });
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
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask);

                    // TODO: This may not always throw QuicOperationAbortedException due to a data race with MsQuic worker threads
                    // (CloseAsync may be processed before OpenStreamAsync as it is scheduled to the front of the operation queue)
                    // To be revisited once we standardize on exceptions.
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/55619")]
                    await Assert.ThrowsAnyAsync<QuicException>(() => connectTask);

                    // Subsequent attempts should fail
                    // TODO: Should these be QuicOperationAbortedException, to match above? Or vice-versa?
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await serverConnection.AcceptInboundStreamAsync());
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await OpenAndUseStreamAsync(serverConnection));
                });
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
                    QuicConnectionAbortedException ex;

                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => acceptTask);
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => connectTask);
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);

                    await sync.WaitAsync();

                    // Subsequent attempts should fail
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => serverConnection.AcceptInboundStreamAsync().AsTask());
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => OpenAndUseStreamAsync(serverConnection));
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
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
                    using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.CloseAsync(ExpectedErrorCode);

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // Since the peer did the abort, we should receive the abort error code in the exception.
                    QuicConnectionAbortedException ex;
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.ReadAsync(new byte[1]));
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.WriteAsync(new byte[1]));
                    Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
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

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // Since the peer did the abort, we should receive the abort error code in the exception.
                    QuicConnectionAbortedException ex;
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.ReadAsync(new byte[1]));
                    Assert.Equal(DefaultCloseErrorCodeClient, ex.ErrorCode);
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.WriteAsync(new byte[1]));
                    Assert.Equal(DefaultCloseErrorCodeClient, ex.ErrorCode);
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
                    using QuicStream clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.DisposeAsync();

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptInboundStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // The client has done an abortive shutdown of the connection, which means we are not notified that the connection has closed.
                    // But the connection idle timeout should kick in and eventually we will get exceptions.
                    await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.WriteAsync(new byte[1]));
                }, listenerOptions: listenerOptions);
        }
    }
}
