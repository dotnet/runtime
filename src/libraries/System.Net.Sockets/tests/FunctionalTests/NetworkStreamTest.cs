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
        public void Ctor_NullSocket_ThrowsArgumentNullExceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, false));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, true));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, FileAccess.ReadWrite));
            AssertExtensions.Throws<ArgumentNullException>("socket", () => new NetworkStream(null, FileAccess.ReadWrite, false));
        }

        [Fact]
        public void Ctor_NotConnected_ThrowsIOException()
        {
            Assert.Throws<IOException>(() => new NetworkStream(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)));
        }

        [Fact]
        public async Task Ctor_NotStream_ThrowsIOException()
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
        public async Task Ctor_NonBlockingSocket_ThrowsIOException()
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

                Assert.Throws<ObjectDisposedException>(() => server.DataAvailable);

                Assert.Throws<ObjectDisposedException>(() => server.Read(new byte[1], 0, 1));
                Assert.Throws<ObjectDisposedException>(() => server.Write(new byte[1], 0, 1));

                Assert.Throws<ObjectDisposedException>(() => server.BeginRead(new byte[1], 0, 1, null, null));
                Assert.Throws<ObjectDisposedException>(() => server.BeginWrite(new byte[1], 0, 1, null, null));

                Assert.Throws<ObjectDisposedException>(() => server.EndRead(null));
                Assert.Throws<ObjectDisposedException>(() => server.EndWrite(null));

                Assert.Throws<ObjectDisposedException>(() => { server.ReadAsync(new byte[1], 0, 1); });
                Assert.Throws<ObjectDisposedException>(() => { server.WriteAsync(new byte[1], 0, 1); });

                Assert.Throws<ObjectDisposedException>(() => { server.CopyToAsync(new MemoryStream()); });

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task DisposeSocketDirectly_ReadWriteThrowIOException()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.WhenAll(acceptTask, client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port)));
                using (Socket serverSocket = await acceptTask)
                using (DerivedNetworkStream server = new DerivedNetworkStream(serverSocket))
                {
                    serverSocket.Dispose();

                    Assert.Throws<IOException>(() => server.Read(new byte[1], 0, 1));
                    Assert.Throws<IOException>(() => server.Write(new byte[1], 0, 1));

                    Assert.Throws<IOException>(() => server.BeginRead(new byte[1], 0, 1, null, null));
                    Assert.Throws<IOException>(() => server.BeginWrite(new byte[1], 0, 1, null, null));

                    Assert.Throws<IOException>(() => { server.ReadAsync(new byte[1], 0, 1); });
                    Assert.Throws<IOException>(() => { server.WriteAsync(new byte[1], 0, 1); });
                }
            }
        }

        [Fact]
        public async Task InvalidIAsyncResult_EndReadWriteThrows()
        {
            await RunWithConnectedNetworkStreamsAsync((server, _) =>
            {
                Assert.Throws<IOException>(() => server.EndRead(Task.CompletedTask));
                Assert.Throws<IOException>(() => server.EndWrite(Task.CompletedTask));
                return Task.CompletedTask;
            });
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
        public async Task ReadWrite_InvalidArguments_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((server, _) =>
            {
                Assert.Throws<ArgumentNullException>(() => server.Read(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[1], 2, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[1], 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Read(new byte[1], 0, 2));

                Assert.Throws<ArgumentNullException>(() => server.BeginRead(null, 0, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginRead(new byte[1], -1, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginRead(new byte[1], 2, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginRead(new byte[1], 0, -1, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginRead(new byte[1], 0, 2, null, null));

                Assert.Throws<ArgumentNullException>(() => { server.ReadAsync(null, 0, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.ReadAsync(new byte[1], -1, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.ReadAsync(new byte[1], 2, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.ReadAsync(new byte[1], 0, -1); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.ReadAsync(new byte[1], 0, 2); });

                Assert.Throws<ArgumentNullException>(() => server.Write(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[1], 2, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[1], 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Write(new byte[1], 0, 2));

                Assert.Throws<ArgumentNullException>(() => server.BeginWrite(null, 0, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginWrite(new byte[1], -1, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginWrite(new byte[1], 2, 0, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginWrite(new byte[1], 0, -1, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => server.BeginWrite(new byte[1], 0, 2, null, null));

                Assert.Throws<ArgumentNullException>(() => { server.WriteAsync(null, 0, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.WriteAsync(new byte[1], -1, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.WriteAsync(new byte[1], 2, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.WriteAsync(new byte[1], 0, -1); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { server.WriteAsync(new byte[1], 0, 2); });

                Assert.Throws<ArgumentNullException>(() => server.EndRead(null));
                Assert.Throws<ArgumentNullException>(() => server.EndWrite(null));

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task NotSeekable_OperationsThrowExceptions()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                Assert.False(server.CanSeek && client.CanSeek);
                Assert.Throws<NotSupportedException>(() => server.Seek(0, SeekOrigin.Begin));
                Assert.Throws<NotSupportedException>(() => server.Length);
                Assert.Throws<NotSupportedException>(() => server.SetLength(1024));
                Assert.Throws<NotSupportedException>(() => server.Position);
                Assert.Throws<NotSupportedException>(() => server.Position = 0);
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
        public async Task ReadWrite_Array_Success()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var clientData = new byte[] { 42 };
                client.Write(clientData, 0, clientData.Length);

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, server.Read(serverData, 0, serverData.Length));

                Assert.Equal(clientData, serverData);

                client.Flush(); // nop

                return Task.CompletedTask;
            });
        }

        [OuterLoop] // TODO: Issue #11345
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

                Assert.Equal(TaskStatus.RanToCompletion, client.FlushAsync().Status); // nop
            });
        }

        [Fact]
        public async Task BeginEndReadWrite_Sync_Success()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var clientData = new byte[] { 42 };

                client.EndWrite(client.BeginWrite(clientData, 0, clientData.Length, null, null));

                var serverData = new byte[clientData.Length];
                Assert.Equal(serverData.Length, server.EndRead(server.BeginRead(serverData, 0, serverData.Length, null, null)));

                Assert.Equal(clientData, serverData);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task BeginEndReadWrite_Async_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var clientData = new byte[] { 42 };
                var serverData = new byte[clientData.Length];
                var tcs = new TaskCompletionSource<bool>();

                client.BeginWrite(clientData, 0, clientData.Length, writeIar =>
                {
                    try
                    {
                        client.EndWrite(writeIar);
                        server.BeginRead(serverData, 0, serverData.Length, readIar =>
                        {
                            try
                            {
                                Assert.Equal(serverData.Length, server.EndRead(readIar));
                                tcs.SetResult(true);
                            }
                            catch (Exception e2) { tcs.SetException(e2); }
                        }, null);
                    }
                    catch (Exception e1) { tcs.SetException(e1); }
                }, null);

                await tcs.Task;
                Assert.Equal(clientData, serverData);
            });
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop("Timeouts")]
        [Fact]
        public async Task ReadTimeout_Expires_ThrowsSocketException()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                Assert.Equal(-1, server.ReadTimeout);

                server.ReadTimeout = 1;
                Assert.ThrowsAny<IOException>(() => server.Read(new byte[1], 0, 1));

                return Task.CompletedTask;
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task Timeout_InvalidData_ThrowsArgumentException(int invalidTimeout)
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => server.ReadTimeout = invalidTimeout);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => server.WriteTimeout = invalidTimeout);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task Timeout_ValidData_Roundtrips()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                Assert.Equal(-1, server.ReadTimeout);
                Assert.Equal(-1, server.WriteTimeout);

                server.ReadTimeout = 100;
                Assert.InRange(server.ReadTimeout, 100, int.MaxValue);
                server.ReadTimeout = 100; // same value again
                Assert.InRange(server.ReadTimeout, 100, int.MaxValue);

                server.ReadTimeout = -1;
                Assert.Equal(-1, server.ReadTimeout);

                server.WriteTimeout = 100;
                Assert.InRange(server.WriteTimeout, 100, int.MaxValue);
                server.WriteTimeout = 100; // same value again
                Assert.InRange(server.WriteTimeout, 100, int.MaxValue);

                server.WriteTimeout = -1;
                Assert.Equal(-1, server.WriteTimeout);

                return Task.CompletedTask;
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(4095)]
        [InlineData(1024*1024)]
        public async Task CopyToAsync_AllDataCopied(int byteCount)
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var results = new MemoryStream();
                byte[] dataToCopy = new byte[byteCount];
                new Random().NextBytes(dataToCopy);

                Task copyTask = client.CopyToAsync(results);
                await server.WriteAsync(dataToCopy, 0, dataToCopy.Length);
                server.Dispose();
                await copyTask;

                Assert.Equal(dataToCopy, results.ToArray());
            });
        }

        [Fact]
        public async Task CopyToAsync_InvalidArguments_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((stream, _) =>
            {
                // Null destination
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { stream.CopyToAsync(null); });

                // Buffer size out-of-range
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { stream.CopyToAsync(new MemoryStream(), 0); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { stream.CopyToAsync(new MemoryStream(), -1, CancellationToken.None); });

                // Copying to non-writable stream
                Assert.Throws<NotSupportedException>(() => { stream.CopyToAsync(new MemoryStream(new byte[0], writable: false)); });

                // Copying to a disposed stream
                Assert.Throws<ObjectDisposedException>(() =>
                {
                    var disposedTarget = new MemoryStream();
                    disposedTarget.Dispose();
                    stream.CopyToAsync(disposedTarget);
                });

                // Already canceled
                Assert.Equal(TaskStatus.Canceled, stream.CopyToAsync(new MemoryStream(new byte[1]), 1, new CancellationToken(canceled: true)).Status);

                return Task.CompletedTask;
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Optimized .NET Core CopyToAsync doesn't use Begin/EndRead, skipping code that throws ObjectDisposedException on netfx")]
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
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows); 
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
                    using (NetworkStream serverStream = new NetworkStream(remote.Client, serverAccess, ownsSocket:true))
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

        private sealed class DerivedNetworkStream : NetworkStream
        {
            public DerivedNetworkStream(Socket socket) : base(socket) { }

            public new Socket Socket => base.Socket;

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
