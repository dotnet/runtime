// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class Connected
    {
        [Fact]
        public void NonBlockingFailedConnect_ConnectedReturnsFalse()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Blocking = false;

            // Connect to port 1 where we expect no server to be listening.
            SocketException se = Assert.ThrowsAny<SocketException>(() => socket.Connect(IPAddress.Loopback, 1));

            Assert.Equal(SocketError.WouldBlock, se.SocketErrorCode);

            // Give the non-blocking connect some time to complete.
            socket.Poll(5_000, SelectMode.SelectWrite);

            Assert.False(socket.Connected);
        }
    }
}
