// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets
{
    internal sealed class ServerWebSocket : WebSocketBase
    {
        internal static WebSocket Create(Stream innerStream,
            string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            if (!WebSocketProtocolComponent.IsSupported)
            {
                HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
            }

            HttpWebSocket.ValidateInnerStream(innerStream);
            HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, HttpWebSocket.MinSendBufferSize, keepAliveInterval);
            WebSocketValidate.ValidateArraySegment(internalBuffer, nameof(internalBuffer));
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, HttpWebSocket.MinSendBufferSize, true);

            return new ServerWebSocket(innerStream,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }


        private readonly SafeHandle _sessionHandle;
        private readonly Interop.WebSocket.Property[] _properties;

        public ServerWebSocket(Stream innerStream,
            string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
            : base(innerStream, subProtocol, keepAliveInterval,
                WebSocketBuffer.CreateServerBuffer(internalBuffer, receiveBufferSize))
        {
            _properties = InternalBuffer.CreateProperties(false);
            SafeHandle sessionHandle = CreateWebSocketHandle();

            if (sessionHandle == null || sessionHandle.IsInvalid)
            {
                sessionHandle?.Dispose();
                HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
            }

            _sessionHandle = sessionHandle;
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
            return WebSocketProtocolComponent.WebSocketCreateServerHandle(
                _properties,
                _properties.Length);
        }
    }
}
