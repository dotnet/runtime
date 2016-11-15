// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets
{
    internal sealed class ServerWebSocket : WebSocketBase
    {
        internal static WebSocket Create(Stream innerStream,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            if (!WebSocketProtocolComponent.IsSupported)
            {
                WebSocketValidate.ThrowPlatformNotSupportedException_WSPC();
            }

            WebSocketValidate.ValidateInnerStream(innerStream);
            WebSocketValidate.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            WebSocketValidate.ValidateArraySegment<byte>(internalBuffer, nameof(internalBuffer));
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return new ServerWebSocket(innerStream,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }


        private readonly SafeHandle _sessionHandle;
        private readonly Interop.WebSocket.Property[] _properties;

        public ServerWebSocket(Stream innerStream,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
            : base(innerStream, subProtocol, keepAliveInterval,
                WebSocketBuffer.CreateServerBuffer(internalBuffer, receiveBufferSize))
        {
            _properties = InternalBuffer.CreateProperties(false);
            _sessionHandle = CreateWebSocketHandle();

            if (_sessionHandle == null || _sessionHandle.IsInvalid)
            {
                WebSocketValidate.ThrowPlatformNotSupportedException_WSPC();
            }

            StartKeepAliveTimer();
        }

        internal override SafeHandle SessionHandle
        {
            get
            {
                Debug.Assert(_sessionHandle != null, "'_sessionHandle MUST NOT be NULL.");
                return _sessionHandle;
            }
        }

        private SafeHandle CreateWebSocketHandle()
        {
            Debug.Assert(_properties != null, "'_properties' MUST NOT be NULL.");
            SafeWebSocketHandle sessionHandle;
            WebSocketProtocolComponent.WebSocketCreateServerHandle(
                _properties,
                _properties.Length,
                out sessionHandle);
            Debug.Assert(sessionHandle != null, "'sessionHandle MUST NOT be NULL.");

            return sessionHandle;
        }
    }
}
