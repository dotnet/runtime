// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed class WebSocketExceptionTests
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
            new object[] { 0, WebSocketError.Success },
            new object[] { -2147467259, WebSocketError.NativeError },
        };

        public static object[][] UnrelatedErrorData =
            ErrorData.SelectMany(wse => NativeErrorData.Select(ne => new object[] { wse[0], ne[0] })).ToArray();

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Success(WebSocketError error)
        {
            var wse = new WebSocketException(error);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.NotEqual(wse.Message, "");
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Message_Success(WebSocketError error)
        {
            const string Message = "Message";
            var wse = new WebSocketException(error, Message);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.Equal(wse.Message, Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Exception_Success(WebSocketError error)
        {
            var inner = new Exception();
            var wse = new WebSocketException(error, inner);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.NotEqual(wse.Message, "");
            Assert.Equal(wse.InnerException, inner);
        }

        [Theory, MemberData(nameof(ErrorData))]
        public void ConstructorTests_WebSocketError_Message_Exception_Success(WebSocketError error)
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(error, Message, inner);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.Equal(wse.Message, Message);
            Assert.Equal(wse.InnerException, inner);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Success(int nativeError, WebSocketError webSocketError)
        {
            var wse = new WebSocketException(nativeError);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, webSocketError);
            Assert.NotEqual(wse.Message, "");
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Message_Success(int nativeError, WebSocketError webSocketError)
        {
            const string Message = "Message";
            var wse = new WebSocketException(nativeError, Message);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, webSocketError);
            Assert.Equal(wse.Message, Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(NativeErrorData))]
        public void ConstructorTests_NativeError_Exception_Success(int nativeError, WebSocketError webSocketError)
        {
            var inner = new Exception();
            var wse = new WebSocketException(nativeError, inner);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, webSocketError);
            Assert.NotEqual(wse.Message, "");
            Assert.Equal(wse.InnerException, inner);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Success(int nativeError, WebSocketError error)
        {
            var wse = new WebSocketException(error, nativeError);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.NotEqual(wse.Message, "");
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Message_Success(int nativeError, WebSocketError error)
        {
            const string Message = "Message";
            var wse = new WebSocketException(error, nativeError, Message);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.Equal(wse.Message, Message);
            Assert.Null(wse.InnerException);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Exception_Success(int nativeError, WebSocketError error)
        {
            var inner = new Exception();
            var wse = new WebSocketException(error, nativeError, inner);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.NotEqual(wse.Message, "");
            Assert.Equal(wse.InnerException, inner);
        }

        [Theory, MemberData(nameof(UnrelatedErrorData))]
        public void ConstructorTests_WebSocketError_NativeError_Message_Exception_Success(int nativeError, WebSocketError error)
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(error, nativeError, Message, inner);
            Assert.Equal(wse.HResult, nativeError);
            Assert.Equal(wse.WebSocketErrorCode, error);
            Assert.Equal(wse.Message, Message);
            Assert.Equal(wse.InnerException, inner);
        }

        public void ConstructorTests_Message_Success()
        {
            const string Message = "Message";
            var wse = new WebSocketException(Message);
            Assert.Equal(wse.WebSocketErrorCode, WebSocketError.Success);
            Assert.Equal(wse.Message, Message);
            Assert.Null(wse.InnerException);
        }

        public void ConstructorTests_Message_Exception_Success()
        {
            const string Message = "Message";
            var inner = new Exception();
            var wse = new WebSocketException(Message, inner);
            Assert.Equal(wse.WebSocketErrorCode, WebSocketError.Success);
            Assert.Equal(wse.Message, Message);
            Assert.Equal(wse.InnerException, inner);
        }
    }
}
