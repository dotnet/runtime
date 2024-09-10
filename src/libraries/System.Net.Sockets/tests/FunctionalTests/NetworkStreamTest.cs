// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class NetworkStreamTest : ConnectedStreamConformanceTests
    {
        protected override bool BlocksOnZeroByteReads => true;
        protected override bool CanTimeout => true;
        protected override Type InvalidIAsyncResultExceptionType => typeof(IOException);
        protected override bool FlushRequiredToWriteData => false;
        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool ReadWriteValueTasksProtectSingleConsumption => true;
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint);
            Socket server = listener.Accept();

            return Task.FromResult<StreamPair>((new NetworkStream(client, ownsSocket: true), new NetworkStream(server, ownsSocket: true)));
        }

        [Fact]
        public void Ctor_NullSocket_ThrowsArgumentNullExceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, false));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, true));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, FileAccess.ReadWrite));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, FileAccess.ReadWrite, false));
        }

        [Fact]
        public void Ctor_NotConnected_Throws()
        {
            Assert.Throws<IOException>(() => new NetworkStream(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)));
        }

        [Fact]
        public async Task Ctor_NotStream_Throws()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port));
                Assert.Throws<IOException>(() => new NetworkStream(client));
            }
        }

        [Fact]
        public async Task Ctor_NonBlockingSocket_Throws()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                {
                    server.Blocking = false;
                    Assert.Throws<IOException>(() => new NetworkStream(server));
                }
            }
        }

        [Fact]
        public async Task Ctor_Socket_CanReadAndWrite_DoesntOwn()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                {
                    for (int i = 0; i < 2; i++) // Verify closing the streams doesn't close the sockets
                    {
                        using (var serverStream = new NetworkStream(server))
                        using (var clientStream = new NetworkStream(client))
                        {
                            Assert.True(serverStream.CanWrite && serverStream.CanRead);
                            Assert.True(clientStream.CanWrite && clientStream.CanRead);
                            Assert.False(serverStream.CanSeek && clientStream.CanSeek);
                            Assert.True(serverStream.CanTimeout && clientStream.CanTimeout);

                            // Verify Read and Write on both streams
                            byte[] buffer = new byte[1];

                            await serverStream.WriteAsync(new byte[] { (byte)'a' }, 0, 1);
                            Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal('a', (char)buffer[0]);

                            await clientStream.WriteAsync(new byte[] { (byte)'b' }, 0, 1);
                            Assert.Equal(1, await serverStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal('b', (char)buffer[0]);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData((FileAccess)42)] // unknown values treated as ReadWrite
        public async Task Ctor_SocketFileAccessBool_CanReadAndWrite_DoesntOwn(FileAccess access)
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                {
                    for (int i = 0; i < 2; i++) // Verify closing the streams doesn't close the sockets
                    {
                        using (var serverStream = new NetworkStream(server, access, false))
                        using (var clientStream = new NetworkStream(client, access, false))
                        {
                            Assert.True(serverStream.CanWrite && serverStream.CanRead);
                            Assert.True(clientStream.CanWrite && clientStream.CanRead);
                            Assert.False(serverStream.CanSeek && clientStream.CanSeek);
                            Assert.True(serverStream.CanTimeout && clientStream.CanTimeout);

                            // Verify Read and Write on both streams
                            byte[] buffer = new byte[1];

                            await serverStream.WriteAsync(new byte[] { (byte)'a' }, 0, 1);
                            Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal('a', (char)buffer[0]);

                            await clientStream.WriteAsync(new byte[] { (byte)'b' }, 0, 1);
                            Assert.Equal(1, await serverStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal('b', (char)buffer[0]);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Ctor_SocketBool_CanReadAndWrite(bool ownsSocket)
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                {
                    for (int i = 0; i < 2; i++) // Verify closing the streams doesn't close the sockets
                    {
                        Exception e = await Record.ExceptionAsync(async () =>
                        {
                            using (var serverStream = new NetworkStream(server, ownsSocket))
                            using (var clientStream = new NetworkStream(client, ownsSocket))
                            {
                                Assert.True(serverStream.CanWrite && serverStream.CanRead);
                                Assert.True(clientStream.CanWrite && clientStream.CanRead);
                                Assert.False(serverStream.CanSeek && clientStream.CanSeek);
                                Assert.True(serverStream.CanTimeout && clientStream.CanTimeout);

                                // Verify Read and Write on both streams
                                byte[] buffer = new byte[1];

                                await serverStream.WriteAsync(new byte[] { (byte)'a' }, 0, 1);
                                Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                                Assert.Equal('a', (char)buffer[0]);

                                await clientStream.WriteAsync(new byte[] { (byte)'b' }, 0, 1);
                                Assert.Equal(1, await serverStream.ReadAsync(buffer, 0, 1));
                                Assert.Equal('b', (char)buffer[0]);
                            }
                        });
                        if (i == 0)
                        {
                            Assert.Null(e);
                        }
                        else if (ownsSocket)
                        {
                            Assert.IsType<IOException>(e);
                        }
                        else
                        {
                            Assert.Null(e);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Ctor_SocketFileAccess_CanReadAndWrite()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                {
                    for (int i = 0; i < 2; i++) // Verify closing the streams doesn't close the sockets
                    {
                        using (var serverStream = new NetworkStream(server, FileAccess.Write))
                        using (var clientStream = new NetworkStream(client, FileAccess.Read))
                        {
                            Assert.True(serverStream.CanWrite && !serverStream.CanRead);
                            Assert.True(!clientStream.CanWrite && clientStream.CanRead);
                            Assert.False(serverStream.CanSeek && clientStream.CanSeek);
                            Assert.True(serverStream.CanTimeout && clientStream.CanTimeout);

                            // Verify Read and Write on both streams
                            byte[] buffer = new byte[1];

                            await serverStream.WriteAsync(new byte[] { (byte)'a' }, 0, 1);
                            Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal('a', (char)buffer[0]);

                            Assert.Throws<InvalidOperationException>(() => { serverStream.BeginRead(buffer, 0, 1, null, null); });
                            Assert.Throws<InvalidOperationException>(() => { clientStream.BeginWrite(buffer, 0, 1, null, null); });

                            Assert.Throws<InvalidOperationException>(() => { serverStream.ReadAsync(buffer, 0, 1); });
                            Assert.Throws<InvalidOperationException>(() => { clientStream.WriteAsync(buffer, 0, 1); });
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task SocketProperty_SameAsProvidedSocket()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                using (DerivedNetworkStream serverStream = new DerivedNetworkStream(server))
                {
                    Assert.Same(server, serverStream.Socket);
                }
            }
        }

        [OuterLoop("Spins waiting for DataAvailable")]
        [Fact]
        public async Task DataAvailable_ReturnsFalseOrTrueAppropriately()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                Assert.False(server.DataAvailable && client.DataAvailable);

                await server.WriteAsync(new byte[1], 0, 1);
                Assert.False(server.DataAvailable);
                Assert.True(SpinWait.SpinUntil(() => client.DataAvailable, 10000), "DataAvailable did not return true in the allotted time");
            });
        }

        [Theory]	
        [InlineData(false)]	
        [InlineData(true)]	
        public async Task DisposedClosed_MembersThrowObjectDisposedException(bool close)	
        {	
            await RunWithConnectedNetworkStreamsAsync((server, _) =>	
            {
                if (close) server.Close();
                else server.Dispose();	

                // Unique members to NetworkStream; others covered by stream conformance tests
                Assert.Throws<ObjectDisposedException>(() => server.DataAvailable);

                return Task.CompletedTask;	
            });	
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DisposeSocketDirectly_ReadWriteThrowNetworkException(bool derivedNetworkStream)
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using Socket serverSocket = await acceptTask;

                using NetworkStream server = derivedNetworkStream ? (NetworkStream)new DerivedNetworkStream(serverSocket) : new NetworkStream(serverSocket);

                serverSocket.Dispose();

                ExpectIOException(() => server.Read(new byte[1], 0, 1));
                ExpectIOException(() => server.Write(new byte[1], 0, 1));

                ExpectIOException(() => server.Read((Span<byte>)new byte[1]));
                ExpectIOException(() => server.Write((ReadOnlySpan<byte>)new byte[1]));

                ExpectIOException(() => server.BeginRead(new byte[1], 0, 1, null, null));
                ExpectIOException(() => server.BeginWrite(new byte[1], 0, 1, null, null));

                ExpectIOException(() => { _ = server.ReadAsync(new byte[1], 0, 1); });
                ExpectIOException(() => { _ = server.WriteAsync(new byte[1], 0, 1); });
            }

            static void ExpectIOException(Action action)
            {
                IOException ex = Assert.Throws<IOException>(action);
                Assert.IsType<ObjectDisposedException>(ex.InnerException);
            }
        }

        [Fact]
        public async Task Close_InvalidArgument_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((server, _) =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Close(-2));
                server.Close(-1);
                server.Close(0);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadableWriteableProperties_Roundtrip()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket server = await acceptTask)
                using (DerivedNetworkStream serverStream = new DerivedNetworkStream(server))
                {
                    Assert.True(serverStream.Readable && serverStream.Writeable);

                    serverStream.Readable = false;
                    Assert.False(serverStream.Readable);
                    Assert.False(serverStream.CanRead);
                    Assert.Throws<InvalidOperationException>(() => serverStream.Read(new byte[1], 0, 1));

                    serverStream.Readable = true;
                    Assert.True(serverStream.Readable);
                    Assert.True(serverStream.CanRead);
                    await client.SendAsync(new ArraySegment<byte>(new byte[1], 0, 1), SocketFlags.None);
                    Assert.Equal(1, await serverStream.ReadAsync(new byte[1], 0, 1));

                    serverStream.Writeable = false;
                    Assert.False(serverStream.Writeable);
                    Assert.False(serverStream.CanWrite);
                    Assert.Throws<InvalidOperationException>(() => serverStream.Write(new byte[1], 0, 1));

                    serverStream.Writeable = true;
                    Assert.True(serverStream.Writeable);
                    Assert.True(serverStream.CanWrite);
                    await serverStream.WriteAsync(new byte[1], 0, 1);
                    Assert.Equal(1, await client.ReceiveAsync(new ArraySegment<byte>(new byte[1], 0, 1), SocketFlags.None));
                }
            }
        }

        [Fact]
        public async Task CopyToAsync_DisposedSourceStream_ThrowsOnWindows_NoThrowOnUnix()
        {
            await RunWithConnectedNetworkStreamsAsync(async (stream, _) =>
            {
                // Copying while disposing the stream
                Task copyTask = stream.CopyToAsync(new MemoryStream());
                stream.Dispose();
                Exception e = await Record.ExceptionAsync(() => copyTask);

                // Difference in shutdown/close behavior between Windows and Unix.
                // On Windows, the outstanding receive is completed as aborted when the
                // socket is closed.  On Unix, it's completed as successful once or after
                // the shutdown is issued, but depending on timing, if it's then closed
                // before that takes effect, it may also complete as aborted.
                bool isWindows = OperatingSystem.IsWindows();
                Assert.True(
                    (isWindows && e is IOException) ||
                    (!isWindows && (e == null || e is IOException)),
                    $"Got unexpected exception: {e?.ToString() ?? "(null)"}");

                // Copying after disposing the stream
                Assert.Throws<ObjectDisposedException>(() => { stream.CopyToAsync(new MemoryStream()); });
            });
        }

        [Fact]
        public async Task CopyToAsync_NonReadableSourceStream_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((stream, _) =>
            {
                // Copying from non-readable stream
                Assert.Throws<NotSupportedException>(() => { stream.CopyToAsync(new MemoryStream()); });
                return Task.CompletedTask;
            }, serverAccess:FileAccess.Write);
        }

        [Fact]
        public async Task ReadAsync_MultipleConcurrentValueTaskReads_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                ValueTask<int> r1 = server.ReadAsync(b1);
                ValueTask<int> r2 = server.ReadAsync(b2);
                ValueTask<int> r3 = server.ReadAsync(b3);

                await client.WriteAsync(new byte[] { 42, 43, 44 });

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task ReadAsync_MultipleConcurrentValueTaskReads_AsTask_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Task<int> r1 = server.ReadAsync((Memory<byte>)b1).AsTask();
                Task<int> r2 = server.ReadAsync((Memory<byte>)b2).AsTask();
                Task<int> r3 = server.ReadAsync((Memory<byte>)b3).AsTask();

                await client.WriteAsync(new byte[] { 42, 43, 44 });

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task WriteAsync_MultipleConcurrentValueTaskWrites_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                ValueTask s1 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 42 }));
                ValueTask s2 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 43 }));
                ValueTask s3 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 44 }));

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Assert.Equal(3,
                    await client.ReadAsync((Memory<byte>)b1) +
                    await client.ReadAsync((Memory<byte>)b2) +
                    await client.ReadAsync((Memory<byte>)b3));

                await s1;
                await s2;
                await s3;

                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        [Fact]
        public async Task WriteAsync_MultipleConcurrentValueTaskWrites_AsTask_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                // Technically this isn't supported behavior, but it happens to work because it's supported on socket.
                // So validate it to alert us to any potential future breaks.

                Task s1 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 42 })).AsTask();
                Task s2 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 43 })).AsTask();
                Task s3 = server.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 44 })).AsTask();

                byte[] b1 = new byte[1], b2 = new byte[1], b3 = new byte[1];
                Task<int> r1 = client.ReadAsync((Memory<byte>)b1).AsTask();
                Task<int> r2 = client.ReadAsync((Memory<byte>)b2).AsTask();
                Task<int> r3 = client.ReadAsync((Memory<byte>)b3).AsTask();

                await Task.WhenAll(s1, s2, s3, r1, r2, r3);

                Assert.Equal(3, await r1 + await r2 + await r3);
                Assert.Equal(42 + 43 + 44, b1[0] + b2[0] + b3[0]);
            });
        }

        /// <summary>
        /// Creates a pair of connected NetworkStreams and invokes the provided <paramref name="func"/>
        /// with them as arguments.
        /// </summary>
        private static async Task RunWithConnectedNetworkStreamsAsync(Func<NetworkStream, NetworkStream, Task> func,
            FileAccess serverAccess = FileAccess.ReadWrite, FileAccess clientAccess = FileAccess.ReadWrite)
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
                    using (NetworkStream serverStream = new NetworkStream(remote.Client, serverAccess, ownsSocket: true))
                    using (NetworkStream clientStream = new NetworkStream(client.Client, clientAccess, ownsSocket: true))
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

        [Fact]
        public async Task NetworkStream_ReadTimeout_RemainUseable()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            NetworkStream readable = (NetworkStream)streams.Stream1;

            Assert.True(readable.Socket.Connected);
            readable.Socket.ReceiveTimeout = TestSettings.FailingTestTimeout;
            var buffer = new byte[100];
            int readBytes;
            try
            {
                readBytes = readable.Read(buffer);
            }
            catch (IOException ex) when (ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.TimedOut)
            {
            }
            Assert.True(readable.Socket.Connected);

            try
            {
                readBytes = readable.Read(buffer);
            }
            catch (IOException ex) when (ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.TimedOut)
            {
            }
            Assert.True(readable.Socket.Connected);

            streams.Stream2.Write(new byte[] { 65 });
            readBytes = readable.Read(buffer);
            Assert.Equal(1, readBytes);
            Assert.True(readable.Socket.Connected);
        }


        [Fact]
        public async Task NetworkStream_ReadAsyncTimeout_RemainUseable()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            NetworkStream readable = (NetworkStream)streams.Stream1;

            Assert.True(readable.Socket.Connected);

            CancellationTokenSource cts = new CancellationTokenSource(TestSettings.FailingTestTimeout);
            var buffer = new byte[100];
            int readBytes;
            try
            {
                readBytes = await readable.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            Assert.True(readable.Socket.Connected);

            try
            {
                cts = new CancellationTokenSource(TestSettings.FailingTestTimeout);
                readBytes = await readable.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            Assert.True(readable.Socket.Connected);

            await streams.Stream2.WriteAsync(new byte[] { 65 });
            readBytes = await readable.ReadAsync(buffer);
            Assert.Equal(1, readBytes);
            Assert.True(readable.Socket.Connected);
        }

        private sealed class DerivedNetworkStream : NetworkStream
        {
            public DerivedNetworkStream(Socket socket) : base(socket) { }

            public new bool Readable
            {
                get { return base.Readable; }
                set { base.Readable = value; }
            }

            public new bool Writeable
            {
                get { return base.Writeable; }
                set { base.Writeable = value; }
            }
        }
    }
}
