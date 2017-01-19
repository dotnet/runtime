// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class SendFile
    {
        public static readonly object[][] SendFile_MemberData = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };

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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendFile_MemberData))]
        private void SendFile_Synchronous(IPAddress listenAt, bool sendPreAndPostBuffers)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 10;
            const int TestTimeout = 30000;

            // Create file to send
            byte[] preBuffer;
            byte[] postBuffer;
            Fletcher32 sentChecksum;
            string filename = CreateFileToSend(BytesToSend, sendPreAndPostBuffers, out preBuffer, out postBuffer, out sentChecksum);

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

            Assert.Equal(BytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);

            // Clean up the file we created
            File.Delete(filename);
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(SendFile_MemberData))]
        private void SendFile_APM(IPAddress listenAt, bool sendPreAndPostBuffers)
        {
            const int BytesToSend = 123456;
            const int ListenBacklog = 1;
            const int LingerTime = 10;
            const int TestTimeout = 30000;

            // Create file to send
            byte[] preBuffer;
            byte[] postBuffer;
            Fletcher32 sentChecksum;
            string filename = CreateFileToSend(BytesToSend, sendPreAndPostBuffers, out preBuffer, out postBuffer, out sentChecksum);

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

            client.ConnectAPM(clientEndpoint, () =>
            {
                client.SendFileAPM(filename, preBuffer, postBuffer, TransmitFileOptions.UseDefaultWorkerThread, () =>
                {
                    client.LingerState = new LingerOption(true, LingerTime);
                    client.Dispose();
                });
            });

            Assert.True(serverFinished.Task.Wait(TestTimeout), "Completed within allowed time");

            Assert.Equal(BytesToSend, bytesReceived);
            Assert.Equal(sentChecksum.Sum, receivedChecksum.Sum);

            // Clean up the file we created
            File.Delete(filename);
        }
    }
}
