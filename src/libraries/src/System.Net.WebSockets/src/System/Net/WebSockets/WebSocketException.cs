// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.WebSockets
{
    [Serializable]
    public sealed class WebSocketException : Win32Exception
    {
        private readonly WebSocketError _webSocketErrorCode;

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
           Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException()
            : this(Marshal.GetLastWin32Error())
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error)
            : this(error, GetErrorMessage(error))
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, string message) : base(message)
        {
            _webSocketErrorCode = error;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, Exception innerException)
            : this(error, GetErrorMessage(error), innerException)
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, string message, Exception innerException)
            : base(message, innerException)
        {
            _webSocketErrorCode = error;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(int nativeError)
            : base(nativeError)
        {
            _webSocketErrorCode = !Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            SetErrorCodeOnError(nativeError);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(int nativeError, string message)
            : base(nativeError, message)
        {
            _webSocketErrorCode = !Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            SetErrorCodeOnError(nativeError);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(int nativeError, Exception innerException)
            : base(SR.net_WebSockets_Generic, innerException)
        {
            _webSocketErrorCode = !Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            SetErrorCodeOnError(nativeError);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, int nativeError)
            : this(error, nativeError, GetErrorMessage(error))
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, int nativeError, string message)
            : base(message)
        {
            _webSocketErrorCode = error;
            SetErrorCodeOnError(nativeError);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, int nativeError, Exception innerException)
            : this(error, nativeError, GetErrorMessage(error), innerException)
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(WebSocketError error, int nativeError, string message, Exception innerException)
            : base(message, innerException)
        {
            _webSocketErrorCode = error;
            SetErrorCodeOnError(nativeError);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(string message)
            : base(message)
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This ctor is harmless, because it does not pass arbitrary data into the native code.")]
        public WebSocketException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private WebSocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            _webSocketErrorCode = (WebSocketError)serializationInfo.GetInt32(nameof(WebSocketErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue(nameof(WebSocketErrorCode), (int)_webSocketErrorCode);
            base.GetObjectData(info, context);
        }

        public override int ErrorCode
        {
            get
            {
                return base.NativeErrorCode;
            }
        }

        public WebSocketError WebSocketErrorCode
        {
            get
            {
                return _webSocketErrorCode;
            }
        }

        private static string GetErrorMessage(WebSocketError error)
        {
            // Provide a canned message for the error type.
            switch (error)
            {
                case WebSocketError.InvalidMessageType:
                    return SR.Format(SR.net_WebSockets_InvalidMessageType_Generic,
                        $"{nameof(WebSocket)}.{nameof(WebSocket.CloseAsync)}",
                        $"{nameof(WebSocket)}.{nameof(WebSocket.CloseOutputAsync)}");
                case WebSocketError.Faulted:
                    return SR.net_Websockets_WebSocketBaseFaulted;
                case WebSocketError.NotAWebSocket:
                    return SR.net_WebSockets_NotAWebSocket_Generic;
                case WebSocketError.UnsupportedVersion:
                    return SR.net_WebSockets_UnsupportedWebSocketVersion_Generic;
                case WebSocketError.UnsupportedProtocol:
                    return SR.net_WebSockets_UnsupportedProtocol_Generic;
                case WebSocketError.HeaderError:
                    return SR.net_WebSockets_HeaderError_Generic;
                case WebSocketError.ConnectionClosedPrematurely:
                    return SR.net_WebSockets_ConnectionClosedPrematurely_Generic;
                case WebSocketError.InvalidState:
                    return SR.net_WebSockets_InvalidState_Generic;
                default:
                    return SR.net_WebSockets_Generic;
            }
        }

        // Set the error code only if there is an error (i.e. nativeError >= 0). Otherwise the code fails during deserialization 
        // as the Exception..ctor() throws on setting HResult to 0. The default for HResult is -2147467259.
        private void SetErrorCodeOnError(int nativeError)
        {
            if (!Succeeded(nativeError))
            {
                HResult = nativeError;
            }
        }

        private static bool Succeeded(int hr)
        {
            return (hr >= 0);
        }
    }
}
