// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
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
    }
}
