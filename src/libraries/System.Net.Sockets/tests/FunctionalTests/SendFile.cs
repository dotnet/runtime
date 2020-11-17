// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Sdk;

namespace System.Net.Sockets.Tests
{
    public class SendFileTest : FileCleanupTestBase
    {
        public static IEnumerable<object[]> SendFile_MemberData()
        {
            foreach (IPAddress listenAt in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                foreach (bool sendPreAndPostBuffers in new[] { true, false })
                {
                    foreach (int bytesToSend in new[] { 512, 1024, 12345678 })
                    {
                        yield return new object[] { listenAt, sendPreAndPostBuffers, bytesToSend };
                    }
                }
            }
        }

        public static IEnumerable<object[]> SendFileSync_MemberData()
        {
            foreach (object[] memberData in SendFile_MemberData())
            {
                yield return memberData.Concat(new object[] { true }).ToArray();
                yield return memberData.Concat(new object[] { false }).ToArray();
            }
        }

        private string CreateFileToSend(int size, bool sendPreAndPostBuffers, out byte[] preBuffer, out byte[] postBuffer, out Fletcher32 checksum)
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

            string path = Path.GetTempFileName();
            File.WriteAllBytes(path, fileBuffer);

            checksum.Add(fileBuffer, 0, fileBuffer.Length);

            postBuffer = null;
            if (sendPreAndPostBuffers)
            {
                postBuffer = new byte[256];
                random.NextBytes(postBuffer);
                checksum.Add(postBuffer, 0, postBuffer.Length);
            }

            return path;
        }

        [Fact]
        public void Disposed_ThrowsException()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                s.Dispose();
                Assert.Throws<ObjectDisposedException>(() => s.SendFile(null));
                Assert.Throws<ObjectDisposedException>(() => s.BeginSendFile(null, null, null));
                Assert.Throws<ObjectDisposedException>(() => s.BeginSendFile(null, null, null, TransmitFileOptions.UseDefaultWorkerThread, null, null));
                Assert.Throws<ObjectDisposedException>(() => s.EndSendFile(null));
            }
        }

        [Fact]
        public void EndSendFile_NullAsyncResult_Throws()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<ArgumentNullException>(() => s.EndSendFile(null));
            }
        }

        [Fact]
        public void NotConnected_ThrowsException()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<NotSupportedException>(() => s.SendFile(null));
                Assert.Throws<NotSupportedException>(() => s.BeginSendFile(null, null, null));
                Assert.Throws<NotSupportedException>(() => s.BeginSendFile(null, null, null, TransmitFileOptions.UseDefaultWorkerThread, null, null));
            }
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task SendFile_NoFile_Succeeds(bool useAsync, bool usePreBuffer, bool usePostBuffer)
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.BindToAnonymousPort(IPAddress.Loopback);
            listener.Listen(1);

            client.Connect(listener.LocalEndPoint);
            using Socket server = listener.Accept();

            if (useAsync)
            {
                await Task.Factory.FromAsync<string>(server.BeginSendFile, server.EndSendFile, null, null);
            }
            else
            {
                server.SendFile(null);
            }
            Assert.Equal(0, client.Available);

            byte[] preBuffer = usePreBuffer ? new byte[1] : null;
            byte[] postBuffer = usePostBuffer ? new byte[1] : null;
            int bytesExpected = (usePreBuffer ? 1 : 0) + (usePostBuffer ? 1 : 0);

            if (useAsync)
            {
                await Task.Factory.FromAsync((c, s) => server.BeginSendFile(null, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread, c, s), server.EndSendFile, null);
            }
            else
            {
                server.SendFile(null, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);
            }

            byte[] receiveBuffer = new byte[1];
            for (int i = 0; i < bytesExpected; i++)
            {
                Assert.Equal(1, client.Receive(receiveBuffer));
            }

            Assert.Equal(0, client.Available);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/42534", TestPlatforms.Windows)]
        [OuterLoop("Creates and sends a file several gigabytes long")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendFile_GreaterThan2GBFile_SendsAllBytes(bool useAsync)
        {
            const long FileLength = 100L + int.MaxValue;

            string tmpFile = GetTestFilePath();
            using (FileStream fs = File.Create(tmpFile))
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
                Task.Run(async () =>
                {
                    if (useAsync)
                    {
                        await Task.Factory.FromAsync(server.BeginSendFile, server.EndSendFile, tmpFile, null);
                    }
                    else
                    {
                        server.SendFile(tmpFile);
                    }
                }),
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

        [OuterLoop]
        [Theory]
        [MemberData(nameof(SendFileSync_MemberData))]
        public void SendFile_Synchronous(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend, bool forceNonBlocking)
        {
            const int ListenBacklog = 1;
            const int TestTimeout = 30000;

            // Create file to send
            byte[] preBuffer;
            byte[] postBuffer;
            Fletcher32 sentChecksum;
            string filename = CreateFileToSend(bytesToSend, sendPreAndPostBuffers, out preBuffer, out postBuffer, out sentChecksum);

            // Start server
            var server = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.BindToAnonymousPort(listenAt);

            server.Listen(ListenBacklog);

            server.ForceNonBlocking(forceNonBlocking);

            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();
            var serverThread = new Thread(() =>
            {
                using (server)
                {
                    Socket remote = server.Accept();
                    Assert.NotNull(remote);

                    remote.ForceNonBlocking(forceNonBlocking);

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
            serverThread.Start();

            // Run client
            EndPoint clientEndpoint = server.LocalEndPoint;
            var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            client.ForceNonBlocking(forceNonBlocking);

            client.Connect(clientEndpoint);

            using (client)
            {
                client.SendFile(filename, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);
                client.Shutdown(SocketShutdown.Send);
            }

            Assert.True(serverThread.Join(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);

            // Clean up the file we created
            File.Delete(filename);
        }

        [Fact]
        public async Task SyncSendFileGetsCanceledByDispose()
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
                    Task socketOperation = Task.Run(() =>
                    {
                        // Create a large file that will cause SendFile to block until the peer starts reading.
                        string filename = GetTestFilePath();
                        using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
                        {
                            fs.SetLength(20 * 1024 * 1024 /* 20MB */);
                        }

                        socket1.SendFile(filename);
                    });

                    // Wait a little so the operation is started.
                    await Task.Delay(msDelay);
                    msDelay *= 2;
                    Task disposeTask = Task.Run(() => socket1.Dispose());

                    await Task.WhenAny(disposeTask, socketOperation).TimeoutAfter(30000);
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
                    catch (ObjectDisposedException)
                    { }
                    Assert.Equal(SocketError.ConnectionAborted, localSocketError);

                    // On OSX, we're unable to unblock the on-going socket operations and
                    // perform an abortive close.
                    if (!PlatformDetection.IsOSXLike)
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

        [OuterLoop]
        [Theory]
        [MemberData(nameof(SendFile_MemberData))]
        public async Task SendFile_APM(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend)
        {
            const int ListenBacklog = 1, TestTimeout = 30000;

            // Create file to send
            byte[] preBuffer, postBuffer;
            Fletcher32 sentChecksum;
            string filename = CreateFileToSend(bytesToSend, sendPreAndPostBuffers, out preBuffer, out postBuffer, out sentChecksum);

            // Start server
            using (var listener = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(listenAt);
                listener.Listen(ListenBacklog);

                int bytesReceived = 0;
                var receivedChecksum = new Fletcher32();

                Task serverTask = Task.Run(async () =>
                {
                    using (var serverStream = new NetworkStream(await listener.AcceptAsync(), ownsSocket: true))
                    {
                        var buffer = new byte[256];
                        int bytesRead;
                        while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            bytesReceived += bytesRead;
                            receivedChecksum.Add(buffer, 0, bytesRead);
                        }
                    }
                });
                Task clientTask = Task.Run(async () =>
                {
                    using (var client = new Socket(listener.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                    {
                        await client.ConnectAsync(listener.LocalEndPoint);
                        await Task.Factory.FromAsync(
                            (callback, state) => client.BeginSendFile(filename, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread, callback, state),
                            iar => client.EndSendFile(iar),
                            null);
                        client.Shutdown(SocketShutdown.Send);
                    }
                });

                // Wait for the tasks to complete
                await (new[] { serverTask, clientTask }).WhenAllOrAnyFailed(TestTimeout);

                // Validate the results
                Assert.Equal(bytesToSend, bytesReceived);
                Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);
            }

            // Clean up the file we created
            File.Delete(filename);
        }
    }
}
