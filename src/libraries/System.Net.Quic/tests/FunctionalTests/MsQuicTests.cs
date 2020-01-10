// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class MsQuicTests : MsQuicTestBase
    {
        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact(Skip = "MsQuic not available")]
        public async Task BasicTest()
        {
            for (int i = 0; i < 100; i++)
            {
                Task listenTask = Task.Run(async () =>
                {
                    using QuicConnection connection = await DefaultListener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();

                    byte[] buffer = new byte[s_data.Length];
                    int bytesRead = await stream.ReadAsync(buffer);

                    Assert.Equal(s_data.Length, bytesRead);
                    Assert.True(s_data.Span.SequenceEqual(buffer));

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream.ShutdownWriteCompleted();

                    await connection.CloseAsync();
                });

                Task clientTask = Task.Run(async () =>
                {
                    using QuicConnection connection = CreateQuicConnection(DefaultListener.ListenEndPoint);
                    await connection.ConnectAsync();
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    await stream.WriteAsync(s_data, endStream: true);

                    byte[] memory = new byte[12];
                    int bytesRead = await stream.ReadAsync(memory);

                    Assert.Equal(s_data.Length, bytesRead);
                    // TODO this failed once...
                    Assert.True(s_data.Span.SequenceEqual(memory));
                    await stream.ShutdownWriteCompleted();

                    await connection.CloseAsync();
                });

                await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 10000);
            }
        }

        [Fact(Skip = "MsQuic not available")]
        public async Task MultipleReadsAndWrites()
        {
            for (int j = 0; j < 100; j++)
            {
                Task listenTask = Task.Run(async () =>
                {
                    // Connection isn't being accepted, interesting.
                    using QuicConnection connection = await DefaultListener.AcceptConnectionAsync();
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
                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);
                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync();
                });

                Task clientTask = Task.Run(async () =>
                {
                    using QuicConnection connection = CreateQuicConnection(DefaultListener.ListenEndPoint);
                    await connection.ConnectAsync();
                    await using QuicStream stream = connection.OpenBidirectionalStream();

                    for (int i = 0; i < 5; i++)
                    {
                        await stream.WriteAsync(s_data);
                    }

                    await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

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

                    await stream.ShutdownWriteCompleted();
                    await connection.CloseAsync();
                });

                await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 1000000);

            }
        }

        [Fact(Skip = "MsQuic not available")]
        public async Task MultipleStreamsOnSingleConnection()
        {
            Task listenTask = Task.Run(async () =>
            {
                {
                    using QuicConnection connection = await DefaultListener.AcceptConnectionAsync();
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    await using QuicStream stream2 = await connection.AcceptStreamAsync();

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

                    while (true)
                    {
                        int bytesRead = await stream2.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        Assert.True(s_data.Span.SequenceEqual(buffer));
                    }

                    await stream.WriteAsync(s_data, endStream: true);
                    await stream.ShutdownWriteCompleted();

                    await stream2.WriteAsync(s_data, endStream: true);
                    await stream2.ShutdownWriteCompleted();

                    await connection.CloseAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using QuicConnection connection = CreateQuicConnection(DefaultListener.ListenEndPoint);
                await connection.ConnectAsync();
                await using QuicStream stream = connection.OpenBidirectionalStream();
                await using QuicStream stream2 = connection.OpenBidirectionalStream();

                await stream.WriteAsync(s_data, endStream: true);
                await stream.ShutdownWriteCompleted();
                await stream2.WriteAsync(s_data, endStream: true);
                await stream2.ShutdownWriteCompleted();

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

                while (true)
                {
                    int res = await stream2.ReadAsync(memory);
                    if (res == 0)
                    {
                        break;
                    }
                    Assert.True(s_data.Span.SequenceEqual(memory));
                }

                await connection.CloseAsync();
            });

            await (new[] { listenTask, clientTask }).WhenAllOrAnyFailed(millisecondsTimeout: 60000);
        }

        [Fact(Skip = "MsQuic not available")]
        public async Task AbortiveConnectionFromClient()
        {
            using QuicConnection clientConnection = CreateQuicConnection(DefaultListener.ListenEndPoint);

            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await DefaultListener.AcceptConnectionAsync();
            await clientTask;
            // Close connection on client, verifying server connection is aborted.
            await clientConnection.CloseAsync();
            QuicStream stream = await serverConnection.AcceptStreamAsync();

            // Providers are alaways wrapped right now by a QuicStream. All fields are null here.
            // TODO make sure this returns null.
            Assert.Throws<NullReferenceException>(() => stream.CanRead);
        }

        [Fact(Skip = "MsQuic not available")]
        public async Task TestStreams()
        {
            using (QuicListener listener = new QuicListener(
                QuicImplementationProviders.MsQuic,
                new IPEndPoint(IPAddress.Loopback, 0),
                GetSslServerAuthenticationOptions()))
            {
                listener.Start();
                IPEndPoint listenEndPoint = listener.ListenEndPoint;

                using (QuicConnection clientConnection = new QuicConnection(
                    QuicImplementationProviders.MsQuic,
                    listenEndPoint,
                    sslClientAuthenticationOptions: new SslClientAuthenticationOptions { ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") } }))
                {
                    Assert.False(clientConnection.Connected);
                    Assert.Equal(listenEndPoint, clientConnection.RemoteEndPoint);

                    ValueTask connectTask = clientConnection.ConnectAsync();
                    QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                    await connectTask;

                    Assert.True(clientConnection.Connected);
                    Assert.True(serverConnection.Connected);
                    Assert.Equal(listenEndPoint, serverConnection.LocalEndPoint);
                    Assert.Equal(listenEndPoint, clientConnection.RemoteEndPoint);
                    Assert.Equal(clientConnection.LocalEndPoint, serverConnection.RemoteEndPoint);

                    await CreateAndTestBidirectionalStream(clientConnection, serverConnection);
                    await CreateAndTestBidirectionalStream(serverConnection, clientConnection);
                    await CreateAndTestUnidirectionalStream(serverConnection, clientConnection);
                    await CreateAndTestUnidirectionalStream(clientConnection, serverConnection);
                    await clientConnection.CloseAsync();
                }
            }
        }

        private static async Task CreateAndTestBidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            using (QuicStream s1 = c1.OpenBidirectionalStream())
            {
                Assert.True(s1.CanRead);
                Assert.True(s1.CanWrite);

                ValueTask writeTask = s1.WriteAsync(s_data);
                using (QuicStream s2 = await c2.AcceptStreamAsync())
                {
                    await ReceiveDataAsync(s_data, s2);
                    await writeTask;
                    await TestBidirectionalStream(s1, s2);
                }
            }
        }

        private static async Task CreateAndTestUnidirectionalStream(QuicConnection c1, QuicConnection c2)
        {
            using (QuicStream s1 = c1.OpenUnidirectionalStream())
            {
                Assert.False(s1.CanRead);
                Assert.True(s1.CanWrite);

                ValueTask writeTask = s1.WriteAsync(s_data);
                using (QuicStream s2 = await c2.AcceptStreamAsync())
                {
                    await ReceiveDataAsync(s_data, s2);
                    await writeTask;
                    await TestUnidirectionalStream(s1, s2);
                }
            }
        }

        private static async Task TestBidirectionalStream(QuicStream s1, QuicStream s2)
        {
            Assert.True(s1.CanRead);
            Assert.True(s1.CanWrite);
            Assert.True(s2.CanRead);
            Assert.True(s2.CanWrite);
            Assert.Equal(s1.StreamId, s2.StreamId);

            await SendAndReceiveDataAsync(s_data, s1, s2);
            await SendAndReceiveDataAsync(s_data, s2, s1);
            await SendAndReceiveDataAsync(s_data, s2, s1);
            await SendAndReceiveDataAsync(s_data, s1, s2);

            await SendAndReceiveEOFAsync(s1, s2);
            await SendAndReceiveEOFAsync(s2, s1);
        }

        private static async Task TestUnidirectionalStream(QuicStream s1, QuicStream s2)
        {
            Assert.False(s1.CanRead);
            Assert.True(s1.CanWrite);
            Assert.True(s2.CanRead);
            Assert.False(s2.CanWrite);
            Assert.Equal(s1.StreamId, s2.StreamId);

            await SendAndReceiveDataAsync(s_data, s1, s2);
            await SendAndReceiveDataAsync(s_data, s1, s2);

            await SendAndReceiveEOFAsync(s1, s2);
        }

        private static async Task SendAndReceiveDataAsync(ReadOnlyMemory<byte> data, QuicStream s1, QuicStream s2)
        {
            await s1.WriteAsync(data);
            await ReceiveDataAsync(data, s2);
        }

        private static async Task ReceiveDataAsync(ReadOnlyMemory<byte> data, QuicStream s)
        {
            Memory<byte> readBuffer = new byte[data.Length];

            int bytesRead = 0;
            while (bytesRead < data.Length)
            {
                bytesRead += await s.ReadAsync(readBuffer.Slice(bytesRead));
            }

            Assert.True(data.Span.SequenceEqual(readBuffer.Span));
        }

        private static async Task SendAndReceiveEOFAsync(QuicStream s1, QuicStream s2)
        {
            byte[] readBuffer = new byte[1];

            await s1.WriteAsync(Memory<byte>.Empty, endStream: true);
            await s1.ShutdownWriteCompleted();

            int bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);

            // Another read should still give EOF
            bytesRead = await s2.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);
        }
    }
}
