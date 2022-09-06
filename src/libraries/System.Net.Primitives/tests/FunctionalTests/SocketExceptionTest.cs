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
            const string Message = "Hello World";
            SocketException e = new SocketException((int)SocketError.AccessDenied, Message);
            Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            Assert.Null(e.InnerException);
            Assert.Equal(Message, e.Message);
            Assert.Contains(Message, e.ToString());
        }
    }
}
