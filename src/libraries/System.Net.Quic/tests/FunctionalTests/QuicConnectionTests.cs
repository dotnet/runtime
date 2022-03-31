// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public abstract class QuicConnectionTests<T> : QuicTestBase<T>
        where T : IQuicImplProviderFactory, new()
    {
        const int ExpectedErrorCode = 1234;

        public QuicConnectionTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task TestConnect()
        {
            using QuicListener listener = CreateQuicListener();

            using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);

            Assert.False(clientConnection.Connected);
            Assert.Equal(listener.ListenEndPoint, clientConnection.RemoteEndPoint);

            ValueTask connectTask = clientConnection.ConnectAsync();
            ValueTask<QuicConnection> acceptTask = listener.AcceptConnectionAsync();

            await new Task[] { connectTask.AsTask(), acceptTask.AsTask() }.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);
            QuicConnection serverConnection = acceptTask.Result;

            Assert.True(clientConnection.Connected);
            Assert.True(serverConnection.Connected);
            Assert.Equal(listener.ListenEndPoint, serverConnection.LocalEndPoint);
            Assert.Equal(listener.ListenEndPoint, clientConnection.RemoteEndPoint);
            Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);
            Assert.Equal(ApplicationProtocol.ToString(), clientConnection.NegotiatedApplicationProtocol.ToString());
            Assert.Equal(ApplicationProtocol.ToString(), serverConnection.NegotiatedApplicationProtocol.ToString());
        }

        private static async Task<QuicStream> OpenAndUseStreamAsync(QuicConnection c)
        {
            QuicStream s = c.OpenBidirectionalStream();

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
                    Task<QuicStream> acceptTask = serverConnection.AcceptStreamAsync().AsTask();
                    Assert.False(acceptTask.IsCompleted);
                    Task<QuicStream> connectTask = OpenAndUseStreamAsync(serverConnection);
                    Assert.False(connectTask.IsCompleted);

                    await serverConnection.CloseAsync(ExpectedErrorCode);

                    sync.Release();

                    // Pending ops should fail
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask);
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => connectTask);

                    // Subsequent attempts should fail
                    // TODO: Which exception is correct?
                    if (IsMockProvider)
                    {
                        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await serverConnection.AcceptStreamAsync());
                        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await OpenAndUseStreamAsync(serverConnection));
                    }
                    else
                    {
                        await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverConnection.AcceptStreamAsync());
                        await Assert.ThrowsAsync<QuicException>(() => OpenAndUseStreamAsync(serverConnection));
                    }
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
                    Task<QuicStream> acceptTask = serverConnection.AcceptStreamAsync().AsTask();
                    Assert.False(acceptTask.IsCompleted);
                    Task<QuicStream> connectTask = OpenAndUseStreamAsync(serverConnection);
                    Assert.False(connectTask.IsCompleted);

                    serverConnection.Dispose();

                    sync.Release();

                    // Pending ops should fail
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask);
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => connectTask);

                    // Subsequent attempts should fail
                    // TODO: Should these be QuicOperationAbortedException, to match above? Or vice-versa?
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await serverConnection.AcceptStreamAsync());
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await OpenAndUseStreamAsync(serverConnection));
                });
        }

        [Fact]
        public async Task ConnectionClosedByPeer_WithPendingAcceptAndConnect_PendingAndSubsequentThrowConnectionAbortedException()
        {
            if (IsMockProvider)
            {
                return;
            }

            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await sync.WaitAsync();

                    await clientConnection.CloseAsync(ExpectedErrorCode);
                },
                async serverConnection =>
                {
                    // Pend operations before the client closes.
                    Task<QuicStream> acceptTask = serverConnection.AcceptStreamAsync().AsTask();
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

                    // Subsequent attempts should fail
                    ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => serverConnection.AcceptStreamAsync().AsTask());
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
            if (IsMockProvider)
            {
                return;
            }

            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    await clientConnection.CloseAsync(ExpectedErrorCode);

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
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

        [OuterLoop("Depends on IdleTimeout")]
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task Dispose_WithOpenLocalStream_LocalStreamFailsWithQuicOperationAbortedException(int writesBeforeClose)
        {
            if (IsMockProvider)
            {
                return;
            }

            // Set a short idle timeout so that after we dispose the connection, the peer will discover the connection is dead before too long.
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.IdleTimeout = TimeSpan.FromSeconds(1);

            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                    await DoWrites(clientStream, writesBeforeClose);

                    // Wait for peer to receive data
                    await sync.WaitAsync();

                    clientConnection.Dispose();

                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await clientStream.WriteAsync(new byte[1]));
                },
                async serverConnection =>
                {
                    using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                    await DoReads(serverStream, writesBeforeClose);

                    sync.Release();

                    // The client has done an abortive shutdown of the connection, which means we are not notified that the connection has closed.
                    // But the connection idle timeout should kick in and eventually we will get exceptions.
                    await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await serverStream.WriteAsync(new byte[1]));
                }, listenerOptions: listenerOptions);
        }
    }

    [ConditionalClass(typeof(QuicTestBase<MockProviderFactory>), nameof(QuicTestBase<MockProviderFactory>.IsSupported))]
    public sealed class QuicConnectionTests_MockProvider : QuicConnectionTests<MockProviderFactory>
    {
        public QuicConnectionTests_MockProvider(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicConnectionTests_MsQuicProvider : QuicConnectionTests<MsQuicProviderFactory>
    {
        public QuicConnectionTests_MsQuicProvider(ITestOutputHelper output) : base(output) { }
    }
}
