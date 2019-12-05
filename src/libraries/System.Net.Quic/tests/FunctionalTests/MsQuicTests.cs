// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class MsQuicTests : MsQuicTestBase
    {
        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task BasicTest()
        {
            await DefaultListener.StartAsync();

            Task listenTask = Task.Run(async () =>
            {
                await using QuicConnection connection = await DefaultListener.AcceptConnectionAsync();
                await using QuicStream stream = await connection.AcceptStreamAsync();

                byte[] buffer = new byte[s_data.Length];
                int bytesRead = await stream.ReadAsync(buffer);

                Assert.Equal(s_data.Length, bytesRead);
                Assert.True(s_data.Span.SequenceEqual(buffer));

                await stream.WriteAsync(s_data);
                stream.ShutdownWrite();
            });

            Task clientTask = Task.Run(async () =>
            {
                await using QuicConnection connection = CreateQuicConnection(DefaultEndpoint);
                await connection.ConnectAsync();
                await using QuicStream stream = connection.OpenBidirectionalStream();

                await stream.WriteAsync(s_data);
                byte[] memory = new byte[12];
                // TODO hit a stress bug where this read was aborted.
                int res = await stream.ReadAsync(memory);

                Assert.True(s_data.Span.SequenceEqual(memory));
            });

            await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 10000);
        }

        [Fact]
        public async Task MultipleReadsAndWrites()
        {
            await DefaultListener.StartAsync();

            Task listenTask = Task.Run(async () =>
            {
                await using QuicConnection connection = await DefaultListener.AcceptConnectionAsync();
                await using QuicStream stream = await connection.AcceptStreamAsync();
                byte[] buffer = new byte[s_data.Length];

                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.True(s_data.Span.SequenceEqual(buffer));
                }

                for (int i = 0; i < 5; i++)
                {
                    await stream.WriteAsync(s_data);
                }

                stream.ShutdownWrite();
            });

            Task clientTask = Task.Run(async () =>
            {
                await using QuicConnection connection = CreateQuicConnection(DefaultEndpoint);
                await connection.ConnectAsync();
                await using QuicStream stream = connection.OpenBidirectionalStream();

                for (int i = 0; i < 5; i++)
                {
                    await stream.WriteAsync(s_data);
                }

                stream.ShutdownWrite();

                byte[] memory = new byte[12];
                while (true)
                {
                    int res = await stream.ReadAsync(memory);
                    if (res == 0)
                    {
                        break;
                    }
                    Assert.True(s_data.Span.SequenceEqual(memory));
                }

            });

            await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 1000000);

        }
    }
}
