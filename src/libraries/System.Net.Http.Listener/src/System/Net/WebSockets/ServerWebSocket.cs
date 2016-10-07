//------------------------------------------------------------------------------
// <copyright file="ServerWebSocket.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
                WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
            }

            WebSocketHelpers.ValidateInnerStream(innerStream);
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            WebSocketHelpers.ValidateArraySegment<byte>(internalBuffer, "internalBuffer");
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return new ServerWebSocket(innerStream,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }


        private readonly SafeHandle m_SessionHandle;
        private readonly WebSocketProtocolComponent.Property[] m_Properties;
        
        public ServerWebSocket(Stream innerStream,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
            : base(innerStream, subProtocol, keepAliveInterval, 
                WebSocketBuffer.CreateServerBuffer(internalBuffer, receiveBufferSize))
        {
            m_Properties = InternalBuffer.CreateProperties(false);
            m_SessionHandle = CreateWebSocketHandle();

            if (m_SessionHandle == null || m_SessionHandle.IsInvalid)
            {
                WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
            }

            StartKeepAliveTimer();
        }

        internal override SafeHandle SessionHandle
        {
            get
            {
                Debug.Assert(m_SessionHandle != null, "'m_SessionHandle MUST NOT be NULL.");
                return m_SessionHandle;
            }
        }

        private SafeHandle CreateWebSocketHandle()
        {
            Debug.Assert(m_Properties != null, "'m_Properties' MUST NOT be NULL.");
            SafeWebSocketHandle sessionHandle;
            WebSocketProtocolComponent.WebSocketCreateServerHandle(
                m_Properties,
                m_Properties.Length, 
                out sessionHandle);
            Debug.Assert(sessionHandle != null, "'sessionHandle MUST NOT be NULL.");

            return sessionHandle;
        }
    }
}
