// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public class SelectAndPollTests
    {
        private const int SelectTimeout = 100;
        private const int SelectSuccessTimeoutMicroseconds = 5*1000*1000; // 5 seconds

        [Fact]
        public void SelectNone_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, null, null, SelectSuccessTimeoutMicroseconds));
        }

        [Fact]
        public void Select_Read_NotASocket_Throws()
        {
            var list = new List<object> { new object() };
            AssertExtensions.Throws<ArgumentException>("socketList", () => Socket.Select(list, null, null, SelectSuccessTimeoutMicroseconds));
        }

        [Fact]
        public void SelectRead_Single_Success()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int receiverPort = receiver.BindToAnonymousPort(IPAddress.Loopback);
                var receiverEndpoint = new IPEndPoint(IPAddress.Loopback, receiverPort);

                sender.SendTo(new byte[1], SocketFlags.None, receiverEndpoint);

                var list = new List<Socket> { receiver };
                Socket.Select(list, null, null, SelectSuccessTimeoutMicroseconds);

                Assert.Equal(1, list.Count);
                Assert.Equal(receiver, list[0]);
            }
        }

        [Fact]
        public void SelectRead_Single_Timeout()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.BindToAnonymousPort(IPAddress.Loopback);

                var list = new List<Socket> { receiver };
                Socket.Select(list, null, null, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void SelectRead_Multiple_Success()
        {
            using (var firstReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var secondReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int firstReceiverPort = firstReceiver.BindToAnonymousPort(IPAddress.Loopback);
                var firstReceiverEndpoint = new IPEndPoint(IPAddress.Loopback, firstReceiverPort);

                int secondReceiverPort = secondReceiver.BindToAnonymousPort(IPAddress.Loopback);
                var secondReceiverEndpoint = new IPEndPoint(IPAddress.Loopback, secondReceiverPort);

                sender.SendTo(new byte[1], SocketFlags.None, firstReceiverEndpoint);
                sender.SendTo(new byte[1], SocketFlags.None, secondReceiverEndpoint);

                var sw = Stopwatch.StartNew();
                Assert.True(SpinWait.SpinUntil(() =>
                {
                    var list = new List<Socket> { firstReceiver, secondReceiver };
                    Socket.Select(list, null, null, Math.Max((int)(SelectSuccessTimeoutMicroseconds - (sw.Elapsed.TotalSeconds * 1000000)), 0));
                    Assert.True(list.Count <= 2);
                    if (list.Count == 2)
                    {
                        Assert.Equal(firstReceiver, list[0]);
                        Assert.Equal(secondReceiver, list[1]);
                        return true;
                    }
                    return false;
                }, SelectSuccessTimeoutMicroseconds / 1000), "Failed to select both items within allotted time");
            }
        }

        [Fact]
        public void SelectRead_Multiple_Timeout()
        {
            using (var firstReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var secondReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                firstReceiver.BindToAnonymousPort(IPAddress.Loopback);
                secondReceiver.BindToAnonymousPort(IPAddress.Loopback);

                var list = new List<Socket> { firstReceiver, secondReceiver };
                Socket.Select(list, null, null, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void SelectRead_Multiple_Mixed()
        {
            using (var firstReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var secondReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                firstReceiver.BindToAnonymousPort(IPAddress.Loopback);

                int secondReceiverPort = secondReceiver.BindToAnonymousPort(IPAddress.Loopback);
                var secondReceiverEndpoint = new IPEndPoint(IPAddress.Loopback, secondReceiverPort);

                sender.SendTo(new byte[1], SocketFlags.None, secondReceiverEndpoint);

                var list = new List<Socket> { firstReceiver, secondReceiver };
                Socket.Select(list, null, null, SelectSuccessTimeoutMicroseconds);

                Assert.Equal(1, list.Count);
                Assert.Equal(secondReceiver, list[0]);
            }
        }

        [Fact]
        public void Select_Write_NotASocket_Throws()
        {
            var list = new List<object> { new object() };
            AssertExtensions.Throws<ArgumentException>("socketList", () => Socket.Select(null, list, null, SelectSuccessTimeoutMicroseconds));
        }

        [Fact]
        public void SelectWrite_Single_Success()
        {
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                var list = new List<Socket> { sender };
                Socket.Select(null, list, null, SelectSuccessTimeoutMicroseconds);

                Assert.Equal(1, list.Count);
                Assert.Equal(sender, list[0]);
            }
        }

        [Fact]
        public void SelectWrite_Single_Timeout()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);
                listener.AcceptAsync();

                var list = new List<Socket> { listener };
                Socket.Select(null, list, null, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void SelectWrite_Multiple_Success()
        {
            using (var firstSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var secondSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                var list = new List<Socket> { firstSender, secondSender };
                Socket.Select(null, list, null, SelectSuccessTimeoutMicroseconds);

                Assert.Equal(2, list.Count);
                Assert.Equal(firstSender, list[0]);
                Assert.Equal(secondSender, list[1]);
            }
        }

        [Fact]
        public void SelectWrite_Multiple_Timeout()
        {
            using (var firstListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var secondListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                firstListener.BindToAnonymousPort(IPAddress.Loopback);
                firstListener.Listen(1);
                firstListener.AcceptAsync();

                secondListener.BindToAnonymousPort(IPAddress.Loopback);
                secondListener.Listen(1);
                secondListener.AcceptAsync();

                var list = new List<Socket> { firstListener, secondListener };
                Socket.Select(null, list, null, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void SelectWrite_Multiple_Mixed()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);
                listener.AcceptAsync();

                var list = new List<Socket> { listener, sender };
                Socket.Select(null, list, null, SelectSuccessTimeoutMicroseconds);

                Assert.Equal(1, list.Count);
                Assert.Equal(sender, list[0]);
            }
        }

        [Fact]
        public void Select_Error_NotASocket_Throws()
        {
            var list = new List<object> { new object() };
            AssertExtensions.Throws<ArgumentException>("socketList", () => Socket.Select(null, null, list, SelectSuccessTimeoutMicroseconds));
        }

        [Fact]
        public void SelectError_Single_Timeout()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.BindToAnonymousPort(IPAddress.Loopback);

                var list = new List<Socket> { receiver };
                Socket.Select(null, null, list, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void SelectError_Multiple_Timeout()
        {
            using (var firstReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var secondReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                firstReceiver.BindToAnonymousPort(IPAddress.Loopback);
                secondReceiver.BindToAnonymousPort(IPAddress.Loopback);

                var list = new List<Socket> { firstReceiver, secondReceiver };
                Socket.Select(null, null, list, SelectTimeout);

                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void PollRead_Single_Success()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int receiverPort = receiver.BindToAnonymousPort(IPAddress.Loopback);
                var receiverEndpoint = new IPEndPoint(IPAddress.Loopback, receiverPort);

                sender.SendTo(new byte[1], SocketFlags.None, receiverEndpoint);

                Assert.True(receiver.Poll(SelectSuccessTimeoutMicroseconds, SelectMode.SelectRead));
            }
        }

        [Fact]
        public void PollRead_Single_Timeout()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.BindToAnonymousPort(IPAddress.Loopback);

                Assert.False(receiver.Poll(SelectTimeout, SelectMode.SelectRead));
            }
        }

        [Fact]
        public void PollWrite_Single_Success()
        {
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.True(sender.Poll(SelectSuccessTimeoutMicroseconds, SelectMode.SelectWrite));
            }
        }

        [Fact]
        public void PollWrite_Single_Timeout()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);
                listener.AcceptAsync();

                Assert.False(listener.Poll(SelectTimeout, SelectMode.SelectWrite));
            }
        }

        [Fact]
        public void PollError_Single_Timeout()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.BindToAnonymousPort(IPAddress.Loopback);

                Assert.False(receiver.Poll(SelectTimeout, SelectMode.SelectError));
            }
        }
    }

    public class SelectAndPollZeroTimeoutTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZeroTimeout_ReportsReadiness_AndAsyncIOStillWorks(bool useSelect)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task<Socket> acceptTask = listener.AcceptAsync();
            await client.ConnectAsync(listener.LocalEndPoint!);
            using Socket server = await acceptTask;

            Assert.False(IsReady(server, SelectMode.SelectRead, useSelect), "readable before send");
            Assert.True(IsReady(client, SelectMode.SelectWrite, useSelect), "writable");

            await client.SendAsync(new byte[] { 42 }, SocketFlags.None);

            Stopwatch sw = Stopwatch.StartNew();
            while (!IsReady(server, SelectMode.SelectRead, useSelect))
            {
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), "Socket did not become readable.");
                await Task.Delay(10);
            }

            byte[] buffer = new byte[1];
            Assert.Equal(1, await server.ReceiveAsync(buffer, SocketFlags.None));
            Assert.Equal(42, buffer[0]);
            if (!OperatingSystem.IsWasi())
            {
                // On WASI, readiness can stay set after the data is drained until a read would block.
                Assert.False(IsReady(server, SelectMode.SelectRead, useSelect), "readable after drain");
            }

            Task<int> pendingReceive = server.ReceiveAsync(buffer, SocketFlags.None);
            Assert.False(IsReady(server, SelectMode.SelectRead, useSelect), "readable with pending receive");
            await client.SendAsync(new byte[] { 43 }, SocketFlags.None);
            Assert.Equal(1, await pendingReceive.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.Equal(43, buffer[0]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void NonZeroTimeout_SingleThreaded_Throws()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            Assert.Throws<PlatformNotSupportedException>(() => socket.Poll(1, SelectMode.SelectRead));
            Assert.Throws<PlatformNotSupportedException>(() => socket.Poll(-1, SelectMode.SelectRead));
            Assert.Throws<PlatformNotSupportedException>(() => Socket.Select(new List<Socket> { socket }, null, null, 1));
            Assert.Throws<PlatformNotSupportedException>(() => Socket.Select(new List<Socket> { socket }, null, null, -1));
        }

        private static bool IsReady(Socket socket, SelectMode mode, bool useSelect)
        {
            if (!useSelect)
            {
                return socket.Poll(0, mode);
            }

            var list = new List<Socket> { socket };
            Socket.Select(
                mode == SelectMode.SelectRead ? list : null,
                mode == SelectMode.SelectWrite ? list : null,
                mode == SelectMode.SelectError ? list : null,
                0);
            return list.Count == 1;
        }
    }
}
