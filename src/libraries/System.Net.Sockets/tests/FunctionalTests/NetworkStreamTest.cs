// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class NetworkStreamTest
    {
        [Theory]
        [MemberData(nameof(NonCanceledTokens))]
        public async Task ReadWriteAsync_NonCanceled_Success(CancellationToken nonCanceledToken)
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var clientData = new byte[] { 42 };
                await client.WriteAsync(clientData, 0, clientData.Length, nonCanceledToken);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, await server.ReadAsync(serverData, 0, serverData.Length, nonCanceledToken));

                Assert.Equal(clientData, serverData);
            });
        }

        [Fact]
        public async Task ReadWriteAsync_Canceled_ThrowsOperationCanceledException()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var canceledToken = new CancellationToken(canceled: true);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.WriteAsync(new byte[1], 0, 1, canceledToken));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.ReadAsync(new byte[1], 0, 1, canceledToken));
            });
        }

        public static object[][] NonCanceledTokens = new object[][]
        {
            new object[] { CancellationToken.None },             // CanBeCanceled == false
            new object[] { new CancellationTokenSource().Token } // CanBeCanceled == true
        };

        /// <summary>
        /// Creates a pair of connected NetworkStreams and invokes the provided <paramref name="func"/>
        /// with them as arguments.
        /// </summary>
        private static async Task RunWithConnectedNetworkStreamsAsync(Func<NetworkStream, NetworkStream, Task> func)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start(1);
                var clientEndpoint = (IPEndPoint)listener.LocalEndpoint;

                using (var client = new TcpClient(clientEndpoint.AddressFamily))
                {
                    Task<TcpClient> remoteTask = listener.AcceptTcpClientAsync();
                    Task clientConnectTask = client.ConnectAsync(clientEndpoint.Address, clientEndpoint.Port);

                    await Task.WhenAll(remoteTask, clientConnectTask);

                    using (TcpClient remote = remoteTask.Result)
                    using (NetworkStream serverStream = remote.GetStream())
                    using (NetworkStream clientStream = client.GetStream())
                    {
                        await func(serverStream, clientStream);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

    }
}
