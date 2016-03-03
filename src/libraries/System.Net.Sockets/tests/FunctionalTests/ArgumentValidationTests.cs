// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using Xunit;
using System.Threading;

namespace System.Net.Sockets.Tests
{
    // TODO:
    //
    // - Connect(EndPoint):
    //   - disconnected socket
    // - Accept(EndPoint):
    //   - disconnected socket
    public class ArgumentValidation
    {
        // This type is used to test Socket.Select's argument validation.
        private sealed class LargeList : IList
        {
            private const int MaxSelect = 65536;

            public int Count { get { return MaxSelect + 1; } }
            public bool IsFixedSize { get { return true; } }
            public bool IsReadOnly { get { return true; } }
            public bool IsSynchronized { get { return false; } }
            public object SyncRoot { get { return null; } }

            public object this[int index]
            {
                get { return null; }
                set { }
            }

            public int Add(object value) { return -1; }
            public void Clear() { }
            public bool Contains(object value) { return false; }
            public void CopyTo(Array array, int index) { }
            public IEnumerator GetEnumerator() { return null; }
            public int IndexOf(object value) { return -1; }
            public void Insert(int index, object value) { }
            public void Remove(object value) { }
            public void RemoveAt(int index) { }
        }

        private readonly static byte[] s_buffer = new byte[1];
        private readonly static IList<ArraySegment<byte>> s_buffers = new List<ArraySegment<byte>> { new ArraySegment<byte>(s_buffer) };
        private readonly static SocketAsyncEventArgs s_eventArgs = new SocketAsyncEventArgs();
        private readonly static Socket s_ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly static Socket s_ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        private static Socket GetSocket(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            Debug.Assert(addressFamily == AddressFamily.InterNetwork || addressFamily == AddressFamily.InterNetworkV6);
            return addressFamily == AddressFamily.InterNetwork ? s_ipv4Socket : s_ipv6Socket;
        }

        [Fact]
        public void SetExclusiveAddressUse_BoundSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Assert.Throws<InvalidOperationException>(() =>
                {
                    socket.ExclusiveAddressUse = true;
                });
            }
        }

        [Fact]
        public void SetReceiveBufferSize_Negative_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().ReceiveBufferSize = -1;
            });
        }

        [Fact]
        public void SetSendBufferSize_Negative_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().SendBufferSize = -1;
            });
        }

        [Fact]
        public void SetReceiveTimeout_LessThanNegativeOne_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().ReceiveTimeout = int.MinValue;
            });
        }

        [Fact]
        public void SetSendTimeout_LessThanNegativeOne_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().SendTimeout = int.MinValue;
            });
        }

        [Fact]
        public void SetTtl_OutOfRange_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().Ttl = -1;
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                GetSocket().Ttl = 256;
            });
        }

        [Fact]
        public void DontFragment_IPv6_Throws_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetworkV6).DontFragment);
        }

        [Fact]
        public void SetDontFragment_Throws_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                GetSocket(AddressFamily.InterNetworkV6).DontFragment = true;
            });
        }

        [Fact]
        public void Bind_Throws_NullEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().Bind(null));
        }

        [Fact]
        public void Connect_EndPoint_NullEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().Connect(null));
        }

        [Fact]
        public void Connect_EndPoint_ListeningSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => socket.Connect(new IPEndPoint(IPAddress.Loopback, 1)));
            }
        }

        [Fact]
        public void Connect_IPAddress_NullIPAddress_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().Connect((IPAddress)null, 1));
        }

        [Fact]
        public void Connect_IPAddress_InvalidPort_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect(IPAddress.Loopback, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect(IPAddress.Loopback, 65536));
        }

        [Fact]
        public void Connect_IPAddress_InvalidAddressFamily_Throws_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetwork).Connect(IPAddress.IPv6Loopback, 1));
            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetworkV6).Connect(IPAddress.Loopback, 1));
        }

        [Fact]
        public void Connect_Host_NullHost_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().Connect((string)null, 1));
        }

        [Fact]
        public void Connect_Host_InvalidPort_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect("localhost", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect("localhost", 65536));
        }

        [Fact]
        public void Connect_IPAddresses_NullArray_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().Connect((IPAddress[])null, 1));
        }

        [Fact]
        public void Connect_IPAddresses_EmptyArray_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().Connect(new IPAddress[0], 1));
        }

        [Fact]
        public void Connect_IPAddresses_InvalidPort_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect(new[] { IPAddress.Loopback }, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Connect(new[] { IPAddress.Loopback }, 65536));
        }

        [Fact]
        public void Accept_NotBound_Throws_InvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => GetSocket().Accept());
        }

        [Fact]
        public void Accept_NotListening_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Assert.Throws<InvalidOperationException>(() => socket.Accept());
            }
        }

        [Fact]
        public void Send_Buffer_NullBuffer_Throws_ArgumentNull()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentNullException>(() => GetSocket().Send(null, 0, 0, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Send_Buffer_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Send(s_buffer, -1, 0, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Send(s_buffer, s_buffer.Length + 1, 0, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Send_Buffer_InvalidCount_Throws_ArgumentOutOfRange()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Send(s_buffer, 0, -1, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Send(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Send(s_buffer, s_buffer.Length, 1, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Send_Buffers_NullBuffers_Throws_ArgumentNull()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentNullException>(() => GetSocket().Send(null, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Send_Buffers_EmptyBuffers_Throws_Argument()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentException>(() => GetSocket().Send(new List<ArraySegment<byte>>(), SocketFlags.None, out errorCode));
        }

        [Fact]
        public void SendTo_NullBuffer_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendTo(null, 0, 0, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 1)));
        }

        [Fact]
        public void SendTo_NullEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendTo(s_buffer, 0, 0, SocketFlags.None, null));
        }

        [Fact]
        public void SendTo_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().SendTo(s_buffer, -1, s_buffer.Length, SocketFlags.None, endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().SendTo(s_buffer, s_buffer.Length + 1, s_buffer.Length, SocketFlags.None, endpoint));
        }

        [Fact]
        public void SendTo_InvalidSize_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().SendTo(s_buffer, 0, -1, SocketFlags.None, endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().SendTo(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().SendTo(s_buffer, s_buffer.Length, 1, SocketFlags.None, endpoint));
        }

        [Fact]
        public void Receive_Buffer_NullBuffer_Throws_ArgumentNull()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentNullException>(() => GetSocket().Receive(null, 0, 0, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Receive_Buffer_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Receive(s_buffer, -1, 0, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Receive(s_buffer, s_buffer.Length + 1, 0, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Receive_Buffer_InvalidCount_Throws_ArgumentOutOfRange()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Receive(s_buffer, 0, -1, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Receive(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, out errorCode));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().Receive(s_buffer, s_buffer.Length, 1, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Receive_Buffers_NullBuffers_Throws_ArgumentNull()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentNullException>(() => GetSocket().Receive(null, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Receive_Buffers_EmptyBuffers_Throws_Argument()
        {
            SocketError errorCode;
            Assert.Throws<ArgumentException>(() => GetSocket().Receive(new List<ArraySegment<byte>>(), SocketFlags.None, out errorCode));
        }

        [Fact]
        public void ReceiveFrom_NullBuffer_Throws_ArgumentNull()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveFrom(null, 0, 0, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveFrom_NullEndPoint_Throws_ArgumentNull()
        {
            EndPoint endpoint = null;
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveFrom_AddressFamily_Throws_Argument()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            Assert.Throws<ArgumentException>(() => GetSocket(AddressFamily.InterNetwork).ReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveFrom_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveFrom(s_buffer, -1, s_buffer.Length, SocketFlags.None, ref endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveFrom(s_buffer, s_buffer.Length + 1, s_buffer.Length, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveFrom_InvalidSize_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveFrom(s_buffer, 0, -1, SocketFlags.None, ref endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveFrom(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, ref endpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveFrom(s_buffer, s_buffer.Length, 1, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveFrom_NotBound_Throws_InvalidOperation()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<InvalidOperationException>(() => GetSocket().ReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint));
        }

        [Fact]
        public void ReceiveMessageFrom_NullBuffer_Throws_ArgumentNull()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveMessageFrom(null, 0, 0, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void ReceiveMessageFrom_NullEndPoint_Throws_ArgumentNull()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = null;
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveMessageFrom(s_buffer, 0, 0, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void ReceiveMessageFrom_AddressFamily_Throws_Argument()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentException>(() => GetSocket(AddressFamily.InterNetwork).ReceiveMessageFrom(s_buffer, 0, 0, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void ReceiveMessageFrom_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveMessageFrom(s_buffer, -1, s_buffer.Length, ref flags, ref remote, out packetInfo));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveMessageFrom(s_buffer, s_buffer.Length + 1, s_buffer.Length, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void ReceiveMessageFrom_InvalidSize_Throws_ArgumentOutOfRange()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveMessageFrom(s_buffer, 0, -1, ref flags, ref remote, out packetInfo));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveMessageFrom(s_buffer, 0, s_buffer.Length + 1, ref flags, ref remote, out packetInfo));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().ReceiveMessageFrom(s_buffer, s_buffer.Length, 1, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void ReceiveMessageFrom_NotBound_Throws_InvalidOperation()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<InvalidOperationException>(() => GetSocket().ReceiveMessageFrom(s_buffer, 0, 0, ref flags, ref remote, out packetInfo));
        }
        
        [Fact]
        public void SetSocketOption_Object_ObjectNull_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, (object)null));
        }

        [Fact]
        public void SetSocketOption_Linger_NotLingerOption_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new object()));
        }

        [Fact]
        public void SetSocketOption_Linger_InvalidLingerTime_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, -1)));
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, (int)ushort.MaxValue + 1)));
        }

        [Fact]
        public void SetSocketOption_IPMulticast_NotIPMulticastOption_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new object()));
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new object()));
        }

        [Fact]
        public void SetSocketOption_IPv6Multicast_NotIPMulticastOption_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new object()));
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new object()));
        }

        [Fact]
        public void SetSocketOption_Object_InvalidOptionName_Throws_Argument()
        {
            Assert.Throws<ArgumentException>(() => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, new object()));
        }

        // TODO: Select
        [Fact]
        public void Select_NullOrEmptyLists_Throws_ArgumentNull()
        {
            var emptyList = new List<Socket>();
            
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, null, null, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, null, null, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, emptyList, null, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, emptyList, null, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, null, emptyList, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, null, emptyList, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, emptyList, emptyList, -1));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, emptyList, emptyList, -1));
        }

        [Fact]
        public void Select_LargeList_Throws_ArgumentOutOfRange()
        {
            var largeList = new LargeList();

            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(largeList, null, null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, largeList, null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, null, largeList, -1));
        }

        [Fact]
        public void AcceptAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().AcceptAsync(null));
        }

        [Fact]
        public void AcceptAsync_BufferList_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                BufferList = s_buffers
            };

            Assert.Throws<ArgumentException>(() => GetSocket().AcceptAsync(eventArgs));
        }

        [Fact]
        public void AcceptAsync_NotBound_Throws_InvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => GetSocket().AcceptAsync(s_eventArgs));
        }

        [Fact]
        public void AcceptAsync_NotListening_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Assert.Throws<InvalidOperationException>(() => socket.AcceptAsync(s_eventArgs));
            }
        }

        [Fact]
        public void ConnectAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ConnectAsync(null));
        }

        [Fact]
        public void ConnectAsync_BufferList_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                BufferList = s_buffers
            };

            Assert.Throws<ArgumentException>(() => GetSocket().ConnectAsync(eventArgs));
        }

        [Fact]
        public void ConnectAsync_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ConnectAsync(s_eventArgs));
        }

        [Fact]
        public void ConnectAsync_ListeningSocket_Throws_InvalidOperation()
        {
            var eventArgs = new SocketAsyncEventArgs {
                RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 1)
            };

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => socket.ConnectAsync(eventArgs));
            }
        }

        [Fact]
        public void ConnectAsync_AddressFamily_Throws_NotSupported()
        {
            var eventArgs = new SocketAsyncEventArgs {
                RemoteEndPoint = new DnsEndPoint("localhost", 1, AddressFamily.InterNetworkV6)
            };

            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetwork).ConnectAsync(eventArgs));

            eventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetwork).ConnectAsync(eventArgs));
        }

        [Fact]
        public void ConnectAsync_Static_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, null));
        }

        [Fact]
        public void ConnectAsync_Static_BufferList_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                BufferList = s_buffers
            };

            Assert.Throws<ArgumentException>(() => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, eventArgs));
        }

        [Fact]
        public void ConnectAsync_Static_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, s_eventArgs));
        }

        [Fact]
        public void ReceiveAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveAsync(null));
        }

        [Fact]
        public void ReceiveFromAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveFromAsync(null));
        }

        [Fact]
        public void ReceiveFromAsync_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveFromAsync(s_eventArgs));
        }

        [Fact]
        public void ReceiveFromAsync_AddressFamily_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 1)
            };

            Assert.Throws<ArgumentException>(() => GetSocket(AddressFamily.InterNetwork).ReceiveFromAsync(eventArgs));
        }

        [Fact]
        public void ReceiveMessageFromAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveMessageFromAsync(null));
        }

        [Fact]
        public void ReceiveMessageFromAsync_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveMessageFromAsync(s_eventArgs));
        }

        [Fact]
        public void ReceiveMessageFromAsync_AddressFamily_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 1)
            };

            Assert.Throws<ArgumentException>(() => GetSocket(AddressFamily.InterNetwork).ReceiveMessageFromAsync(eventArgs));
        }

        [Fact]
        public void SendAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendAsync(null));
        }

        [Fact]
        public void SendPacketsAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendPacketsAsync(null));
        }

        [Fact]
        public void SendPacketsAsync_NullSendPacketsElements_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendPacketsAsync(s_eventArgs));
        }

        [Fact]
        public void SendPacketsAsync_NotConnected_Throws_NotSupported()
        {
            var eventArgs = new SocketAsyncEventArgs {
                SendPacketsElements = new SendPacketsElement[0]
            };

            Assert.Throws<NotSupportedException>(() => GetSocket().SendPacketsAsync(eventArgs));
        }

        [Fact]
        public void SendToAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendToAsync(null));
        }

        [Fact]
        public void SendToAsync_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendToAsync(s_eventArgs));
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_Connect_DnsEndPoint_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect(new DnsEndPoint("localhost", 12345)));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_Connect_StringHost_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect("localhost", 12345));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_Connect_MultipleAddresses_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect(new[] { IPAddress.Loopback }, 12345));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_ConnectAsync_DnsEndPoint_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => { s.ConnectAsync(new DnsEndPoint("localhost", 12345)); });
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_ConnectAsync_StringHost_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => { s.ConnectAsync("localhost", 12345); });
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Socket_ConnectAsync_MultipleAddresses_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => { s.ConnectAsync(new[] { IPAddress.Loopback }, 12345); });
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Connect_ConnectTwice_NotSupported()
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                //
                // Connect once, to an invalid address, expecting failure
                //
                EndPoint ep = new IPEndPoint(IPAddress.Broadcast, 1234);
                Assert.Throws<SocketException>(() => client.Connect(ep));

                //
                // Connect again, expecting PlatformNotSupportedException
                //
                Assert.Throws<PlatformNotSupportedException>(() => client.Connect(ep));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void ConnectAsync_ConnectTwice_NotSupported()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                //
                // Connect once, to an invalid address, expecting failure
                //
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 1234);
                args.Completed += delegate
                {
                    completed.Set();
                };

                if (client.ConnectAsync(args))
                {
                    Assert.True(completed.WaitOne(5000), "IPv4: Timed out while waiting for connection");
                }

                Assert.NotEqual(SocketError.Success, args.SocketError);

                //
                // Connect again, expecting PlatformNotSupportedException
                //
                Assert.Throws<PlatformNotSupportedException>(() => client.ConnectAsync(args));
            }
        }
    }
}
