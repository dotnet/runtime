// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace System.Net
{
    // TODO: #13187
    public sealed unsafe partial class HttpListenerContext
    {
        public HttpListenerRequest Request
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public HttpListenerResponse Response
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IPrincipal User
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol)
        {
            throw new PlatformNotSupportedException();
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            throw new PlatformNotSupportedException();
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
