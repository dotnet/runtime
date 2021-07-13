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

        [Fact]
        public async Task TestConnect()
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
            Assert.Equal(ApplicationProtocol.ToString(), clientConnection.NegotiatedApplicationProtocol.ToString());
            Assert.Equal(ApplicationProtocol.ToString(), serverConnection.NegotiatedApplicationProtocol.ToString());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55242", TestPlatforms.Linux)]
        public async Task AcceptStream_ConnectionAborted_ByClient_Throws()
        {
            using var sync = new SemaphoreSlim(0);

            await RunClientServer(
                async clientConnection =>
                {
                    await clientConnection.CloseAsync(ExpectedErrorCode);
                    sync.Release();
                },
                async serverConnection =>
                {
                    await sync.WaitAsync();
                    QuicConnectionAbortedException ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => serverConnection.AcceptStreamAsync().AsTask());
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
            if (typeof(T) == typeof(MockProviderFactory))
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
            if (typeof(T) == typeof(MockProviderFactory))
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
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverStream.ReadAsync(new byte[1]));
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverStream.WriteAsync(new byte[1]));
                }, listenerOptions: listenerOptions);
        }
    }

    public sealed class QuicConnectionTests_MockProvider : QuicConnectionTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicConnectionTests_MsQuicProvider : QuicConnectionTests<MsQuicProviderFactory> { }
}
