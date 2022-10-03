// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets
{
    // This class is a wrapper for a WSPC (WebSocket protocol component) session. WebSocketCreateClientHandle and WebSocketCreateServerHandle return a PVOID and not a real handle
    // but we use a SafeHandle because it provides us the guarantee that WebSocketDeleteHandle will always get called.
    internal sealed class SafeWebSocketHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeWebSocketHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                WebSocketProtocolComponent.WebSocketDeleteHandle(handle);
            }
            return true;
        }
    }
}
