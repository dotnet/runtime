// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed partial class WebSocketExceptionTests
    {
        public static object[][] ErrorData = {
            new object[] { WebSocketError.Success },
            new object[] { WebSocketError.InvalidMessageType },
            new object[] { WebSocketError.Faulted },
            new object[] { WebSocketError.NativeError },
            new object[] { WebSocketError.NotAWebSocket },
            new object[] { WebSocketError.UnsupportedVersion },
            new object[] { WebSocketError.UnsupportedProtocol },
            new object[] { WebSocketError.HeaderError },
            new object[] { WebSocketError.ConnectionClosedPrematurely },
            new object[] { WebSocketError.InvalidState },
        };

        public static object[][] NativeErrorData = {
            new object[] { 0, WebSocketError.Success, unchecked((int)0x80004005) },
            new object[] { -2147467259, WebSocketError.NativeError, -2147467259},
        };

        public static object[][] UnrelatedErrorData =
            ErrorData.SelectMany(wse => NativeErrorData.Select(ne => new object[] { wse[0], ne[0], ne[2] })).ToArray();

        [Fact]
        public void ConstructorTests_Parameterless_Success()
        {
            int lastError = Marshal.GetLastPInvokeError();
            var wse = new WebSocketException();
            Assert.Equal(wse.NativeErrorCode, lastError);
            Assert.Equal(wse.ErrorCode, lastError);
            Assert.NotEmpty(wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Success(WebSocketError error)
        {
            var wse = new WebSocketException(error);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Message_Success(WebSocketError error)
        {
            const string Message = "Message";
            var wse = new WebSocketException(error, Message);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Exception_Success(WebSocketError error)
        {
            var inner = new Exception();
            var wse = new WebSocketException(error, inner);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Same(inner, wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Message_Exception_Success(WebSocketError error)
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(error, Message, inner);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Same(inner, wse.InnerException);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Success(int nativeError, WebSocketError webSocketError, int expectedHResult)
        {
            var wse = new WebSocketException(nativeError);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(webSocketError, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Message_Success(int nativeError, WebSocketError webSocketError, int expectedHResult)
        {
            const string Message = "Message";
            var wse = new WebSocketException(nativeError, Message);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(webSocketError, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Exception_Success(int nativeError, WebSocketError webSocketError, int expectedHResult)
        {
            var inner = new Exception();
            var wse = new WebSocketException(nativeError, inner);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(webSocketError, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Same(inner, wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Success(int nativeError, WebSocketError error, int expectedHResult)
        {
            var wse = new WebSocketException(error, nativeError);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Message_Success(int nativeError, WebSocketError error, int expectedHResult)
        {
            const string Message = "Message";
            var wse = new WebSocketException(error, nativeError, Message);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Exception_Success(int nativeError, WebSocketError error, int expectedHResult)
        {
            var inner = new Exception();
            var wse = new WebSocketException(error, nativeError, inner);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.NotEqual("", wse.Message);
            Assert.Same(inner, wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Message_Exception_Success(int nativeError, WebSocketError error, int expectedHResult)
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(error, nativeError, Message, inner);
            Assert.Equal(expectedHResult, wse.HResult);
            Assert.Equal(error, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Same(inner, wse.InnerException);
        }

        [Fact]
        public void ConstructorTests_Message_Success()
        {
            const string Message = "Message";
            var wse = new WebSocketException(Message);
            Assert.Equal(WebSocketError.Success, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Null(wse.InnerException);
        }

        [Fact]
        public void ConstructorTests_Message_Exception_Success()
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(Message, inner);
            Assert.Equal(WebSocketError.Success, wse.WebSocketErrorCode);
            Assert.Equal(Message, wse.Message);
            Assert.Same(inner, wse.InnerException);
        }
    }
}
