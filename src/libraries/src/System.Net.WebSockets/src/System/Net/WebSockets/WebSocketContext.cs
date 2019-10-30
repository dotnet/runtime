// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;

namespace System.Net.WebSockets
{
    public abstract class WebSocketContext
    {
        public abstract Uri RequestUri { get; }
        public abstract NameValueCollection Headers { get; }
        public abstract string Origin { get; }
        public abstract IEnumerable<string> SecWebSocketProtocols { get; }
        public abstract string SecWebSocketVersion { get; }
        public abstract string SecWebSocketKey { get; }
        public abstract CookieCollection CookieCollection { get; }
        public abstract IPrincipal User { get; }
        public abstract bool IsAuthenticated { get; }
        public abstract bool IsLocal { get; }
        public abstract bool IsSecureConnection { get; }
        public abstract WebSocket WebSocket { get; }
    }
}
