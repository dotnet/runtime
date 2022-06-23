// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public abstract class QuicListenerTests<T> : QuicTestBase<T>
        where T : IQuicImplProviderFactory, new()
    {
        public QuicListenerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Listener_Backlog_Success()
        {
            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener();

                using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                var clientStreamTask = clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }

        [Fact]
        public async Task Listener_Backlog_Success_IPv6()
        {
            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener(new IPEndPoint(IPAddress.IPv6Loopback, 0));

                using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                var clientStreamTask = clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }

        [ConditionalFact(nameof(IsMsQuicProvider))]
        public async Task Listener_IPv6Any_Accepts_IPv4()
        {
            await Task.Run(async () =>
            {
                using QuicListener listener = CreateQuicListener(new IPEndPoint(IPAddress.IPv6Any, 0));

                using QuicConnection clientConnection = CreateQuicConnection(new IPEndPoint(IPAddress.Loopback, listener.ListenEndPoint.Port));
                var clientStreamTask = clientConnection.ConnectAsync();

                using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                await clientStreamTask;
            }).WaitAsync(TimeSpan.FromSeconds(6));
        }
    }

    [ConditionalClass(typeof(QuicTestBase<MockProviderFactory>), nameof(QuicTestBase<MockProviderFactory>.IsSupported))]
    public sealed class QuicListenerTests_MockProvider : QuicListenerTests<MockProviderFactory>
    {
        public QuicListenerTests_MockProvider(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    [Collection(nameof(DisableParallelization))]
    public sealed class QuicListenerTests_MsQuicProvider : QuicListenerTests<MsQuicProviderFactory>
    {
        public QuicListenerTests_MsQuicProvider(ITestOutputHelper output) : base(output) { }
    }
}
