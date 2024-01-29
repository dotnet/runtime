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

            if (usePreAndPostbufferOverload)
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => SendFileAsync(client, tempFile.Path, Array.Empty<byte>(), Array.Empty<byte>(), TransmitFileOptions.UseDefaultWorkerThread));
            }
            else
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => SendFileAsync(client, tempFile.Path));
            }
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

            await serverTask.WaitAsync(TimeSpan.FromMilliseconds(TestTimeout));
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73536", TestPlatforms.iOS | TestPlatforms.tvOS)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80169", typeof(PlatformDetection), nameof(PlatformDetection.IsApplePlatform))]
        public async Task SendFileGetsCanceledByDispose(bool owning)
        {
            // Aborting sync operations for non-owning handles is not supported on Unix.
            if (!owning && UsesSync && !PlatformDetection.IsWindows)
            {
                return;
            }

            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, the peer won't see a ConnectionReset SocketException and we won't
            // see a SocketException either.
            await RetryHelper.ExecuteAsync(async () =>
            {
                (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
                using SafeSocketHandle? owner = ReplaceWithNonOwning(ref socket1, owning);

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

                    // read one byte to make sure SendFileAsync started
                    byte[] buffer = new byte[1];
                    socket2.Receive(buffer);

                    Task disposeTask = Task.Run(() => socket1.Dispose());

                    await Task.WhenAny(disposeTask, socketOperation).WaitAsync(TimeSpan.FromSeconds(30));
                    await disposeTask;

                    SocketError? localSocketError = null;

                    try
                    {
                        await socketOperation;
                    }
                    catch (SocketException se)
                    {
                        localSocketError = se.SocketErrorCode;
                    }

                    if (UsesSync)
                    {
                        Assert.Equal(SocketError.ConnectionAborted, localSocketError);
                    }
                    else
                    {
                        Assert.Equal(SocketError.OperationAborted, localSocketError);
                    }

                    owner?.Dispose();

                    // On OSX, we're unable to unblock the on-going socket operations and
                    // perform an abortive close.
                    if (!(UsesSync && PlatformDetection.IsApplePlatform))
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

    public sealed class SendFile_Task : SendFile<SocketHelperTask>
    {
        public SendFile_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_CancellableTask : SendFile<SocketHelperCancellableTask>
    {
        public SendFile_CancellableTask(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task SendFileAsync_Precanceled_Throws()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await server.SendFileAsync(null, ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread, cts.Token));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendFileAsync_CanceledDuringOperation_Throws(bool ipv6)
        {
            const int CancelAfter = 200; // ms
            const int NumOfSends = 100;
            const int SendBufferSize = 1024;

            (Socket client, Socket server) = SocketTestExtensions.CreateConnectedSocketPair(ipv6);
            byte[] buffer = new byte[1024 * 64];
            using (client)
            using (server)
            {
                client.SendBufferSize = SendBufferSize;
                CancellationTokenSource cts = new CancellationTokenSource();

                List<Task> tasks = new List<Task>();

                // After flooding the socket with a high number of SendFile tasks,
                // we assume some of them won't complete before the "CancelAfter" period expires.
                for (int i = 0; i < NumOfSends; i++)
                {
                    var task = server.SendFileAsync(null, buffer, ReadOnlyMemory<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread, cts.Token).AsTask();
                    tasks.Add(task);
                }

                cts.CancelAfter(CancelAfter);

                // We shall see at least one cancellation amongst all the scheduled sends:
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(tasks));
            }
        }
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
        }

        [Fact]
        public void EndSendFile_NullAsyncResult_Throws()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.Throws<ArgumentNullException>(() => s.EndSendFile(null));
        }
    }

    // Running all cases of GreaterThan2GBFile_SendsAllBytes in parallel may attempt to allocate Min(ProcessorCount, Subclass_Count) * 2GB of disk space
    // in extreme cases. Some CI machines may run out of disk space if this happens.
    [Collection(nameof(DisableParallelization))]
    public abstract class SendFile_NonParallel<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected SendFile_NonParallel(ITestOutputHelper output) : base(output)
        {
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
    }

    public sealed class SendFile_NonParallel_SyncSpan : SendFile_NonParallel<SocketHelperSpanSync>
    {
        public SendFile_NonParallel_SyncSpan(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_NonParallel_SyncSpanForceNonBlocking : SendFile_NonParallel<SocketHelperSpanSyncForceNonBlocking>
    {
        public SendFile_NonParallel_SyncSpanForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_NonParallel_ArraySync : SendFile_NonParallel<SocketHelperArraySync>
    {
        public SendFile_NonParallel_ArraySync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_NonParallel_Task : SendFile_NonParallel<SocketHelperTask>
    {
        public SendFile_NonParallel_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SendFile_NonParallel_Apm : SendFile_NonParallel<SocketHelperApm>
    {
        public SendFile_NonParallel_Apm(ITestOutputHelper output) : base(output) { }
    }
}
