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
        public async Task AcceptStream_ConnectionAborted_ByClient_Throws()
        {
            const int ExpectedErrorCode = 1234;

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
    }

    public sealed class QuicConnectionTests_MockProvider : QuicConnectionTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicConnectionTests_MsQuicProvider : QuicConnectionTests<MsQuicProviderFactory> { }
}
