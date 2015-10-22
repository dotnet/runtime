// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class DisposedSocket
    {
        private readonly static byte[] s_buffer = new byte[1];
        private readonly static IList<ArraySegment<byte>> s_buffers = new List<ArraySegment<byte>> { new ArraySegment<byte>(s_buffer) };

        private static Socket GetDisposedSocket(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            using (var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                return socket;
            }
        }

        private static void TheAsyncCallback(IAsyncResult ar)
        {
        }

        [Fact]
        public void BeginConnect_EndPoint_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginConnect(new IPEndPoint(IPAddress.Loopback, 1), TheAsyncCallback, null));
        }

        [Fact]
        public void BeginConnect_IPAddress_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginConnect(IPAddress.Loopback, 1, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginConnect_IPAddresses_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginConnect(new[] { IPAddress.Loopback }, 1, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginConnect_Host_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginConnect("localhost", 1, TheAsyncCallback, null));
        }

        [Fact]
        public void EndConnect_Throws_ArgumentNullException()
        {
            // Behavior difference:  EndConnect_Throws_ObjectDisposed
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndConnect(null));
        }

        [Fact]
        public void BeginSend_Buffer_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffer_SocketError_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffers_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffers_SocketError_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void EndSend_Throws_ArgumentNullException()
        {
            // Behavior difference: EndSend_Throws_ObjectDisposed
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndSend(null));
        }

        [Fact]
        public void BeginSendTo_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSendTo(s_buffer, 0, s_buffer.Length, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 1), TheAsyncCallback, null));
        }

        [Fact]
        public void EndSendTo_Throws_ArgumentNullException()
        {
            // Behavior difference: EndSendTo_Throws_ObjectDisposed
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndSendTo(null));
        }

        [Fact]
        public void BeginReceive_Buffer_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffer_SocketError_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffers_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffers_SocketError_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceive_Throws_ArgumentNullException()
        {
            // Behavior difference from Desktop: EndReceive_Throws_ObjectDisposed
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndReceive(null));
        }

        [Fact]
        public void BeginReceiveFrom_Throws_ObjectDisposedException()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceiveFrom(s_buffer, 0, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceiveFrom_Throws_ArgumentNullException()
        {
            // Behavior difference from Desktop: EndReceiveFrom_Throws_ObjectDisposed.
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndReceiveFrom(null, ref remote));
        }

        [Fact]
        public void BeginReceiveMessageFrom_Throws_ObjectDisposedException()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceiveMessageFrom(s_buffer, 0, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceiveMessageFrom_Throws_ArgumentNullException()
        {
            // Behavior difference from Desktop: EndReceiveMessageFrom_Throws_ObjectDisposed.
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndReceiveMessageFrom(null, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void BeginAccept_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginAccept(TheAsyncCallback, null));
        }

        [Fact]
        public void EndAccept_Throws_ArgumentNullException()
        {
            // Behavior difference from Desktop: EndAccept_Throws_ObjectDisposed
            Assert.Throws<ArgumentNullException>(() => GetDisposedSocket().EndAccept(null));
        }
    }
}
