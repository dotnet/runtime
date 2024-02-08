// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(DisableParallelization))]
    [Collection(nameof(QuicCountersListener))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    public sealed class QuicListenerTests : QuicTestBase
    {
        public QuicListenerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Listener_Backlog_Success()
        {
            await Task.Run(async () =>
            {
                await using QuicListener listener = await CreateQuicListener();

                var clientStreamTask = CreateQuicConnection(listener.LocalEndPoint);
                await using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await using QuicConnection clientConnection = await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }

        [ConditionalFact(nameof(IsIPv6Available))]
        public async Task Listener_Backlog_Success_IPv6()
        {
            await Task.Run(async () =>
            {
                await using QuicListener listener = await CreateQuicListener(new IPEndPoint(IPAddress.IPv6Loopback, 0));

                var clientStreamTask = CreateQuicConnection(listener.LocalEndPoint);
                await using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await using QuicConnection clientConnection = await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }

        [Fact]
        public async Task Listener_IPv6Any_Accepts_IPv4()
        {
            await Task.Run(async () =>
            {
                // QuicListener has special behavior for IPv6Any (listening on all IP addresses, i.e. including IPv4).
                // Use a copy of IPAddress.IPv6Any to make sure address detection doesn't rely on reference equality comparison.
                IPAddress IPv6Any = new IPAddress((ReadOnlySpan<byte>)new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);

                await using QuicListener listener = await CreateQuicListener(new IPEndPoint(IPv6Any, 0));

                var clientStreamTask = CreateQuicConnection(new IPEndPoint(IPAddress.Loopback, listener.LocalEndPoint.Port));
                await using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await using QuicConnection clientConnection = await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }

        [Fact]
        public async Task AcceptConnectionAsync_InvalidConnectionOptions_Throws()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            // Do not set any options, which should throw an argument exception from accept.
            listenerOptions.ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(new QuicServerConnectionOptions());
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(listener.LocalEndPoint);
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await listener.AcceptConnectionAsync());
        }

        [Fact]
        public void ListenAsync_MissingAlpn_Throws()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ApplicationProtocols = null;

            Assert.Throws<ArgumentNullException>(() => QuicListener.ListenAsync(listenerOptions));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AcceptConnectionAsync_ThrowingOptionsCallback_Throws(bool useFromException)
        {
            const string expectedMessage = "Expected Message";

            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            // Throw an exception, which should throw the same from accept.
            listenerOptions.ConnectionOptionsCallback = (_, _, _) => useFromException ? ValueTask.FromException<QuicServerConnectionOptions>(new Exception(expectedMessage)) : throw new Exception(expectedMessage);
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(listener.LocalEndPoint);

            Exception exception = await AssertThrowsQuicExceptionAsync(QuicError.CallbackError, async () => await listener.AcceptConnectionAsync());
            Assert.NotNull(exception.InnerException);
            Assert.Equal(expectedMessage, exception.InnerException.Message);
            await Assert.ThrowsAsync<AuthenticationException>(() => connectTask.AsTask());
        }

        [Fact]
        public async Task AcceptConnectionAsync_ThrowingCallbackOde_KeepRunning()
        {
            bool firstRun = true;

            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            // Throw an exception, which should throw the same from accept.
            listenerOptions.ConnectionOptionsCallback = (_, _, _) =>
            {
                if (firstRun)
                {
                    firstRun = false;
                    throw new ObjectDisposedException("failed");
                }

                return ValueTask.FromResult(CreateQuicServerOptions());
            };
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(listener.LocalEndPoint);

            Exception exception = await AssertThrowsQuicExceptionAsync(QuicError.CallbackError, async () => await listener.AcceptConnectionAsync().AsTask().WaitAsync(PassingTestTimeout));
            Assert.IsType<ObjectDisposedException>(exception.InnerException);
            await Assert.ThrowsAsync<AuthenticationException>(() => connectTask.AsTask().WaitAsync(PassingTestTimeout));

            // Throwing ODE in callback should keep Listener running
            connectTask = CreateQuicConnection(listener.LocalEndPoint);
            await using QuicConnection serverConnection = await listener.AcceptConnectionAsync().AsTask().WaitAsync(PassingTestTimeout);
            await using QuicConnection clientConnection = await connectTask.AsTask().WaitAsync(PassingTestTimeout);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [OuterLoop("Exercises several seconds long timeout.")]
        public async Task AcceptConnectionAsync_SlowOptionsCallback_TimesOut(bool useCancellationToken, bool clientShorterTimeout)
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            // Stall the options callback to force the timeout.
            listenerOptions.ConnectionOptionsCallback = async (connection, hello, cancellationToken) =>
            {
                if (useCancellationToken)
                {
                    var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.Delay(QuicDefaults.HandshakeTimeout + TimeSpan.FromSeconds(1), cancellationToken));
                    Assert.True(cancellationToken.IsCancellationRequested);
                    Assert.Equal(cancellationToken, oce.CancellationToken);
                    ExceptionDispatchInfo.Throw(oce);
                }
                await Task.Delay(QuicDefaults.HandshakeTimeout + TimeSpan.FromSeconds(1));
                return CreateQuicServerOptions();
            };
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.HandshakeTimeout = clientShorterTimeout ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(20);

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(clientOptions);

            if (clientShorterTimeout)
            {
                // Client gave up earlier than server and aborts the handshake, server should get UserCanceled
                // TLS alert
                var connectException = await Assert.ThrowsAsync<AuthenticationException>(async () => await listener.AcceptConnectionAsync());
                Assert.Contains(TlsAlertMessage.UserCanceled.ToString(), connectException.Message);

                var exception = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionTimeout, async () => await connectTask);
                Assert.Equal(SR.Format(SR.net_quic_handshake_timeout, clientOptions.HandshakeTimeout), exception.Message);
            }
            else
            {
                // handshake timed out on server side, expect ConnectionTimeout
                Exception exception = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionTimeout, async () => await listener.AcceptConnectionAsync());
                Assert.Equal(SR.Format(SR.net_quic_handshake_timeout, QuicDefaults.HandshakeTimeout), exception.Message);

                // Server aborts the handshake while client is still waiting, so UserCanceled alert is expected
                var connectException = await Assert.ThrowsAsync<AuthenticationException>(async () => await connectTask);
                Assert.Contains(TlsAlertMessage.UserCanceled.ToString(), connectException.Message);
            }
        }

        [Fact]
        [OuterLoop("Exercises several seconds long timeout.")]
        public async Task AcceptConnectionAsync_ClientCancels_FiresOptionCallbackCancellationToken()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ConnectionOptionsCallback = async (connection, hello, cancellationToken) =>
            {
                // default timeout for callback is 10s, wait for 5s for cancellation from client
                // terminating the connection mid-handshake
                var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                Assert.True(cancellationToken.IsCancellationRequested);
                Assert.Equal(cancellationToken, oce.CancellationToken);
                ExceptionDispatchInfo.Throw(oce);
                return null; // unreached;
            };
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.HandshakeTimeout = TimeSpan.FromSeconds(0.1);

            ValueTask<QuicConnection> connectTask = CreateQuicConnection(clientOptions);

            var connectException = await Assert.ThrowsAsync<AuthenticationException>(async () => await listener.AcceptConnectionAsync());
            Assert.Contains(TlsAlertMessage.UserCanceled.ToString(), connectException.Message);

            var exception = await AssertThrowsQuicExceptionAsync(QuicError.ConnectionTimeout, async () => await connectTask);
            Assert.Equal(SR.Format(SR.net_quic_handshake_timeout, clientOptions.HandshakeTimeout), exception.Message);
        }

        [Fact]
        public async Task AcceptConnectionAsync_ListenerDisposed_Throws()
        {
            var serverDisposed = new TaskCompletionSource();
            var connectAttempted = new TaskCompletionSource();

            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            // Stall the options callback to force the timeout.
            listenerOptions.ConnectionOptionsCallback = async (connection, hello, cancellationToken) =>
            {
                connectAttempted.SetResult();
                await serverDisposed.Task;
                Assert.True(cancellationToken.IsCancellationRequested);
                return null;
            };
            QuicListener listener = await CreateQuicListener(listenerOptions);

            // One accept that will have an incoming connection from the client.
            ValueTask<QuicConnection> acceptTask1 = listener.AcceptConnectionAsync();
            // Another accept without any incoming connection.
            ValueTask<QuicConnection> acceptTask2 = listener.AcceptConnectionAsync();

            // Attempt to connect the first accept.
            ValueTask<QuicConnection> connectTask = CreateQuicConnection(listener.LocalEndPoint);

            // First, wait for the connect attempt to reach the server; otherwise, the client exception might end up being HostUnreachable.
            // Then, dispose the listener and un-block the waiting server options callback.
            await connectAttempted.Task;
            await listener.DisposeAsync();
            serverDisposed.SetResult();

            var accept1Exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask1);
            var accept2Exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask2);

            // Connect attempt should be stopped with "UserCanceled".
            var connectException = await Assert.ThrowsAsync<AuthenticationException>(async () => await connectTask);
            Assert.Contains(TlsAlertMessage.UserCanceled.ToString(), connectException.Message);
        }

        [Fact]
        public async Task Listener_BacklogLimitRefusesConnection_ClientThrows()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ListenBacklog = 2;
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            // The third connection attempt fails with ConnectionRefused.
            await using var clientConnection1 = await CreateQuicConnection(listener.LocalEndPoint);
            await using var clientConnection2 = await CreateQuicConnection(listener.LocalEndPoint);
            await AssertThrowsQuicExceptionAsync(QuicError.ConnectionRefused, async () => await CreateQuicConnection(listener.LocalEndPoint));

            // Accept one connection and attempt another one.
            await using var serverConnection = await listener.AcceptConnectionAsync();
            await using var clientConnection3 = await CreateQuicConnection(listener.LocalEndPoint);
            // Third one again, should fail.
            await AssertThrowsQuicExceptionAsync(QuicError.ConnectionRefused, async () => await CreateQuicConnection(listener.LocalEndPoint));

            // Accept the remaining connection to see that failure do not affect them.
            await using var serverConnection2 = await listener.AcceptConnectionAsync();
            await using var serverConnection3 = await listener.AcceptConnectionAsync();
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(15, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 15)]
        public Task Listener_BacklogLimitRefusesConnection_ParallelClients_ClientThrows(int backlogLimit, int connectCount)
            => Listener_BacklogLimitRefusesConnection_ParallelClients_ClientThrows_Core(backlogLimit, connectCount);

        [Theory]
        [InlineData(100, 250)]
        [InlineData(250, 100)]
        [InlineData(100, 99)]
        [InlineData(100, 100)]
        [InlineData(100, 101)]
        [InlineData(15, 100)]
        [InlineData(10, 1_000)]
        [OuterLoop("Higher number of connections slow the test down.")]
        private Task Listener_BacklogLimitRefusesConnection_ParallelClients_ClientThrows_Slow(int backlogLimit, int connectCount)
            => Listener_BacklogLimitRefusesConnection_ParallelClients_ClientThrows_Core(backlogLimit, connectCount);

        private async Task Listener_BacklogLimitRefusesConnection_ParallelClients_ClientThrows_Core(int backlogLimit, int connectCount)
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ListenBacklog = backlogLimit;
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            // Kick off requested number of parallel connects.
            List<Task> connectTasks = new List<Task>();
            for (int i = 0; i < connectCount; ++i)
            {
                connectTasks.Add(CreateQuicConnection(listener.LocalEndPoint).AsTask());
            }

            // Count the number of successful connections and refused connections.
            int success = 0;
            int failure = 0;
            await Parallel.ForEachAsync(connectTasks, async (connectTask, cancellationToken) =>
            {
                try
                {
                    await connectTask;
                    Interlocked.Increment(ref success);
                }
                catch (QuicException qex) when (qex.QuicError == QuicError.ConnectionRefused)
                {
                    Interlocked.Increment(ref failure);
                }
            });

            // Check that the numbers correspond to backlog limit.
            int pendingConnections = 0;
            if (backlogLimit < connectCount)
            {
                pendingConnections = backlogLimit;
                Assert.Equal(backlogLimit, success);
                Assert.Equal(connectCount - backlogLimit, failure);
            }
            else
            {
                pendingConnections = connectCount;
                Assert.Equal(connectCount, success);
                Assert.Equal(0, failure);
            }

            // Accept all connections and check that the next accept pends.
            for (int i = 0; i < pendingConnections; ++i)
            {
                await using var connection = await listener.AcceptConnectionAsync();
            }

            // All pending connection should be accepted and the following call needs to be cancelled.
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            ValueTask<QuicConnection> acceptTask = listener.AcceptConnectionAsync(cts.Token);
            Assert.False(acceptTask.IsCompleted);
            cts.Cancel();
            var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await acceptTask);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [Fact]
        public async Task AcceptConnectionAsync_CancelThrows()
        {
            await using QuicListener listener = await CreateQuicListener();

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var acceptTask = listener.AcceptConnectionAsync(token);
            cts.Cancel();

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await acceptTask);
            Assert.Equal(token, exception.CancellationToken);
        }

        [Fact]
        public async Task AcceptConnectionAsync_ClientConnects_CancelIgnored()
        {
            await using QuicListener listener = await CreateQuicListener();

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var acceptTask = listener.AcceptConnectionAsync(token);
            await using var clientConnection = await CreateQuicConnection(listener.LocalEndPoint);

            await Task.Delay(TimeSpan.FromSeconds(0.5));

            // Cancellation should get ignored as the connection was connected.
            cts.Cancel();

            await using var serverConnection = await acceptTask;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/83012", TestPlatforms.OSX)]
        public async Task ListenOnAlreadyUsedPort_Throws_AddressInUse()
        {
            // bind a UDP socket to block a port
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Try to create a listener on the same port.
            SocketException ex = await Assert.ThrowsAsync<SocketException>(() => CreateQuicListener((IPEndPoint)s.LocalEndPoint).AsTask());
            Assert.Equal(SocketError.AddressAlreadyInUse, ((SocketException)ex).SocketErrorCode);
        }

        [Fact]
        public async Task TwoListenersOnSamePort_DisjointAlpn_Success()
        {
            await using QuicListener listener1 = await CreateQuicListener();

            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ListenEndPoint = listener1.LocalEndPoint;
            listenerOptions.ApplicationProtocols[0] = new SslApplicationProtocol("someprotocol");
            listenerOptions.ConnectionOptionsCallback = (_, _, _) =>
            {
                var options = CreateQuicServerOptions();
                options.ServerAuthenticationOptions.ApplicationProtocols[0] = listenerOptions.ApplicationProtocols[0];
                return ValueTask.FromResult(options);
            };
            await using QuicListener listener2 = await CreateQuicListener(listenerOptions);

            Assert.Equal(listener1.LocalEndPoint, listener2.LocalEndPoint);

            // Test making a connection to first listener
            ValueTask<QuicConnection> connectTask1 = CreateQuicConnection(listener1.LocalEndPoint);
            await using QuicConnection serverConnection1 = await listener1.AcceptConnectionAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
            await using QuicConnection clientConnection1 = await connectTask1.AsTask().WaitAsync(TimeSpan.FromSeconds(30));

            // Test making a connection to second listener
            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener1.LocalEndPoint);
            clientOptions.ClientAuthenticationOptions.ApplicationProtocols[0] = listenerOptions.ApplicationProtocols[0];
            ValueTask<QuicConnection> connectTask2 = CreateQuicConnection(clientOptions);
            await using QuicConnection serverConnection2 = await listener2.AcceptConnectionAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
            await using QuicConnection clientConnection2 = await connectTask2.AsTask().WaitAsync(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task TwoListenersOnSamePort_SameAlpn_Throws()
        {
            await using QuicListener listener = await CreateQuicListener();
            await AssertThrowsQuicExceptionAsync(QuicError.AlpnInUse, async () => await CreateQuicListener(listener.LocalEndPoint));
        }

        [Fact]
        public async Task Listener_AwaitsConnection_ListenerSurvivesGC()
        {
            TaskCompletionSource<IPEndPoint> listenerEndpointTcs = new TaskCompletionSource<IPEndPoint>();
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    await using var listener = await CreateQuicListener();
                    listenerEndpointTcs.SetResult(listener.LocalEndPoint);
                    var connection = await listener.AcceptConnectionAsync();
                    await connection.DisposeAsync();
                }).WaitAsync(TimeSpan.FromSeconds(5)),
                Task.Run(async () =>
                {
                    var endpoint = await listenerEndpointTcs.Task;
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                    GC.Collect();
                    var connection = await CreateQuicConnection(endpoint);
                    await connection.DisposeAsync();
                }).WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task Listener_AlpnNarrowingDown_Success()
        {
            using CancellationTokenSource testTimeoutCts = new CancellationTokenSource(PassingTestTimeout);
            CancellationToken timeoutToken = testTimeoutCts.Token;

            var listenerOptions = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>()
                {
                    new SslApplicationProtocol("foo"),
                    new SslApplicationProtocol("bar"),
                    new SslApplicationProtocol("test"),
                },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    // Narrowing down alpn list to more specific.
                    var options = CreateQuicServerOptions();
                    options.ServerAuthenticationOptions.ApplicationProtocols = new()
                    {
                        new SslApplicationProtocol("bar"),
                        new SslApplicationProtocol("test"),
                    };
                    return ValueTask.FromResult(options);
                }
            };
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            // Successful connection with bar ALPN
            QuicClientConnectionOptions clientOptions1 = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions1.ClientAuthenticationOptions.ApplicationProtocols = new()
            {
                new SslApplicationProtocol("foo"),
                new SslApplicationProtocol("bar"),
            };
            ValueTask<QuicConnection> connectTask1 = CreateQuicConnection(clientOptions1);
            await using QuicConnection serverConnection1 = await listener.AcceptConnectionAsync().AsTask().WaitAsync(timeoutToken);
            await using QuicConnection clientConnection1 = await connectTask1.AsTask().WaitAsync(timeoutToken);

            Assert.Equal(new SslApplicationProtocol("bar"), clientConnection1.NegotiatedApplicationProtocol);

            // Successful connection with test ALPN
            QuicClientConnectionOptions clientOptions2 = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions2.ClientAuthenticationOptions.ApplicationProtocols = new()
            {
                new SslApplicationProtocol("foo"),
                new SslApplicationProtocol("test"),
            };
            ValueTask<QuicConnection> connectTask2 = CreateQuicConnection(clientOptions2);
            await using QuicConnection serverConnection2 = await listener.AcceptConnectionAsync().AsTask().WaitAsync(timeoutToken);
            await using QuicConnection clientConnection2 = await connectTask2.AsTask().WaitAsync(timeoutToken);

            Assert.Equal(new SslApplicationProtocol("test"), clientConnection2.NegotiatedApplicationProtocol);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("not_existing")]
        public async Task Listener_AlpnNarrowingDown_Failure(string alpn)
        {
            using CancellationTokenSource testTimeoutCts = new CancellationTokenSource(PassingTestTimeout);
            CancellationToken timeoutToken = testTimeoutCts.Token;

            var listenerOptions = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>()
                {
                    new SslApplicationProtocol("foo"),
                    new SslApplicationProtocol("bar"),
                    new SslApplicationProtocol("test"),
                },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    // Narrowing down alpn list to more specific.
                    var options = CreateQuicServerOptions();
                    options.ServerAuthenticationOptions.ApplicationProtocols = new()
                    {
                        new SslApplicationProtocol("bar"),
                        new SslApplicationProtocol("test"),
                    };
                    return ValueTask.FromResult(options);
                }
            };
            bool isAlpnPresentOnInitialAlpnList = listenerOptions.ApplicationProtocols.Contains(new SslApplicationProtocol(alpn)); // If the ALPN is not present on initial list, AcceptConnectionAsync will not throw AuthenticationException.
            await using QuicListener listener = await CreateQuicListener(listenerOptions);
            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.ClientAuthenticationOptions.ApplicationProtocols = new()
            {
                new SslApplicationProtocol(alpn),
            };
            ValueTask<QuicConnection> connectTask = CreateQuicConnection(clientOptions);
            if (isAlpnPresentOnInitialAlpnList)
            {
                await Assert.ThrowsAsync<AuthenticationException>(() => listener.AcceptConnectionAsync().AsTask().WaitAsync(timeoutToken));
            }
            await Assert.ThrowsAsync<AuthenticationException>(() => connectTask.AsTask().WaitAsync(timeoutToken));
        }
    }
}
