// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SendFileTest
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

        [OuterLoop] // TODO: Issue #11345
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
                        for (; ; )
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

        [OuterLoop] // TODO: Issue #11345
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
