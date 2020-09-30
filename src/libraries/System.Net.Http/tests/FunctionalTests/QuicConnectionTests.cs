// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public class QuicConnectionTests : MsQuicTestBase
    {
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
}
