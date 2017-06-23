// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SelectAndPollTests
    {
        const int SelectTimeout = 100;
        const int SelectSuccessTimeoutMicroseconds = 5*1000*1000; // 5 seconds

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

                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1], SocketFlags.None, receiverEndpoint);
                }

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

                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1], SocketFlags.None, firstReceiverEndpoint);
                    sender.SendTo(new byte[1], SocketFlags.None, secondReceiverEndpoint);
                }

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

                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1], SocketFlags.None, secondReceiverEndpoint);
                }

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

                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1], SocketFlags.None, receiverEndpoint);
                }

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
}
