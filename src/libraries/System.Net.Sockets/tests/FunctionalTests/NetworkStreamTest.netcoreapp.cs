// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
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
    }
}
