// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets
{
    internal struct WebSocketHandle
    {
        #region Properties
        public bool IsValid
        {
            get
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
            }
        }

        public WebSocketCloseStatus? CloseStatus
        {
            get
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
            }
        }

        public string CloseStatusDescription
        {
            get
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
            }
        }

        public WebSocketState State
        {
            get
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
            }
        }

        public string SubProtocol
        {
            get
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
            }
        }
        #endregion

        public static WebSocketHandle Create()
        {
            return default(WebSocketHandle);
        }

        public static void CheckPlatformSupport()
        {
        }
        
        public Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        public Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        public Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        public Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        public void Dispose()
        {
        }

        public void Abort()
        {
        }
    }
}
