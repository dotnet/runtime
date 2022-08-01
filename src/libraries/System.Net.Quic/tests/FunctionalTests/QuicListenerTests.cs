// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Security;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported))]
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

        [Fact]
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
            Exception exception = await Assert.ThrowsAsync<Exception>(async () => await listener.AcceptConnectionAsync());
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task ListenOnAlreadyUsedPort_Throws_AddressInUse()
        {
            // bind a UDP socket to block a port
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Try to create a listener on the same port.
            await AssertThrowsQuicExceptionAsync(QuicError.AddressInUse, async () => await CreateQuicListener((IPEndPoint)s.LocalEndPoint));
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

            //
            // TODO: MsQuic returns QUIC_STATUS_INVALID_STATE in this case, returning
            // ADDRESS_IN_USE could be confusing because you can actually bind two listeners
            // to the same port (see TwoListenersOnSamePort_DisjointAlpn_Success). It may be better
            // to add a new error code to identify this case
            //
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/73045")]
            //
            await AssertThrowsQuicExceptionAsync(QuicError.InternalError, async () => await CreateQuicListener(listener.LocalEndPoint));
        }
    }
}
