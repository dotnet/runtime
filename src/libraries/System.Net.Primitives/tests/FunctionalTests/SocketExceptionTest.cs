// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class SocketExceptionTest
    {
        [Fact]
        public static void Create_AllErrorCodes_Success()
        {
            foreach (SocketError error in Enum.GetValues(typeof(SocketError)))
            {
                SocketException e = new SocketException((int)error);
                Assert.Equal(error, e.SocketErrorCode);
                Assert.Null(e.InnerException);
                Assert.NotNull(e.Message);
            }
        }

        [Fact]
        public static void Create_ExceptionWithMessage_Success()
        {
            const string message = "Hello World";
            SocketException e = new SocketException((int)SocketError.AccessDenied, message);
            Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            Assert.Null(e.InnerException);
            Assert.Equal(message, e.Message);
            Assert.Contains(message, e.ToString());
        }

        [Fact]
        public static void Create_SocketConnectException_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 55555);
                Assert.ThrowsAsync<SocketException>(() => socket.ConnectAsync(ep));
                try
                {
                    socket.Connect(ep);
                    Assert.Fail("Socket Connect should throw SocketException in this case.");
                }
                catch(SocketException ex)
                {
                    Assert.Equal(ep.ToString(), ex.Message);
                }
            }
        }
    }
}
