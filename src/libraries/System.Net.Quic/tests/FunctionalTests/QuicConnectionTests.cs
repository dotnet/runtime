// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [Collection(nameof(QuicTestCollection))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91757", typeof(PlatformDetection), nameof(PlatformDetection.IsAlpine), nameof(PlatformDetection.IsArmProcess))]
    public sealed class QuicConnectionTests : QuicTestBase
    {
        const int ExpectedErrorCode = 1234;
        public static IEnumerable<object[]> LocalAddresses = Configuration.Sockets.LocalAddresses();

        public QuicConnectionTests(ITestOutputHelper output) : base(output) { }

        [Theory]
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
