// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, -1)));
            AssertExtensions.Throws<ArgumentException>("optionValue", () => GetSocket().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, (int)ushort.MaxValue + 1)));
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
        public void Select_NullOrEmptyLists_Throws_ArgumentNull_TimeSpan()
        {
            TimeSpan nonInfinity = TimeSpan.FromMilliseconds(1);
            var emptyList = new List<Socket>();

            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, null, null, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, null, null, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, emptyList, null, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, emptyList, null, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, null, emptyList, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, null, emptyList, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(null, emptyList, emptyList, nonInfinity));
            Assert.Throws<ArgumentNullException>(() => Socket.Select(emptyList, emptyList, emptyList, nonInfinity));
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
        public void Select_LargeList_Throws_ArgumentOutOfRange_TimeSpan()
        {
            var largeList = new LargeList();

            TimeSpan infinity = Timeout.InfiniteTimeSpan;
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(largeList, null, null, infinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, largeList, null, infinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, null, largeList, infinity));

            TimeSpan negative = TimeSpan.FromMilliseconds(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(largeList, null, null, negative));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, largeList, null, negative));
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket.Select(null, null, largeList, negative));
        }

        [Fact]
        public void AcceptAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().AcceptAsync((SocketAsyncEventArgs)null));
        }

        [Fact]
        public void AcceptAsync_BufferList_Throws_Argument()
        {
            var eventArgs = new SocketAsyncEventArgs {
                BufferList = s_buffers
            };

            AssertExtensions.Throws<ArgumentException>("e", () => GetSocket().AcceptAsync(eventArgs));
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
            Assert.Throws<ArgumentNullException>(() => GetSocket().ConnectAsync((SocketAsyncEventArgs)null));
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

            AssertExtensions.Throws<ArgumentException>("e", () => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, eventArgs));
        }

        [Fact]
        public void ConnectAsync_Static_NullRemoteEndPoint_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>("e", () => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, s_eventArgs));
        }

        [Fact]
        public void ReceiveAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().ReceiveAsync((SocketAsyncEventArgs)null));
        }

        [Fact]
        public void SendAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendAsync((SocketAsyncEventArgs)null));
        }

        [Fact]
        public void SendPacketsAsync_NullAsyncEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetSocket().SendPacketsAsync(null));
        }

        [Fact]
        public void SendPacketsAsync_NullSendPacketsElements_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>("e", () => GetSocket().SendPacketsAsync(s_eventArgs));
        }

        [Fact]
        public void SendPacketsAsync_NotConnected_Throws_NotSupported()
        {
            var eventArgs = new SocketAsyncEventArgs {
                SendPacketsElements = new SendPacketsElement[0]
            };

            Assert.Throws<NotSupportedException>(() => GetSocket().SendPacketsAsync(eventArgs));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_DnsEndPoint_ExposedHandle_NotSupported(bool useSafeHandle)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (useSafeHandle)
                {
                    _ = s.SafeHandle;
                }
                else
                {
                    _ = s.Handle;
                }
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_StringHost_ExposedHandle_NotSupported(bool useSafeHandle)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (useSafeHandle)
                {
                    _ = s.SafeHandle;
                }
                else
                {
                    _ = s.Handle;
                }
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_Connect_MultipleAddresses_ExposedHandle_NotSupported(bool useSafeHandle)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (useSafeHandle)
                {
                    _ = s.SafeHandle;
                }
                else
                {
                    _ = s.Handle;
                }
                Assert.Throws<PlatformNotSupportedException>(() => s.Connect(new[] { IPAddress.Loopback }, 12345));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_ConnectAsync_DnsEndPoint_ExposedHandle_NotSupported(bool useSafeHandle)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (useSafeHandle)
                {
                    _ = s.SafeHandle;
                }
                else
                {
                    _ = s.Handle;
                }
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // API throws PNSE on Unix
        public void Socket_ConnectAsync_StringHost_ExposedHandle_NotSupported(bool useSafeHandle)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (useSafeHandle)
                {
                    _ = s.SafeHandle;
                }
                else
                {
                    _ = s.Handle;
                }
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
            // to work around a behavior of .NET 4.7.2. See https://github.com/dotnet/runtime/issues/26062
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
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task ConnectAsync_IPAddresses_InvalidPort_Throws_ArgumentOutOfRange(int port)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => GetSocket().ConnectAsync(new[] { IPAddress.Loopback }, port));
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
        }

        [Fact]
        public async Task ConnectAsync_IPAddresses_ListeningSocket_Throws_InvalidOperation()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socket.Listen(1);
                await Assert.ThrowsAsync<InvalidOperationException>(() => socket.ConnectAsync(new[] { IPAddress.Loopback }, 1));
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
        public void CancelConnectAsync_NullEventArgs_Throws_ArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Socket.CancelConnectAsync(null));
        }
    }
}
