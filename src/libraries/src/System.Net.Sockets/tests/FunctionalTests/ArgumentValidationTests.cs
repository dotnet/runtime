// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
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

        private static readonly byte[] s_buffer = new byte[1];
        private static readonly IList<ArraySegment<byte>> s_buffers = new List<ArraySegment<byte>> { new ArraySegment<byte>(s_buffer) };
        private static readonly SocketAsyncEventArgs s_eventArgs = new SocketAsyncEventArgs();
        private static readonly Socket s_ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly Socket s_ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        private static void TheAsyncCallback(IAsyncResult ar)
        {
        }

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
            AssertExtensions.Throws<ArgumentException>("addresses", () => GetSocket().Connect(new IPAddress[0], 1));
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
            Assert.Throws<ArgumentNullException>(() => GetSocket().Send((IList<ArraySegment<byte>>)null, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Send_Buffers_EmptyBuffers_Throws_Argument()
        {
            SocketError errorCode;
            AssertExtensions.Throws<ArgumentException>("buffers", () => GetSocket().Send(new List<ArraySegment<byte>>(), SocketFlags.None, out errorCode));
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
            Assert.Throws<ArgumentNullException>(() => GetSocket().Receive((IList<ArraySegment<byte>>)null, SocketFlags.None, out errorCode));
        }

        [Fact]
        public void Receive_Buffers_EmptyBuffers_Throws_Argument()
        {
            SocketError errorCode;
            AssertExtensions.Throws<ArgumentException>("buffers", () => GetSocket().Receive(new List<ArraySegment<byte>>(), SocketFlags.None, out errorCode));
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
            AssertExtensions.Throws<ArgumentException>("remoteEP", () => GetSocket(AddressFamily.InterNetwork).ReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint));
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

            AssertExtensions.Throws<ArgumentException>("remoteEP", () => GetSocket(AddressFamily.InterNetwork).ReceiveMessageFrom(s_buffer, 0, 0, ref flags, ref remote, out packetInfo));
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
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new object()));
        }

        [Fact]
        public void SetSocketOption_Linger_InvalidLingerTime_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("optionValue.LingerTime", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, -1)));
            AssertExtensions.Throws<ArgumentException>("optionValue.LingerTime", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, (int)ushort.MaxValue + 1)));
        }

        [Fact]
        public void SetSocketOption_IPMulticast_NotIPMulticastOption_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new object()));
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new object()));
        }

        [Fact]
        public void SetSocketOption_IPv6Multicast_NotIPMulticastOption_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new object()));
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new object()));
        }

        [Fact]
        public void SetSocketOption_Object_InvalidOptionName_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, new object()));
        }

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

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in AcceptAsync that dereferences null SAEA argument")]
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

            AssertExtensions.Throws<ArgumentException>("BufferList", () => GetSocket().AcceptAsync(eventArgs));
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in ReceiveAsync that dereferences null SAEA argument")]
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

            AssertExtensions.Throws<ArgumentException>("BufferList", () => GetSocket().ConnectAsync(eventArgs));
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in ConnectAsync that dereferences null SAEA argument")]
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

            AssertExtensions.Throws<ArgumentException>("BufferList", () => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, eventArgs));
        }

        [Fact]
        public void ConnectAsync_Static_NullRemoteEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, s_eventArgs));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in ReceiveAsync that dereferences null SAEA argument")]
        [Fact]
        public void ReceiveAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveAsync(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in ReceiveFromAsync that dereferences null SAEA argument")]
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

            AssertExtensions.Throws<ArgumentException>("RemoteEndPoint", () => GetSocket(AddressFamily.InterNetwork).ReceiveFromAsync(eventArgs));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in ReceiveMessageFromAsync that dereferences null SAEA argument")]
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

            AssertExtensions.Throws<ArgumentException>("RemoteEndPoint", () => GetSocket(AddressFamily.InterNetwork).ReceiveMessageFromAsync(eventArgs));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendAsync that dereferences null SAEA argument")]
        [Fact]
        public void SendAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendAsync(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendPacketsAsync that dereferences null SAEA argument")]
        [Fact]
        public void SendPacketsAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendPacketsAsync(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendPacketsAsync that dereferences null SAEA.SendPacketsElements")]
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendToAsync that dereferences null SAEA argument")]
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_DnsEndPoint_ExposedHandle_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IntPtr ignored = s.Handle;
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect(new DnsEndPoint("localhost", 12345)));
            }
        }

        [Fact]
        public async Task Socket_Connect_DnsEndPointWithIPAddressString_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                host.Listen(1);
                Task accept = host.AcceptAsync();

                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    s.Connect(new DnsEndPoint(IPAddress.Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port));
                }

                await accept;
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_StringHost_ExposedHandle_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IntPtr ignored = s.Handle;
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect("localhost", 12345));
            }
        }

        [Fact]
        public async Task Socket_Connect_IPv4AddressAsStringHost_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                host.Listen(1);
                Task accept = host.AcceptAsync();

                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    s.Connect(IPAddress.Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port);
                }

                await accept;
            }
        }

        [Fact]
        public async Task Socket_Connect_IPv6AddressAsStringHost_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                host.Listen(1);
                Task accept = host.AcceptAsync();

                using (Socket s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    s.Connect(IPAddress.IPv6Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port);
                }

                await accept;
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_MultipleAddresses_ExposedHandle_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IntPtr ignored = s.Handle;
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect(new[] { IPAddress.Loopback }, 12345));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_ConnectAsync_DnsEndPoint_ExposedHandle_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IntPtr ignored = s.Handle;
                Assert.Throws<PlatformNotSupportedException>(() => { s.ConnectAsync(new DnsEndPoint("localhost", 12345)); });
            }
        }

        [Fact]
        public async Task Socket_ConnectAsync_DnsEndPointWithIPAddressString_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                host.Listen(1);

                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await Task.WhenAll(
                        host.AcceptAsync(),
                        s.ConnectAsync(new DnsEndPoint(IPAddress.Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port)));
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_ConnectAsync_StringHost_ExposedHandle_NotSupported()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IntPtr ignored = s.Handle;
                Assert.Throws<PlatformNotSupportedException>(() => { s.ConnectAsync("localhost", 12345); });
            }
        }

        [Fact]
        public async Task Socket_ConnectAsync_IPv4AddressAsStringHost_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                host.Listen(1);

                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await Task.WhenAll(
                        host.AcceptAsync(),
                        s.ConnectAsync(IPAddress.Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port));
                }
            }
        }

        [Fact]
        public async Task Socket_ConnectAsync_IPv6AddressAsStringHost_Supported()
        {
            using (Socket host = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                host.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                host.Listen(1);

                using (Socket s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    await Task.WhenAll(
                        host.AcceptAsync(),
                        s.ConnectAsync(IPAddress.IPv6Loopback.ToString(), ((IPEndPoint)host.LocalEndPoint).Port));
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        [InlineData(0)]
        [InlineData(1)]
        public void Connect_ConnectTwice_NotSupported(int invalidatingAction)
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                switch (invalidatingAction)
                {
                    case 0:
                        IntPtr handle = client.Handle; // exposing the underlying handle
                        break;
                    case 1:
                        client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.Debug, 1); // untracked socket option
                        break;
                }

                //
                // Connect once, to an invalid address, expecting failure
                //
                EndPoint ep = new IPEndPoint(IPAddress.Broadcast, 1234);
                Assert.ThrowsAny<SocketException>(() => client.Connect(ep));

                //
                // Connect again, expecting PlatformNotSupportedException
                //
                Assert.Throws<PlatformNotSupportedException>(() => client.Connect(ep));
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        [InlineData(0)]
        [InlineData(1)]
        public void ConnectAsync_ConnectTwice_NotSupported(int invalidatingAction)
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                switch (invalidatingAction)
                {
                    case 0:
                        IntPtr handle = client.Handle; // exposing the underlying handle
                        break;
                    case 1:
                        client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.Debug, 1); // untracked socket option
                        break;
                }

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

        [Fact]
        public void BeginAccept_NotBound_Throws_InvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => GetSocket().BeginAccept(TheAsyncCallback, null));
            Assert.Throws<InvalidOperationException>(() => { GetSocket().AcceptAsync(); });
        }

        [Fact]
        public void BeginAccept_NotListening_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                Assert.Throws<InvalidOperationException>(() => socket.BeginAccept(TheAsyncCallback, null));
                Assert.Throws<InvalidOperationException>(() => { socket.AcceptAsync(); });
            }
        }

        [Fact]
        public void EndAccept_NullAsyncResult_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndAccept(null));
        }

        [Fact]
        public void BeginConnect_EndPoint_NullEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginConnect((EndPoint)null, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ConnectAsync((EndPoint)null); });
        }

        [Fact]
        public void BeginConnect_EndPoint_ListeningSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => socket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 1), TheAsyncCallback, null));
                Assert.Throws<InvalidOperationException>(() => { socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 1)); });
            }
        }

        [Fact]
        public void BeginConnect_EndPoint_AddressFamily_Throws_NotSupported()
        {
            // Unlike other tests that reuse a static Socket instance, this test avoids doing so
            // to work around a behavior of .NET 4.7.2. See https://github.com/dotnet/corefx/issues/29481
            // for more details.

            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<NotSupportedException>(() => s.BeginConnect(
                    new DnsEndPoint("localhost", 1, AddressFamily.InterNetworkV6),
                    TheAsyncCallback, null));
            }

            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<NotSupportedException>(() => { s.ConnectAsync(
                    new DnsEndPoint("localhost", 1, AddressFamily.InterNetworkV6));
                });
            }
        }

        [Fact]
        public void BeginConnect_Host_NullHost_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginConnect((string)null, 1, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ConnectAsync((string)null, 1); });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void BeginConnect_Host_InvalidPort_Throws_ArgumentOutOfRange(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginConnect("localhost", port, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ConnectAsync("localhost", port); });
        }

        [Fact]
        public void BeginConnect_Host_ListeningSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => socket.BeginConnect("localhost", 1, TheAsyncCallback, null));
                Assert.Throws<InvalidOperationException>(() => { socket.ConnectAsync("localhost", 1); });
            }
        }

        [Fact]
        public void BeginConnect_IPAddress_NullIPAddress_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginConnect((IPAddress)null, 1, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ConnectAsync((IPAddress)null, 1); });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void BeginConnect_IPAddress_InvalidPort_Throws_ArgumentOutOfRange(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginConnect(IPAddress.Loopback, port, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ConnectAsync(IPAddress.Loopback, 65536); });
        }

        [Fact]
        public void BeginConnect_IPAddress_AddressFamily_Throws_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => GetSocket(AddressFamily.InterNetwork).BeginConnect(IPAddress.IPv6Loopback, 1, TheAsyncCallback, null));
            Assert.Throws<NotSupportedException>(() => { GetSocket(AddressFamily.InterNetwork).ConnectAsync(IPAddress.IPv6Loopback, 1); });
        }

        [Fact]
        public void BeginConnect_IPAddresses_NullIPAddresses_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginConnect((IPAddress[])null, 1, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ConnectAsync((IPAddress[])null, 1); });
        }

        [Fact]
        public void BeginConnect_IPAddresses_EmptyIPAddresses_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("addresses", () => GetSocket().BeginConnect(new IPAddress[0], 1, TheAsyncCallback, null));
            AssertExtensions.Throws<ArgumentException>("addresses", () => { GetSocket().ConnectAsync(new IPAddress[0], 1); });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void BeginConnect_IPAddresses_InvalidPort_Throws_ArgumentOutOfRange(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginConnect(new[] { IPAddress.Loopback }, port, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ConnectAsync(new[] { IPAddress.Loopback }, port); });
        }

        [Fact]
        public void BeginConnect_IPAddresses_ListeningSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => socket.BeginConnect(new[] { IPAddress.Loopback }, 1, TheAsyncCallback, null));
            }

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                Assert.Throws<InvalidOperationException>(() => { socket.ConnectAsync(new[] { IPAddress.Loopback }, 1); });
            }
        }

        [Fact]
        public void EndConnect_NullAsyncResult_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndConnect(null));
        }

        [Fact]
        public void EndConnect_UnrelatedAsyncResult_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => GetSocket().EndConnect(Task.CompletedTask));
        }

        [Fact]
        public void BeginSend_Buffer_NullBuffer_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginSend(null, 0, 0, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(null, 0, 0), SocketFlags.None); });
        }

        [Fact]
        public void BeginSend_Buffer_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSend(s_buffer, -1, 0, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSend(s_buffer, s_buffer.Length + 1, 0, SocketFlags.None, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(s_buffer, -1, 0), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length + 1, 0), SocketFlags.None); });
        }

        [Fact]
        public void BeginSend_Buffer_InvalidCount_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSend(s_buffer, 0, -1, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSend(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSend(s_buffer, s_buffer.Length, 1, SocketFlags.None, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(s_buffer, 0, -1), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(s_buffer, 0, s_buffer.Length + 1), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length, 1), SocketFlags.None); });
        }

        [Fact]
        public void BeginSend_Buffers_NullBuffers_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginSend(null, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().SendAsync((IList<ArraySegment<byte>>)null, SocketFlags.None); });
        }

        [Fact]
        public void BeginSend_Buffers_EmptyBuffers_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("buffers", () => GetSocket().BeginSend(new List<ArraySegment<byte>>(), SocketFlags.None, TheAsyncCallback, null));
            AssertExtensions.Throws<ArgumentException>("buffers", () => { GetSocket().SendAsync(new List<ArraySegment<byte>>(), SocketFlags.None); });
        }

        [Fact]
        public void EndSend_NullAsyncResult_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndSend(null));
        }

        [Fact]
        public void EndSend_UnrelatedAsyncResult_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => GetSocket().EndSend(Task.CompletedTask));
        }

        [Fact]
        public void BeginSendTo_NullBuffer_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginSendTo(null, 0, 0, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 1), TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(null, 0, 0), SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 1)); });
        }

        [Fact]
        public void BeginSendTo_NullEndPoint_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginSendTo(s_buffer, 0, 0, SocketFlags.None, null, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, null); });
        }

        [Fact]
        public void BeginSendTo_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSendTo(s_buffer, -1, s_buffer.Length, SocketFlags.None, endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSendTo(s_buffer, s_buffer.Length + 1, s_buffer.Length, SocketFlags.None, endpoint, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, -1, s_buffer.Length), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length + 1, s_buffer.Length), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginSendTo_InvalidSize_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSendTo(s_buffer, 0, -1, SocketFlags.None, endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSendTo(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginSendTo(s_buffer, s_buffer.Length, 1, SocketFlags.None, endpoint, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, 0, -1), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, 0, s_buffer.Length + 1), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().SendToAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length, 1), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void EndSendTo_NullAsyncResult_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndSendTo(null));
        }

        [Fact]
        public void EndSendto_UnrelatedAsyncResult_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => GetSocket().EndSendTo(Task.CompletedTask));
        }

        [Fact]
        public void BeginReceive_Buffer_NullBuffer_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceive(null, 0, 0, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(null, 0, 0), SocketFlags.None); });
        }

        [Fact]
        public void BeginReceive_Buffer_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceive(s_buffer, -1, 0, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceive(s_buffer, s_buffer.Length + 1, 0, SocketFlags.None, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(s_buffer, -1, 0), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length + 1, 0), SocketFlags.None); });
        }

        [Fact]
        public void BeginReceive_Buffer_InvalidCount_Throws_ArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceive(s_buffer, 0, -1, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceive(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceive(s_buffer, s_buffer.Length, 1, SocketFlags.None, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(s_buffer, 0, -1), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(s_buffer, 0, s_buffer.Length + 1), SocketFlags.None); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length, 1), SocketFlags.None); });
        }

        [Fact]
        public void BeginReceive_Buffers_NullBuffers_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceive(null, SocketFlags.None, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveAsync((IList<ArraySegment<byte>>)null, SocketFlags.None); });
        }

        [Fact]
        public void BeginReceive_Buffers_EmptyBuffers_Throws_Argument()
        {
            AssertExtensions.Throws<ArgumentException>("buffers", () => GetSocket().BeginReceive(new List<ArraySegment<byte>>(), SocketFlags.None, TheAsyncCallback, null));
            AssertExtensions.Throws<ArgumentException>("buffers", () => { GetSocket().ReceiveAsync(new List<ArraySegment<byte>>(), SocketFlags.None); });
        }

        [Fact]
        public void EndReceive_NullAsyncResult_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndReceive(null));
        }

        [Fact]
        public void BeginReceiveFrom_NullBuffer_Throws_ArgumentNull()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceiveFrom(null, 0, 0, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(null, 0, 0), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginReceiveFrom_NullEndPoint_Throws_ArgumentNull()
        {
            EndPoint endpoint = null;
            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginReceiveFrom_AddressFamily_Throws_Argument()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            AssertExtensions.Throws<ArgumentException>("remoteEP", () => GetSocket(AddressFamily.InterNetwork).BeginReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            AssertExtensions.Throws<ArgumentException>("remoteEP", () => { GetSocket(AddressFamily.InterNetwork).ReceiveFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginReceiveFrom_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveFrom(s_buffer, -1, s_buffer.Length, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveFrom(s_buffer, s_buffer.Length + 1, s_buffer.Length, SocketFlags.None, ref endpoint, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, -1, s_buffer.Length), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length + 1, s_buffer.Length), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginReceiveFrom_InvalidSize_Throws_ArgumentOutOfRange()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveFrom(s_buffer, 0, -1, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveFrom(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveFrom(s_buffer, s_buffer.Length, 1, SocketFlags.None, ref endpoint, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, 0, -1), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, 0, s_buffer.Length + 1), SocketFlags.None, endpoint); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length, 1), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void BeginReceiveFrom_NotBound_Throws_InvalidOperation()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<InvalidOperationException>(() => GetSocket().BeginReceiveFrom(s_buffer, 0, 0, SocketFlags.None, ref endpoint, TheAsyncCallback, null));
            Assert.Throws<InvalidOperationException>(() => { GetSocket().ReceiveFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, endpoint); });
        }

        [Fact]
        public void EndReceiveFrom_NullAsyncResult_Throws_ArgumentNull()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentNullException>(() => GetSocket().EndReceiveFrom(null, ref endpoint));
        }

        [Fact]
        public void BeginReceiveMessageFrom_NullBuffer_Throws_ArgumentNull()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceiveMessageFrom(null, 0, 0, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(null, 0, 0), SocketFlags.None, remote); });
        }

        [Fact]
        public void BeginReceiveMessageFrom_NullEndPoint_Throws_ArgumentNull()
        {
            EndPoint remote = null;

            Assert.Throws<ArgumentNullException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, 0, 0, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<ArgumentNullException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, remote); });
        }

        [Fact]
        public void BeginReceiveMessageFrom_AddressFamily_Throws_Argument()
        {
            EndPoint remote = new IPEndPoint(IPAddress.IPv6Loopback, 1);

            AssertExtensions.Throws<ArgumentException>("remoteEP", () => GetSocket(AddressFamily.InterNetwork).BeginReceiveMessageFrom(s_buffer, 0, 0, SocketFlags.None, ref remote, TheAsyncCallback, null));
            AssertExtensions.Throws<ArgumentException>("remoteEP", () => { GetSocket(AddressFamily.InterNetwork).ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, remote); });
        }

        [Fact]
        public void BeginReceiveMessageFrom_InvalidOffset_Throws_ArgumentOutOfRange()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, -1, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, s_buffer.Length + 1, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, -1, s_buffer.Length), SocketFlags.None, remote); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length + 1, s_buffer.Length), SocketFlags.None, remote); });
        }

        [Fact]
        public void BeginReceiveMessageFrom_InvalidSize_Throws_ArgumentOutOfRange()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, 0, -1, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, 0, s_buffer.Length + 1, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, s_buffer.Length, 1, SocketFlags.None, ref remote, TheAsyncCallback, null));

            Assert.Throws<ArgumentOutOfRangeException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, 0, -1), SocketFlags.None, remote); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, 0, s_buffer.Length + 1), SocketFlags.None, remote); });
            Assert.ThrowsAny<ArgumentException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, s_buffer.Length, 1), SocketFlags.None, remote); });
        }

        [Fact]
        public void BeginReceiveMessageFrom_NotBound_Throws_InvalidOperation()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);

            Assert.Throws<InvalidOperationException>(() => GetSocket().BeginReceiveMessageFrom(s_buffer, 0, 0, SocketFlags.None, ref remote, TheAsyncCallback, null));
            Assert.Throws<InvalidOperationException>(() => { GetSocket().ReceiveMessageFromAsync(new ArraySegment<byte>(s_buffer, 0, 0), SocketFlags.None, remote); });
        }

        [Fact]
        public void EndReceiveMessageFrom_NullEndPoint_Throws_ArgumentNull()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = null;
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentNullException>(() => GetSocket().EndReceiveMessageFrom(null, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void EndReceiveMessageFrom_AddressFamily_Throws_Argument()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.IPv6Loopback, 1);
            IPPacketInformation packetInfo;

            AssertExtensions.Throws<ArgumentException>("endPoint", () => GetSocket(AddressFamily.InterNetwork).EndReceiveMessageFrom(null, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void EndReceiveMessageFrom_NullAsyncResult_Throws_ArgumentNull()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;

            Assert.Throws<ArgumentNullException>(() => GetSocket().EndReceiveMessageFrom(null, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void CancelConnectAsync_NullEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.CancelConnectAsync(null));
        }
    }
}
