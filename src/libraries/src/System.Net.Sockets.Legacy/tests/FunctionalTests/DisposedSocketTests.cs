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
        public void EndConnect_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndConnect(null));
        }

        [Fact]
        public void BeginDisconnect_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginDisconnect(false, TheAsyncCallback, null));
        }

        [Fact]
        public void EndDisconnect_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndDisconnect(null));
        }

        [Fact]
        public void BeginSend_Buffer_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffer_SocketError_Throws_ObjectDisposedException()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffer, 0, s_buffer.Length, SocketFlags.None, out errorCode, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffers_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginSend_Buffers_SocketError_Throws_ObjectDisposedException()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSend(s_buffers, SocketFlags.None, out errorCode, TheAsyncCallback, null));
        }

        [Fact]
        public void EndSend_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndSend(null));
        }

        [Fact]
        public void EndSend_SocketError_Throws_ObjectDisposed()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndSend(null, out errorCode));
        }

        [Fact]
        public void BeginSendTo_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginSendTo(s_buffer, 0, s_buffer.Length, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 1), TheAsyncCallback, null));
        }

        [Fact]
        public void EndSendTo_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndSendTo(null));
        }

        [Fact]
        public void BeginReceive_Buffer_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffer, 0, s_buffer.Length, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffer_SocketError_Throws_ObjectDisposedException()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffer, 0, s_buffer.Length, SocketFlags.None, out errorCode, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffers_Throws_ObjectDisposedException()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffers, SocketFlags.None, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginReceive_Buffers_SocketError_Throws_ObjectDisposedException()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceive(s_buffers, SocketFlags.None, out errorCode, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceive_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndReceive(null));
        }

        [Fact]
        public void EndReceive_SocketError_Throws_ObjectDisposed()
        {
            SocketError errorCode;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndReceive(null, out errorCode));
        }

        [Fact]
        public void BeginReceiveFrom_Throws_ObjectDisposedException()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceiveFrom(s_buffer, 0, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceiveFrom_Throws_ObjectDisposed()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndReceiveFrom(null, ref remote));
        }

        [Fact]
        public void BeginReceiveMessageFrom_Throws_ObjectDisposedException()
        {
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginReceiveMessageFrom(s_buffer, 0, s_buffer.Length, SocketFlags.None, ref remote, TheAsyncCallback, null));
        }

        [Fact]
        public void EndReceiveMessageFrom_Throws_ObjectDisposed()
        {
            SocketFlags flags = SocketFlags.None;
            EndPoint remote = new IPEndPoint(IPAddress.Loopback, 1);
            IPPacketInformation packetInfo;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndReceiveMessageFrom(null, ref flags, ref remote, out packetInfo));
        }

        [Fact]
        public void BeginAccept_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginAccept(TheAsyncCallback, null));
        }

        [Fact]
        public void BeginAccept_Int_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginAccept(0, TheAsyncCallback, null));
        }

        [Fact]
        public void BeginAccept_Socket_Int_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().BeginAccept(null, 0, TheAsyncCallback, null));
        }

        [Fact]
        public void EndAccept_Throws_ObjectDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndAccept(null));
        }

        [Fact]
        public void EndAccept_Buffer_Throws_ObjectDisposed()
        {
            byte[] buffer;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndAccept(out buffer, null));
        }

        [Fact]
        public void EndAccept_Buffer_Int_Throws_ObjectDisposed()
        {
            byte[] buffer;
            int received;
            Assert.Throws<ObjectDisposedException>(() => GetDisposedSocket().EndAccept(out buffer, out received, null));
        }
    }
}
