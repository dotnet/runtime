// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets
{
    internal static class WebSocketProtocolComponent
    {
        private static readonly string s_dummyWebsocketKeyBase64 = Convert.ToBase64String(new byte[16]);
        private static readonly IntPtr s_webSocketDllHandle;
        private static readonly string s_supportedVersion;

        private static readonly Interop.WebSocket.HttpHeader[] s_initialClientRequestHeaders = new Interop.WebSocket.HttpHeader[]
            {
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.Connection,
                    NameLength = (uint)HttpKnownHeaderNames.Connection.Length,
                    Value = HttpKnownHeaderNames.Upgrade,
                    ValueLength = (uint)HttpKnownHeaderNames.Upgrade.Length
                },
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.Upgrade,
                    NameLength = (uint)HttpKnownHeaderNames.Upgrade.Length,
                    Value = HttpWebSocket.WebSocketUpgradeToken,
                    ValueLength = (uint)HttpWebSocket.WebSocketUpgradeToken.Length
                }
            };

        private static readonly Interop.WebSocket.HttpHeader[] s_ServerFakeRequestHeaders;

        internal enum Action
        {
            NoAction = 0,
            SendToNetwork = 1,
            IndicateSendComplete = 2,
            ReceiveFromNetwork = 3,
            IndicateReceiveComplete = 4,
        }
        internal enum BufferType : uint
        {
            None = 0x00000000,
            UTF8Message = 0x80000000,
            UTF8Fragment = 0x80000001,
            BinaryMessage = 0x80000002,
            BinaryFragment = 0x80000003,
            Close = 0x80000004,
            PingPong = 0x80000005,
            UnsolicitedPong = 0x80000006
        }

        internal enum PropertyType
        {
            ReceiveBufferSize = 0,
            SendBufferSize = 1,
            DisableMasking = 2,
            AllocatedBuffer = 3,
            DisableUtf8Verification = 4,
            KeepAliveInterval = 5,
        }

        internal enum ActionQueue
        {
            Send = 1,
            Receive = 2,
        }

#pragma warning disable CA1810 // explicit static cctor
        static WebSocketProtocolComponent()
        {
            s_webSocketDllHandle = Interop.Kernel32.LoadLibraryEx(Interop.Libraries.WebSocket, IntPtr.Zero, 0);

            if (s_webSocketDllHandle == IntPtr.Zero)
                return;

            s_supportedVersion = GetSupportedVersion();

            s_ServerFakeRequestHeaders = new Interop.WebSocket.HttpHeader[]
            {
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.Connection,
                    NameLength = (uint)HttpKnownHeaderNames.Connection.Length,
                    Value = HttpKnownHeaderNames.Upgrade,
                    ValueLength = (uint)HttpKnownHeaderNames.Upgrade.Length
                },
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.Upgrade,
                    NameLength = (uint)HttpKnownHeaderNames.Upgrade.Length,
                    Value = HttpWebSocket.WebSocketUpgradeToken,
                    ValueLength = (uint)HttpWebSocket.WebSocketUpgradeToken.Length
                },
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.Host,
                    NameLength = (uint)HttpKnownHeaderNames.Host.Length,
                    Value = string.Empty,
                    ValueLength = 0
                },
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.SecWebSocketVersion,
                    NameLength = (uint)HttpKnownHeaderNames.SecWebSocketVersion.Length,
                    Value = s_supportedVersion,
                    ValueLength = (uint)s_supportedVersion.Length
                },
                new Interop.WebSocket.HttpHeader()
                {
                    Name = HttpKnownHeaderNames.SecWebSocketKey,
                    NameLength = (uint)HttpKnownHeaderNames.SecWebSocketKey.Length,
                    Value = s_dummyWebsocketKeyBase64,
                    ValueLength = (uint)s_dummyWebsocketKeyBase64.Length
                }
            };
        }
#pragma warning restore CA1810

        internal static string SupportedVersion
        {
            get
            {
                if (!IsSupported)
                {
                    HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
                }

                return s_supportedVersion;
            }
        }

        internal static bool IsSupported
        {
            get
            {
                return s_webSocketDllHandle != IntPtr.Zero;
            }
        }

        internal static string GetSupportedVersion()
        {
            if (!IsSupported)
            {
                HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
            }

            SafeWebSocketHandle webSocketHandle = null;
            try
            {
                int errorCode = Interop.WebSocket.WebSocketCreateClientHandle(null, 0, out webSocketHandle);
                ThrowOnError(errorCode);

                if (webSocketHandle == null ||
                    webSocketHandle.IsInvalid)
                {
                    HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
                }

                IntPtr additionalHeadersPtr;
                uint additionalHeaderCount;

                errorCode = Interop.WebSocket.WebSocketBeginClientHandshake(webSocketHandle,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    s_initialClientRequestHeaders,
                    (uint)s_initialClientRequestHeaders.Length,
                    out additionalHeadersPtr,
                    out additionalHeaderCount);
                ThrowOnError(errorCode);

                Interop.WebSocket.HttpHeader[] additionalHeaders = MarshalHttpHeaders(additionalHeadersPtr, (int)additionalHeaderCount);

                string version = null;
                foreach (Interop.WebSocket.HttpHeader header in additionalHeaders)
                {
                    if (string.Equals(header.Name,
                            HttpKnownHeaderNames.SecWebSocketVersion,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        version = header.Value;
                        break;
                    }
                }
                Debug.Assert(version != null, "'version' MUST NOT be NULL.");

                return version;
            }
            finally
            {
                if (webSocketHandle != null)
                {
                    webSocketHandle.Dispose();
                }
            }
        }

        internal static void WebSocketCreateServerHandle(Interop.WebSocket.Property[] properties,
            int propertyCount,
            out SafeWebSocketHandle webSocketHandle)
        {
            Debug.Assert(propertyCount >= 0, "'propertyCount' MUST NOT be negative.");
            Debug.Assert((properties == null && propertyCount == 0) ||
                (properties != null && propertyCount == properties.Length),
                "'propertyCount' MUST MATCH 'properties.Length'.");

            if (!IsSupported)
            {
                HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
            }

            int errorCode = Interop.WebSocket.WebSocketCreateServerHandle(properties, (uint)propertyCount, out webSocketHandle);
            ThrowOnError(errorCode);

            if (webSocketHandle == null ||
                webSocketHandle.IsInvalid)
            {
                HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
            }

            IntPtr responseHeadersPtr;
            uint responseHeaderCount;

            // Currently the WSPC doesn't allow to initiate a data session
            // without also being involved in the http handshake
            // There is no information whatsoever, which is needed by the
            // WSPC for parsing WebSocket frames from the HTTP handshake
            // In the managed implementation the HTTP header handling
            // will be done using the managed HTTP stack and we will
            // just fake an HTTP handshake for the WSPC calling
            // WebSocketBeginServerHandshake and WebSocketEndServerHandshake
            // with statically defined dummy headers.
            errorCode = Interop.WebSocket.WebSocketBeginServerHandshake(webSocketHandle,
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                s_ServerFakeRequestHeaders,
                (uint)s_ServerFakeRequestHeaders.Length,
                out responseHeadersPtr,
                out responseHeaderCount);

            ThrowOnError(errorCode);

            errorCode = Interop.WebSocket.WebSocketEndServerHandshake(webSocketHandle);

            ThrowOnError(errorCode);

            Debug.Assert(webSocketHandle != null, "'webSocketHandle' MUST NOT be NULL at this point.");
        }

        internal static void WebSocketAbortHandle(SafeHandle webSocketHandle)
        {
            Debug.Assert(webSocketHandle != null && !webSocketHandle.IsInvalid,
                "'webSocketHandle' MUST NOT be NULL or INVALID.");

            Interop.WebSocket.WebSocketAbortHandle(webSocketHandle);

            DrainActionQueue(webSocketHandle, ActionQueue.Send);
            DrainActionQueue(webSocketHandle, ActionQueue.Receive);
        }

        internal static void WebSocketDeleteHandle(IntPtr webSocketPtr)
        {
            Debug.Assert(webSocketPtr != IntPtr.Zero, "'webSocketPtr' MUST NOT be IntPtr.Zero.");
            Interop.WebSocket.WebSocketDeleteHandle(webSocketPtr);
        }

        internal static void WebSocketSend(WebSocketBase webSocket,
            BufferType bufferType,
            Interop.WebSocket.Buffer buffer)
        {
            Debug.Assert(webSocket != null,
                "'webSocket' MUST NOT be NULL or INVALID.");
            Debug.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

            ThrowIfSessionHandleClosed(webSocket);

            int errorCode;
            try
            {
                errorCode = Interop.WebSocket.WebSocketSend_Raw(webSocket.SessionHandle, bufferType, ref buffer, IntPtr.Zero);
            }
            catch (ObjectDisposedException innerException)
            {
                throw ConvertObjectDisposedException(webSocket, innerException);
            }

            ThrowOnError(errorCode);
        }

        internal static void WebSocketSendWithoutBody(WebSocketBase webSocket,
            BufferType bufferType)
        {
            Debug.Assert(webSocket != null,
                "'webSocket' MUST NOT be NULL or INVALID.");
            Debug.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

            ThrowIfSessionHandleClosed(webSocket);

            int errorCode;
            try
            {
                errorCode = Interop.WebSocket.WebSocketSendWithoutBody_Raw(webSocket.SessionHandle, bufferType, IntPtr.Zero, IntPtr.Zero);
            }
            catch (ObjectDisposedException innerException)
            {
                throw ConvertObjectDisposedException(webSocket, innerException);
            }

            ThrowOnError(errorCode);
        }

        internal static void WebSocketReceive(WebSocketBase webSocket)
        {
            Debug.Assert(webSocket != null,
                "'webSocket' MUST NOT be NULL or INVALID.");
            Debug.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

            ThrowIfSessionHandleClosed(webSocket);

            int errorCode;
            try
            {
                errorCode = Interop.WebSocket.WebSocketReceive(webSocket.SessionHandle, IntPtr.Zero, IntPtr.Zero);
            }
            catch (ObjectDisposedException innerException)
            {
                throw ConvertObjectDisposedException(webSocket, innerException);
            }

            ThrowOnError(errorCode);
        }

        internal static void WebSocketGetAction(WebSocketBase webSocket,
            ActionQueue actionQueue,
            Interop.WebSocket.Buffer[] dataBuffers,
            ref uint dataBufferCount,
            out Action action,
            out BufferType bufferType,
            out IntPtr actionContext)
        {
            Debug.Assert(webSocket != null,
                "'webSocket' MUST NOT be NULL or INVALID.");
            Debug.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");
            Debug.Assert(dataBufferCount >= 0, "'dataBufferCount' MUST NOT be negative.");
            Debug.Assert((dataBuffers == null && dataBufferCount == 0) ||
                (dataBuffers != null && dataBufferCount == dataBuffers.Length),
                "'dataBufferCount' MUST MATCH 'dataBuffers.Length'.");

            action = Action.NoAction;
            bufferType = BufferType.None;
            actionContext = IntPtr.Zero;

            IntPtr dummy;
            ThrowIfSessionHandleClosed(webSocket);

            int errorCode;
            try
            {
                errorCode = Interop.WebSocket.WebSocketGetAction(webSocket.SessionHandle,
                    actionQueue,
                    dataBuffers,
                    ref dataBufferCount,
                    out action,
                    out bufferType,
                    out dummy,
                    out actionContext);
            }
            catch (ObjectDisposedException innerException)
            {
                throw ConvertObjectDisposedException(webSocket, innerException);
            }
            ThrowOnError(errorCode);

            webSocket.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);

            Debug.Assert(dataBufferCount >= 0);
            Debug.Assert((dataBufferCount == 0 && dataBuffers == null) ||
                (dataBufferCount <= dataBuffers.Length));
        }

        internal static void WebSocketCompleteAction(WebSocketBase webSocket,
            IntPtr actionContext,
            int bytesTransferred)
        {
            Debug.Assert(webSocket != null,
                "'webSocket' MUST NOT be NULL or INVALID.");
            Debug.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");
            Debug.Assert(actionContext != IntPtr.Zero, "'actionContext' MUST NOT be IntPtr.Zero.");
            Debug.Assert(bytesTransferred >= 0, "'bytesTransferred' MUST NOT be negative.");

            if (webSocket.SessionHandle.IsClosed)
            {
                return;
            }

            try
            {
                Interop.WebSocket.WebSocketCompleteAction(webSocket.SessionHandle, actionContext, (uint)bytesTransferred);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static void DrainActionQueue(SafeHandle webSocketHandle, ActionQueue actionQueue)
        {
            Debug.Assert(webSocketHandle != null && !webSocketHandle.IsInvalid,
                "'webSocketHandle' MUST NOT be NULL or INVALID.");

            IntPtr actionContext;
            IntPtr dummy;
            Action action;
            BufferType bufferType;

            while (true)
            {
                Interop.WebSocket.Buffer[] dataBuffers = new Interop.WebSocket.Buffer[1];
                uint dataBufferCount = 1;
                int errorCode = Interop.WebSocket.WebSocketGetAction(webSocketHandle,
                    actionQueue,
                    dataBuffers,
                    ref dataBufferCount,
                    out action,
                    out bufferType,
                    out dummy,
                    out actionContext);

                if (!Succeeded(errorCode))
                {
                    Debug.Assert(errorCode == 0, "'errorCode' MUST be 0.");
                    return;
                }

                if (action == Action.NoAction)
                {
                    return;
                }

                Interop.WebSocket.WebSocketCompleteAction(webSocketHandle, actionContext, 0);
            }
        }

        private static void MarshalAndVerifyHttpHeader(IntPtr httpHeaderPtr,
            ref Interop.WebSocket.HttpHeader httpHeader)
        {
            Debug.Assert(httpHeaderPtr != IntPtr.Zero, "'currentHttpHeaderPtr' MUST NOT be IntPtr.Zero.");

            IntPtr httpHeaderNamePtr = Marshal.ReadIntPtr(httpHeaderPtr);
            IntPtr lengthPtr = IntPtr.Add(httpHeaderPtr, IntPtr.Size);
            int length = Marshal.ReadInt32(lengthPtr);
            Debug.Assert(length >= 0, "'length' MUST NOT be negative.");

            if (httpHeaderNamePtr != IntPtr.Zero)
            {
                httpHeader.Name = Marshal.PtrToStringAnsi(httpHeaderNamePtr, length);
            }

            if ((httpHeader.Name == null && length != 0) ||
                (httpHeader.Name != null && length != httpHeader.Name.Length))
            {
                Debug.Fail("The length of 'httpHeader.Name' MUST MATCH 'length'.");
                throw new AccessViolationException();
            }

            // structure of Interop.WebSocket.HttpHeader:
            //   Name = string*
            //   NameLength = uint*
            //   Value = string*
            //   ValueLength = uint*
            // NOTE - All fields in the object are pointers to the actual value, hence the use of
            //        n * IntPtr.Size to get to the correct place in the object.
            int valueOffset = 2 * IntPtr.Size;
            int lengthOffset = 3 * IntPtr.Size;

            IntPtr httpHeaderValuePtr =
                Marshal.ReadIntPtr(IntPtr.Add(httpHeaderPtr, valueOffset));
            lengthPtr = IntPtr.Add(httpHeaderPtr, lengthOffset);
            length = Marshal.ReadInt32(lengthPtr);
            httpHeader.Value = Marshal.PtrToStringAnsi(httpHeaderValuePtr, (int)length);

            if ((httpHeader.Value == null && length != 0) ||
                (httpHeader.Value != null && length != httpHeader.Value.Length))
            {
                Debug.Fail("The length of 'httpHeader.Value' MUST MATCH 'length'.");
                throw new AccessViolationException();
            }
        }

        private static Interop.WebSocket.HttpHeader[] MarshalHttpHeaders(IntPtr nativeHeadersPtr,
            int nativeHeaderCount)
        {
            Debug.Assert(nativeHeaderCount >= 0, "'nativeHeaderCount' MUST NOT be negative.");
            Debug.Assert(nativeHeadersPtr != IntPtr.Zero || nativeHeaderCount == 0,
                "'nativeHeaderCount' MUST be 0.");

            Interop.WebSocket.HttpHeader[] httpHeaders = new Interop.WebSocket.HttpHeader[nativeHeaderCount];

            // structure of Interop.WebSocket.HttpHeader:
            //   Name = string*
            //   NameLength = uint*
            //   Value = string*
            //   ValueLength = uint*
            // NOTE - All fields in the object are pointers to the actual value, hence the use of
            //        4 * IntPtr.Size to get to the next header.
            int httpHeaderStructSize = 4 * IntPtr.Size;

            for (int i = 0; i < nativeHeaderCount; i++)
            {
                int offset = httpHeaderStructSize * i;
                IntPtr currentHttpHeaderPtr = IntPtr.Add(nativeHeadersPtr, offset);
                MarshalAndVerifyHttpHeader(currentHttpHeaderPtr, ref httpHeaders[i]);
            }

            Debug.Assert(httpHeaders != null);
            Debug.Assert(httpHeaders.Length == nativeHeaderCount);

            return httpHeaders;
        }

        public static bool Succeeded(int hr)
        {
            return (hr >= 0);
        }

        private static void ThrowOnError(int errorCode)
        {
            if (Succeeded(errorCode))
            {
                return;
            }

            throw new WebSocketException(errorCode);
        }

        private static void ThrowIfSessionHandleClosed(WebSocketBase webSocket)
        {
            if (webSocket.SessionHandle.IsClosed)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State));
            }
        }

        private static WebSocketException ConvertObjectDisposedException(WebSocketBase webSocket, ObjectDisposedException innerException)
        {
            return new WebSocketException(WebSocketError.InvalidState,
                SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State),
                innerException);
        }
    }
}
