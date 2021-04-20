// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public abstract class QuicListenerTests<T> : QuicTestBase<T>
        where T : IQuicImplProviderFactory, new()
    {
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
    }

    public sealed class QuicListenerTests_MockProvider : QuicListenerTests<MockProviderFactory> { }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class QuicListenerTests_MsQuicProvider : QuicListenerTests<MsQuicProviderFactory> { }
}
