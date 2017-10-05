// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class NetworkStreamTest
    {
        [Fact]
        public async Task ReadWrite_Span_Success()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var clientData = new byte[] { 42 };

                client.Write((ReadOnlySpan<byte>)clientData);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, server.Read((Span<byte>)serverData));

                Assert.Equal(clientData, serverData);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadWrite_Memory_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var clientData = new byte[] { 42 };

                await client.WriteAsync((ReadOnlyMemory<byte>)clientData);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, await server.ReadAsync((Memory<byte>)serverData));

                Assert.Equal(clientData, serverData);
            });
        }

        [Fact]
        public async Task ReadWrite_Precanceled_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.WriteAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.ReadAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)).AsTask());

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.WriteAsync((ReadOnlyMemory<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.ReadAsync((Memory<byte>)new byte[0], new CancellationToken(true)).AsTask());
            });
        }
    }
}
