// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class NetworkStreamTest
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
        public void Ctor_NotConnected_ThrowsNetworkException()
        {
            Assert.Throws<NetworkException>(() => new NetworkStream(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)));
        }

        [Fact]
        public async Task Ctor_NotStream_ThrowsNetworkException()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndPoint).Port));
                Assert.Throws<NetworkException>(() => new NetworkStream(client));
            }
        }

        [Fact]
        public async Task Ctor_NonBlockingSocket_ThrowsNetworkException()
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
                    Assert.Throws<NetworkException>(() => new NetworkStream(server));
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
                            Assert.IsType<NetworkException>(e);
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
        public async Task DisposeSocketDirectly_ReadWriteThrowNetworkException()
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

                    Assert.Throws<NetworkException>(() => server.Read(new byte[1], 0, 1));
                    Assert.Throws<NetworkException>(() => server.Write(new byte[1], 0, 1));

                    Assert.Throws<NetworkException>(() => server.BeginRead(new byte[1], 0, 1, null, null));
                    Assert.Throws<NetworkException>(() => server.BeginWrite(new byte[1], 0, 1, null, null));

                    Assert.Throws<NetworkException>(() => { server.ReadAsync(new byte[1], 0, 1); });
                    Assert.Throws<NetworkException>(() => { server.WriteAsync(new byte[1], 0, 1); });
                }
            }
        }

        [Fact]
        public async Task InvalidIAsyncResult_EndReadWriteThrows()
        {
            await RunWithConnectedNetworkStreamsAsync((server, _) =>
            {
                Assert.Throws<NetworkException>(() => server.EndRead(Task.CompletedTask));
                Assert.Throws<NetworkException>(() => server.EndWrite(Task.CompletedTask));
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
        public async Task ReadWrite_Byte_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                for (byte i = 0; i < 10; i++)
                {
                    Task<int> read = Task.Run(() => client.ReadByte());
                    Task write = Task.Run(() => server.WriteByte(i));
                    await Task.WhenAll(read, write);
                    Assert.Equal(i, await read);
                }
            });
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

        [OuterLoop]
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
                var tcs = new TaskCompletionSource();

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
                                tcs.SetResult();
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

        [OuterLoop]
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
                Assert.ThrowsAny<NetworkException>(() => server.Read(new byte[1], 0, 1));

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

        public static IEnumerable<object[]> CopyToAsync_AllDataCopied_MemberData() =>
            from asyncWrite in new bool[] { true, false }
            from byteCount in new int[] { 0, 1, 1024, 4096, 4095, 1024 * 1024 }
            select new object[] { byteCount, asyncWrite };

        [Theory]
        [MemberData(nameof(CopyToAsync_AllDataCopied_MemberData))]
        public async Task CopyToAsync_AllDataCopied(int byteCount, bool asyncWrite)
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var results = new MemoryStream();
                byte[] dataToCopy = new byte[byteCount];
                new Random().NextBytes(dataToCopy);

                Task copyTask = client.CopyToAsync(results);

                if (asyncWrite)
                {
                    await server.WriteAsync(dataToCopy, 0, dataToCopy.Length);
                }
                else
                {
                    server.Write(new ReadOnlySpan<byte>(dataToCopy, 0, dataToCopy.Length));
                }

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
                    (isWindows && e is NetworkException) ||
                    (!isWindows && (e == null || e is NetworkException)),
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
        public async Task ReadWrite_Memory_LargeWrite_Success()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var writeBuffer = new byte[10 * 1024 * 1024];
                var readBuffer = new byte[writeBuffer.Length];
                RandomNumberGenerator.Fill(writeBuffer);

                ValueTask writeTask = client.WriteAsync((ReadOnlyMemory<byte>)writeBuffer);

                int totalRead = 0;
                while (totalRead < readBuffer.Length)
                {
                    int bytesRead = await server.ReadAsync(new Memory<byte>(readBuffer).Slice(totalRead));
                    Assert.InRange(bytesRead, 0, int.MaxValue);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    totalRead += bytesRead;
                }
                Assert.Equal(readBuffer.Length, totalRead);
                Assert.Equal<byte>(writeBuffer, readBuffer);

                await writeTask;
            });
        }

        [Fact]
        public async Task ReadWrite_Precanceled_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.WriteAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReadAsync((ArraySegment<byte>)new byte[0], new CancellationToken(true)));

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.WriteAsync((ReadOnlyMemory<byte>)new byte[0], new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.ReadAsync((Memory<byte>)new byte[0], new CancellationToken(true)));
            });
        }

        [Fact]
        public async Task ReadAsync_AwaitMultipleTimes_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                var b = new byte[1];
                ValueTask<int> r = server.ReadAsync(b);
                await client.WriteAsync(new byte[] { 42 });
                Assert.Equal(1, await r);
                Assert.Equal(42, b[0]);
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await r);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().IsCompleted);
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().GetResult());
            });
        }

        [Fact]
        public async Task ReadAsync_MultipleContinuations_Throws()
        {
            await RunWithConnectedNetworkStreamsAsync((server, client) =>
            {
                var b = new byte[1];
                ValueTask<int> r = server.ReadAsync(b);
                r.GetAwaiter().OnCompleted(() => { });
                Assert.Throws<InvalidOperationException>(() => r.GetAwaiter().OnCompleted(() => { }));
                return Task.CompletedTask;
            });
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

        public static IEnumerable<object[]> ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData() =>
            from flowExecutionContext in new[] { true, false }
            from continueOnCapturedContext in new bool?[] { null, false, true }
            select new object[] { flowExecutionContext, continueOnCapturedContext };

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        public async Task ReadAsync_ContinuesOnCurrentSynchronizationContextIfDesired(
            bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
                {
                    Assert.Null(SynchronizationContext.Current);

                    var continuationRan = new TaskCompletionSource<bool>();
                    var asyncLocal = new AsyncLocal<int>();
                    bool schedulerWasFlowed = false;
                    bool executionContextWasFlowed = false;
                    Action continuation = () =>
                    {
                        schedulerWasFlowed = SynchronizationContext.Current is CustomSynchronizationContext;
                        executionContextWasFlowed = 42 == asyncLocal.Value;
                        continuationRan.SetResult(true);
                    };

                    var readBuffer = new byte[1];
                    ValueTask<int> readValueTask = client.ReadAsync((Memory<byte>)new byte[1]);

                    SynchronizationContext.SetSynchronizationContext(new CustomSynchronizationContext());
                    asyncLocal.Value = 42;
                    switch (continueOnCapturedContext)
                    {
                        case null:
                            if (flowExecutionContext)
                            {
                                readValueTask.GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                        default:
                            if (flowExecutionContext)
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                            }
                            else
                            {
                                readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                            }
                            break;
                    }
                    asyncLocal.Value = 0;
                    SynchronizationContext.SetSynchronizationContext(null);

                    Assert.False(readValueTask.IsCompleted);
                    Assert.False(readValueTask.IsCompletedSuccessfully);
                    await server.WriteAsync(new byte[] { 42 });

                    await continuationRan.Task;
                    Assert.True(readValueTask.IsCompleted);
                    Assert.True(readValueTask.IsCompletedSuccessfully);

                    Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                    Assert.Equal(flowExecutionContext, executionContextWasFlowed);
                });
            });
        }

        [Theory]
        [MemberData(nameof(ReadAsync_ContinuesOnCurrentContextIfDesired_MemberData))]
        public async Task ReadAsync_ContinuesOnCurrentTaskSchedulerIfDesired(
            bool flowExecutionContext, bool? continueOnCapturedContext)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
                {
                    Assert.Null(SynchronizationContext.Current);

                    var continuationRan = new TaskCompletionSource();
                    var asyncLocal = new AsyncLocal<int>();
                    bool schedulerWasFlowed = false;
                    bool executionContextWasFlowed = false;
                    Action continuation = () =>
                    {
                        schedulerWasFlowed = TaskScheduler.Current is CustomTaskScheduler;
                        executionContextWasFlowed = 42 == asyncLocal.Value;
                        continuationRan.SetResult();
                    };

                    var readBuffer = new byte[1];
                    ValueTask<int> readValueTask = client.ReadAsync((Memory<byte>)new byte[1]);

                    await Task.Factory.StartNew(() =>
                    {
                        Assert.IsType<CustomTaskScheduler>(TaskScheduler.Current);
                        asyncLocal.Value = 42;
                        switch (continueOnCapturedContext)
                        {
                            case null:
                                if (flowExecutionContext)
                                {
                                    readValueTask.GetAwaiter().OnCompleted(continuation);
                                }
                                else
                                {
                                    readValueTask.GetAwaiter().UnsafeOnCompleted(continuation);
                                }
                                break;
                            default:
                                if (flowExecutionContext)
                                {
                                    readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(continuation);
                                }
                                else
                                {
                                    readValueTask.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().UnsafeOnCompleted(continuation);
                                }
                                break;
                        }
                        asyncLocal.Value = 0;
                    }, CancellationToken.None, TaskCreationOptions.None, new CustomTaskScheduler());

                    Assert.False(readValueTask.IsCompleted);
                    Assert.False(readValueTask.IsCompletedSuccessfully);
                    await server.WriteAsync(new byte[] { 42 });

                    await continuationRan.Task;
                    Assert.True(readValueTask.IsCompleted);
                    Assert.True(readValueTask.IsCompletedSuccessfully);

                    Assert.Equal(continueOnCapturedContext != false, schedulerWasFlowed);
                    Assert.Equal(flowExecutionContext, executionContextWasFlowed);
                });
            });
        }

        [Fact]
        public async Task DisposeAsync_ClosesStream()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                Assert.True(client.DisposeAsync().IsCompletedSuccessfully);
                Assert.True(server.DisposeAsync().IsCompletedSuccessfully);

                await client.DisposeAsync();
                await server.DisposeAsync();

                Assert.False(server.CanRead);
                Assert.False(server.CanWrite);

                Assert.False(client.CanRead);
                Assert.False(client.CanWrite);
            });
        }

        [Fact]
        public async Task ReadAsync_CancelPendingRead_DoesntImpactSubsequentReads()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await client.ReadAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)); });

                CancellationTokenSource cts = new CancellationTokenSource();
                Task<int> t = client.ReadAsync(new byte[1], 0, 1, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);

                cts = new CancellationTokenSource();
                ValueTask<int> vt = client.ReadAsync(new Memory<byte>(new byte[1]), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);

                byte[] buffer = new byte[1];
                vt = client.ReadAsync(new Memory<byte>(buffer));
                Assert.False(vt.IsCompleted);
                await server.WriteAsync(new ReadOnlyMemory<byte>(new byte[1] { 42 }));
                Assert.Equal(1, await vt);
                Assert.Equal(42, buffer[0]);
            });
        }

        [Fact]
        public async Task WriteAsync_CancelPendingWrite_SucceedsOrThrowsOperationCanceled()
        {
            await RunWithConnectedNetworkStreamsAsync(async (server, client) =>
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.WriteAsync(new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await client.WriteAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)); });

                byte[] hugeBuffer = new byte[100_000_000];
                Exception e;

                var cts = new CancellationTokenSource();
                Task t = client.WriteAsync(hugeBuffer, 0, hugeBuffer.Length, cts.Token);
                cts.Cancel();
                e = await Record.ExceptionAsync(async () => await t);
                if (e != null)
                {
                    Assert.IsAssignableFrom<OperationCanceledException>(e);
                }

                cts = new CancellationTokenSource();
                ValueTask vt = client.WriteAsync(new Memory<byte>(hugeBuffer), cts.Token);
                cts.Cancel();
                e = await Record.ExceptionAsync(async () => await vt);
                if (e != null)
                {
                    Assert.IsAssignableFrom<OperationCanceledException>(e);
                }
            });
        }

        private sealed class CustomSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object state)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SetSynchronizationContext(this);
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        SetSynchronizationContext(null);
                    }
                }, null);
            }
        }

        private sealed class CustomTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) => ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
            protected override IEnumerable<Task> GetScheduledTasks() => null;
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
