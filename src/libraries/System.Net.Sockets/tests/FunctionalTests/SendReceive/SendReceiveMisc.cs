// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SendReceiveMisc
    {
        [Fact]
        public void SendRecvIovMaxTcp_Success()
        {
            // sending/receiving more than IOV_MAX segments causes EMSGSIZE on some platforms.
            // This is handled internally for stream sockets so this error shouldn't surface.

            // Use more than IOV_MAX (1024 on Linux & macOS) segments.
            const int SegmentCount = 2400;
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                var sendBuffer = new byte[SegmentCount];
                Task serverProcessingTask = Task.Run(() =>
                {
                    using (Socket acceptSocket = server.Accept())
                    {
                        // send data as SegmentCount (> IOV_MAX) 1-byte segments.
                        var sendSegments = new List<ArraySegment<byte>>();
                        for (int i = 0; i < SegmentCount; i++)
                        {
                            sendBuffer[i] = (byte)i;
                            sendSegments.Add(new ArraySegment<byte>(sendBuffer, i, 1));
                        }
                        SocketError error;
                        // Send blocks until all segments are sent.
                        int bytesSent = acceptSocket.Send(sendSegments, SocketFlags.None, out error);

                        Assert.Equal(SegmentCount, bytesSent);
                        Assert.Equal(SocketError.Success, error);

                        acceptSocket.Shutdown(SocketShutdown.Send);
                    }
                });

                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.Connect(server.LocalEndPoint);

                    // receive data as 1-byte segments.
                    var receiveBuffer = new byte[SegmentCount];
                    var receiveSegments = new List<ArraySegment<byte>>();
                    for (int i = 0; i < SegmentCount; i++)
                    {
                        receiveSegments.Add(new ArraySegment<byte>(receiveBuffer, i, 1));
                    }
                    var bytesReceivedTotal = 0;
                    do
                    {
                        SocketError error;
                        // Receive can return up to IOV_MAX segments.
                        int bytesReceived = client.Receive(receiveSegments, SocketFlags.None, out error);
                        bytesReceivedTotal += bytesReceived;
                        // Offset receiveSegments for next Receive.
                        receiveSegments.RemoveRange(0, bytesReceived);

                        Assert.NotEqual(0, bytesReceived);
                        Assert.Equal(SocketError.Success, error);
                    } while (bytesReceivedTotal != SegmentCount);

                    AssertExtensions.Equal(sendBuffer, receiveBuffer);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public void SendIovMaxUdp_SuccessOrMessageSize()
        {
            // sending more than IOV_MAX segments causes EMSGSIZE on some platforms.
            // We handle this for stream sockets by truncating.
            // This test verifies we are not truncating non-stream sockets.

            // Use more than IOV_MAX (1024 on Linux & macOS) segments
            // and less than Ethernet MTU.
            const int SegmentCount = 1200;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback);
                // Use our own address as destination.
                socket.Connect(socket.LocalEndPoint);

                var sendBuffer = new byte[SegmentCount];
                var sendSegments = new List<ArraySegment<byte>>();
                for (int i = 0; i < SegmentCount; i++)
                {
                    sendBuffer[i] = (byte)i;
                    sendSegments.Add(new ArraySegment<byte>(sendBuffer, i, 1));
                }

                SocketError error;
                // send data as SegmentCount (> IOV_MAX) 1-byte segments.
                int bytesSent = socket.Send(sendSegments, SocketFlags.None, out error);
                if (error == SocketError.Success)
                {
                    // platform sent message with > IOV_MAX segments
                    Assert.Equal(SegmentCount, bytesSent);
                }
                else
                {
                    // platform returns EMSGSIZE
                    Assert.Equal(SocketError.MessageSize, error);
                    Assert.Equal(0, bytesSent);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public async Task ReceiveIovMaxUdp_SuccessOrMessageSize()
        {
            // receiving more than IOV_MAX segments causes EMSGSIZE on some platforms.
            // We handle this for stream sockets by truncating.
            // This test verifies we are not truncating non-stream sockets.

            // Use more than IOV_MAX (1024 on Linux & macOS) segments
            // and less than Ethernet MTU.
            const int SegmentCount = 1200;
            var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.BindToAnonymousPort(IPAddress.Loopback);
            var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.Connect(sender.LocalEndPoint); // only receive from sender
            EndPoint receiverEndPoint = receiver.LocalEndPoint;

            Barrier b = new Barrier(2);

            Task receiveTask = Task.Run(() =>
            {
                using (receiver)
                {
                    var receiveBuffer = new byte[SegmentCount];
                    var receiveSegments = new List<ArraySegment<byte>>();
                    for (int i = 0; i < SegmentCount; i++)
                    {
                        receiveSegments.Add(new ArraySegment<byte>(receiveBuffer, i, 1));
                    }
                    // receive data as SegmentCount (> IOV_MAX) 1-byte segments.
                    SocketError error;
                    // Signal we are ready to receive.
                    b.SignalAndWait();
                    int bytesReceived = receiver.Receive(receiveSegments, SocketFlags.None, out error);

                    if (error == SocketError.Success)
                    {
                        // platform received message in > IOV_MAX segments
                        Assert.Equal(SegmentCount, bytesReceived);
                    }
                    else
                    {
                        // platform returns EMSGSIZE
                        Assert.Equal(SocketError.MessageSize, error);
                        Assert.Equal(0, bytesReceived);
                    }
                }
            });

            using (sender)
            {
                sender.Connect(receiverEndPoint);

                // Synchronize and wait for receiving task to be ready.
                b.SignalAndWait();

                var sendBuffer = new byte[SegmentCount];
                for (int i = 0; i < 10; i++) // UDPRedundancy
                {
                    int bytesSent = sender.Send(sendBuffer);
                    Assert.Equal(SegmentCount, bytesSent);
                    try
                    {
                        await receiveTask.WaitAsync(TimeSpan.FromMilliseconds(3));
                        break;
                    }
                    catch (TimeoutException) { }
                }
            }

            Assert.True(receiveTask.IsCompleted);
            await receiveTask;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        [SkipOnPlatform(TestPlatforms.Windows, "All data is sent, even when very large (100M).")]
        public void SocketSendWouldBlock_ReturnsBytesSent()
        {
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // listen
                server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);
                // connect
                client.Connect(server.LocalEndPoint);
                // accept
                using (Socket socket = server.Accept())
                {
                    // We send a large amount of data but don't read it.
                    // A chunck will be sent, attempts to send more will return SocketError.WouldBlock.
                    // Socket.Send must return the success of the partial send.
                    socket.Blocking = false;
                    var data = new byte[5_000_000];
                    SocketError error;
                    int bytesSent = socket.Send(data, 0, data.Length, SocketFlags.None, out error);

                    Assert.Equal(SocketError.Success, error);
                    Assert.InRange(bytesSent, 1, data.Length - 1);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public async Task Socket_ReceiveFlags_Success()
        {
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.BindToAnonymousPort(IPAddress.Loopback);
                sender.Connect(receiver.LocalEndPoint);
                sender.SendBufferSize = 1500;

                var data = new byte[500];
                data[0] = data[499] = 1;

                Assert.Equal(500, sender.Send(data));
                data[0] = data[499] = 2;
                Assert.Equal(500, sender.Send(data));

                var tcs = new TaskCompletionSource();
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();

                var receiveBuffer = new byte[600];
                receiveBuffer[0] = data[499] = 0;

                args.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                args.Completed += delegate { tcs.SetResult(); };

                // First peek at the message.
                args.SocketFlags = SocketFlags.Peek;
                if (receiver.ReceiveAsync(args))
                {
                    await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                }
                Assert.Equal(SocketFlags.None, args.SocketFlags);
                Assert.Equal(1, receiveBuffer[0]);
                Assert.Equal(1, receiveBuffer[499]);
                receiveBuffer[0] = receiveBuffer[499] = 0;

                // Now, we should be able to get same message again.
                tcs = new TaskCompletionSource();
                args.SocketFlags = SocketFlags.None;
                if (receiver.ReceiveAsync(args))
                {
                    await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                }
                Assert.Equal(SocketFlags.None, args.SocketFlags);
                Assert.Equal(1, receiveBuffer[0]);
                Assert.Equal(1, receiveBuffer[499]);
                receiveBuffer[0] = receiveBuffer[499] = 0;

                // Set buffer smaller than message.
                tcs = new TaskCompletionSource();
                args.SetBuffer(receiveBuffer, 0, 100);
                if (receiver.ReceiveAsync(args))
                {
                    await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(TestSettings.PassingTestTimeout));
                }
                Assert.Equal(SocketFlags.Truncated, args.SocketFlags);
                Assert.Equal(2, receiveBuffer[0]);

                // There should be no more data.
                Assert.Equal(0, receiver.Available);
            }
        }
    }
}
