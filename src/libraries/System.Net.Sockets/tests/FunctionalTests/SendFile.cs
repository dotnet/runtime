// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SendFileTest
    {
        public static IEnumerable<object[]> SendFile_MemberData_Small() => SendFile_MemberData(1024);

        public static IEnumerable<object[]> SendFile_MemberData_Large() => SendFile_MemberData(12345678);

        public static IEnumerable<object[]> SendFile_MemberData(int bytesToSend)
        {
            foreach (IPAddress listenAt in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                foreach (bool sendPreAndPostBuffers in new[] { true, false })
                {
                    yield return new object[] { listenAt, sendPreAndPostBuffers, bytesToSend };
                }
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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendFile_MemberData_Small))]
        [MemberData(nameof(SendFile_MemberData_Large))]
        public void SendFile_Synchronous(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend)
        {
            const int ListenBacklog = 1;
            const int LingerTime = 10;
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

            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();
            var serverThread = new Thread(() =>
            {
                using (server)
                {
                    Socket remote = server.Accept();
                    Assert.NotNull(remote);

                    using (remote)
                    {
                        var recvBuffer = new byte[256];
                        for (;;)
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
            client.Connect(clientEndpoint);

            using (client)
            {
                client.SendFile(filename, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread);

                client.LingerState = new LingerOption(true, LingerTime);
            }

            Assert.True(serverThread.Join(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);

            // Clean up the file we created
            File.Delete(filename);
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendFile_MemberData_Large))]
        [ActiveIssue(17188, TestPlatforms.OSX)] // recombine into SendFile_APM once fixed
        public void SendFile_APM_Large(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend) =>
            SendFile_APM(listenAt, sendPreAndPostBuffers, bytesToSend);

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendFile_MemberData_Small))]
        public void SendFile_APM(IPAddress listenAt, bool sendPreAndPostBuffers, int bytesToSend)
        {
            const int ListenBacklog = 1;
            const int LingerTime = 10;
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

            var serverFinished = new TaskCompletionSource<bool>();
            int bytesReceived = 0;
            var receivedChecksum = new Fletcher32();

            server.AcceptAPM(remote =>
            {
                Action<int> recvHandler = null;
                bool first = true;

                var recvBuffer = new byte[256];
                recvHandler = received => 
                {
                    if (!first)
                    {
                        if (received == 0)
                        {
                            remote.Dispose();
                            server.Dispose();
                            serverFinished.SetResult(true);
                            return;
                        }

                        bytesReceived += received;
                        receivedChecksum.Add(recvBuffer, 0, received);
                    }
                    else
                    {
                        first = false;
                    }

                    remote.ReceiveAPM(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, recvHandler);
                };

                recvHandler(0);
            });

            // Run client
            EndPoint clientEndpoint = server.LocalEndPoint;
            var client = new Socket(clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var clientFinished = new TaskCompletionSource<bool>();
            client.ConnectAPM(clientEndpoint, () =>
            {
                client.SendFileAPM(filename, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread, ex =>
                {
                    client.LingerState = new LingerOption(true, LingerTime);
                    client.Dispose();

                    if (ex != null)
                    {
                        clientFinished.SetException(ex);
                    }
                    else
                    {
                        clientFinished.SetResult(true);
                    }
                });
            });

            Assert.True(clientFinished.Task.Wait(TestTimeout), "Completed within allowed time");
            Assert.True(serverFinished.Task.Wait(TestTimeout), "Completed within allowed time");

            Assert.Equal(bytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);

            // Clean up the file we created
            File.Delete(filename);
        }
    }
}
