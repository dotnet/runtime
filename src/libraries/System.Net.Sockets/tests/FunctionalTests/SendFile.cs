// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Net.Sockets.Tests
{
    public abstract class SendFile<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected SendFile(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Disposed_ThrowsException()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => SendFileAsync(s, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => SendFileAsync(s, null, null, null, TransmitFileOptions.UseDefaultWorkerThread));
        }


        [Fact]
        public async Task NotConnected_ThrowsNotSupportedException()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            await Assert.ThrowsAsync<NotSupportedException>(() => SendFileAsync(s, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => SendFileAsync(s, null, null, null, TransmitFileOptions.UseDefaultWorkerThread));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FileDoesNotExist_ThrowsFileNotFoundException(bool useOverloadWithBuffers)
        {
            string doesNotExist = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair();

            using (client)
            using (server)
            {
                if (!useOverloadWithBuffers)
                {
                    await Assert.ThrowsAsync<FileNotFoundException>(() => SendFileAsync(client, doesNotExist));
                }
                else
                {
                    await Assert.ThrowsAsync<FileNotFoundException>(() => SendFileAsync(client, doesNotExist, null, null, TransmitFileOptions.UseDefaultWorkerThread));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task UdpConnection_ThrowsException(bool usePreAndPostbufferOverload)
        {
            // Create file to send
            byte[] preBuffer;
            byte[] postBuffer;
            Fletcher32 sentChecksum;
            using TempFile tempFile = CreateFileToSend(size: 1, sendPreAndPostBuffers: false, out preBuffer, out postBuffer, out sentChecksum);

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listener.BindToAnonymousPort(IPAddress.Loopback);

            client.Connect(listener.LocalEndPoint);

            SocketException ex;
            if (usePreAndPostbufferOverload)
            {
                ex = await Assert.ThrowsAsync<SocketException>(() => SendFileAsync(client, tempFile.Path, Array.Empty<byte>(), Array.Empty<byte>(), TransmitFileOptions.UseDefaultWorkerThread));
            }
            else
            {
                ex = await Assert.ThrowsAsync<SocketException>(() => SendFileAsync(client, tempFile.Path));
            }
            Assert.Equal(SocketError.NotConnected, ex.SocketErrorCode);
        }

        public static IEnumerable<object[]> SendFile_MemberData()
        {
            foreach (IPAddress listenAt in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                foreach (bool sendPreAndPostBuffers in new[] { true, false })
                {
                    foreach (int bytesToSend in new[] { 512, 1024 })
                    {
                        yield return new object[] { listenAt, sendPreAndPostBuffers, bytesToSend };
                    }
                }
            }
        }

        [OuterLoop("Creates a file of ~12MB, execution takes long.")]
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public Task IncludeFile_Success_LargeFile(IPAddress listenAt) => IncludeFile_Success(listenAt, true, 12_345_678);

        [Theory]
        [MemberData(nameof(SendFile_MemberData))]
        public async Task IncludeFile_Success(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend)
        {
            const int ListenBacklog = 1;
            const int TestTimeout = 30000;

            // Create file to send
            byte[] preBuffer;
            byte[] postBuffer;
            Fletcher32 sentChecksum;
            using TempFile tempFile = CreateFileToSend(bytesToSend, sendPreAndPostBuffers, out preBuffer, out postBuffer, out sentChecksum);

            // Start server
            var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.BindToAnonymousPort(listenAt);
            Listen(server, ListenBacklog); // Configures NonBlocking behavior

            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();
            var serverTask = Task.Run(() =>
            {
                using (server)
                {
                    Socket remote = server.Accept();
                    Assert.NotNull(remote);

                    using (remote)
                    {
                        var recvBuffer = new byte[256];
                        while (true)
                        {
                            int received = remote.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None);
                            if (received == 0)
                            {
                                break;
                            }

                            bytesReceived += received;
                            receivedChecksum.Add(recvBuffer, 0, received);
                        }
                    }
                }
            });

            // Run client
            EndPoint serverEndpoint = server.LocalEndPoint;
            using (var client = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                await ConnectAsync(client, serverEndpoint); // Configures NonBlocking behavior
                await SendFileAsync(client, tempFile.Path, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);
                client.Shutdown(SocketShutdown.Send);
            }

            await serverTask.TimeoutAfter(TestTimeout);
            Assert.Equal(bytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task NoFile_Succeeds(bool usePreBuffer, bool usePostBuffer)
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.BindToAnonymousPort(IPAddress.Loopback);
            listener.Listen(1);

            client.Connect(listener.LocalEndPoint);
            using Socket server = listener.Accept();

            await SendFileAsync(server, null);
            Assert.Equal(0, client.Available);

            byte[] preBuffer = usePreBuffer ? new byte[1] : null;
            byte[] postBuffer = usePostBuffer ? new byte[1] : null;
            int bytesExpected = (usePreBuffer ? 1 : 0) + (usePostBuffer ? 1 : 0);

            await SendFileAsync(server, null, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);

            byte[] receiveBuffer = new byte[1];
            for (int i = 0; i < bytesExpected; i++)
            {
                Assert.Equal(1, client.Receive(receiveBuffer));
            }

            Assert.Equal(0, client.Available);
        }

        [Fact]
        public async Task SliceBuffers_Success()
        {
            if (!SupportsSendFileSlicing) return; // The overloads under test only support sending byte[] without offset and length

            Random rnd = new Random(0);

            ArraySegment<byte> preBuffer = new ArraySegment<byte>(new byte[100], 25, 50);
            ArraySegment<byte> postBuffer = new ArraySegment<byte>(new byte[100], 25, 50);
            rnd.NextBytes(preBuffer);
            rnd.NextBytes(postBuffer);

            byte[] expected = preBuffer.ToArray().Concat(postBuffer.ToArray()).ToArray();
            uint expectedChecksum = Fletcher32.Checksum(expected, 0, expected.Length);

            (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair();

            using (client)
            using (server)
            {
                await SendFileAsync(client, null, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);
                Fletcher32 receivedChecksum = new Fletcher32();
                byte[] receiveBuffer = new byte[expected.Length];
                int receivedBytes;
                int totalReceived = 0;
                while (totalReceived < expected.Length && (receivedBytes = server.Receive(receiveBuffer)) != 0)
                {
                    totalReceived += receivedBytes;
                    receivedChecksum.Add(receiveBuffer, 0, receivedBytes);
                }
                Assert.Equal(expected.Length, totalReceived);
                Assert.Equal(expectedChecksum, receivedChecksum.Sum);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/42534", TestPlatforms.Windows)]
        [OuterLoop("Creates and sends a file several gigabytes long")]
        [Fact]
        public async Task GreaterThan2GBFile_SendsAllBytes()
        {
            const long FileLength = 100L + int.MaxValue;

            using var tmpFile = TempFile.Create();
            using (FileStream fs = File.Create(tmpFile.Path))
            {
                fs.SetLength(FileLength);
            }

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.BindToAnonymousPort(IPAddress.Loopback);
            listener.Listen(1);

            client.Connect(listener.LocalEndPoint);
            using Socket server = listener.Accept();

            await new Task[]
            {
                SendFileAsync(server, tmpFile.Path),
                Task.Run(() =>
                {
                    byte[] buffer = new byte[100_000];
                    long count = 0;
                    while (count < FileLength)
                    {
                        int received = client.Receive(buffer);
                        Assert.NotEqual(0, received);
                        count += received;
                    }
                    Assert.Equal(0, client.Available);
                })
            }.WhenAllOrAnyFailed();
        }

        [Fact]
        public async Task SendFileGetsCanceledByDispose()
        {
            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, the peer won't see a ConnectionReset SocketException and we won't
            // see a SocketException either.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
                using (socket2)
                {
                    Task socketOperation = Task.Run(async () =>
                    {
                        // Create a large file that will cause SendFile to block until the peer starts reading.
                        using var tempFile = TempFile.Create();
                        using (var fs = new FileStream(tempFile.Path, FileMode.CreateNew, FileAccess.Write))
                        {
                            fs.SetLength(20 * 1024 * 1024 /* 20MB */);
                        }

                        await SendFileAsync(socket1, tempFile.Path);
                    });

                    // Wait a little so the operation is started.
                    await Task.Delay(msDelay);
                    msDelay *= 2;
                    Task disposeTask = Task.Run(() => socket1.Dispose());

                    await Task.WhenAny(disposeTask, socketOperation).TimeoutAfter(30000);
                    await disposeTask;

                    SocketError? localSocketError = null;
                    bool thrownDisposed = false;
                    try
                    {
                        await socketOperation;
                    }
                    catch (SocketException se)
                    {
                        localSocketError = se.SocketErrorCode;
                    }
                    catch (ObjectDisposedException)
                    {
                        thrownDisposed = true;
                    }

                    if (UsesSync)
                    {
                        Assert.Equal(SocketError.ConnectionAborted, localSocketError);
                    }
                    else
                    {
                        Assert.True(thrownDisposed);
                    }
                    

                    // On OSX, we're unable to unblock the on-going socket operations and
                    // perform an abortive close.
                    if (!(UsesSync && PlatformDetection.IsOSXLike))
                    {
                        SocketError? peerSocketError = null;
                        var receiveBuffer = new byte[4096];
                        while (true)
                        {
                            try
                            {
                                int received = socket2.Receive(receiveBuffer);
                                if (received == 0)
                                {
                                    break;
                                }
                            }
                            catch (SocketException se)
                            {
                                peerSocketError = se.SocketErrorCode;
                                break;
                            }
                        }
                        Assert.Equal(SocketError.ConnectionReset, peerSocketError);
                    }
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        private TempFile CreateFileToSend(int size, bool sendPreAndPostBuffers, out byte[] preBuffer, out byte[] postBuffer, out Fletcher32 checksum)
        {
            // Create file to send
            var random = new Random();
            int fileSize = sendPreAndPostBuffers ? size - 512 : size;

            checksum = new Fletcher32();

            preBuffer = null;
            if (sendPreAndPostBuffers)
            {
                preBuffer = new byte[256];
                random.NextBytes(preBuffer);
                checksum.Add(preBuffer, 0, preBuffer.Length);
            }

            byte[] fileBuffer = new byte[fileSize];
            random.NextBytes(fileBuffer);

            var tempFile = TempFile.Create(fileBuffer);

            checksum.Add(fileBuffer, 0, fileBuffer.Length);

            postBuffer = null;
            if (sendPreAndPostBuffers)
            {
                postBuffer = new byte[256];
                random.NextBytes(postBuffer);
                checksum.Add(postBuffer, 0, postBuffer.Length);
            }

            return tempFile;
        }
    }

    public sealed class SendFile_SyncSpan : SendFile<SocketHelperSpanSync>
    {
        public SendFile_SyncSpan(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_SyncSpanForceNonBlocking : SendFile<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendFile_SyncSpanForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_ArraySync : SendFile<SocketHelperArraySync>
    {
        public SendFile_ArraySync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_SyncForceNonBlocking : SendFile<SocketHelperSyncForceNonBlocking>
    {
        public SendFile_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_Apm : SendFile<SocketHelperApm>
    {
        public SendFile_Apm(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void IndividualBeginEndMethods_Disposed_ThrowsObjectDisposedException()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Dispose();
            Assert.Throws<ObjectDisposedException>(() => s.BeginSendFile(null, null, null));
            Assert.Throws<ObjectDisposedException>(() => s.BeginSendFile(null, null, null, TransmitFileOptions.UseDefaultWorkerThread, null, null));
            Assert.Throws<ObjectDisposedException>(() => s.EndSendFile(null));
        }

        [Fact]
        public void EndSendFile_NullAsyncResult_Throws()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.Throws<ArgumentNullException>(() => s.EndSendFile(null));
        }
    }
}
