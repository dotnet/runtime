// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Principal;

namespace System.Net.WebSockets
{
    public class HttpListenerWebSocketContext : WebSocketContext
    {
        private readonly Uri _requestUri;
        private readonly NameValueCollection _headers;
        private readonly CookieCollection _cookieCollection;
        private readonly IPrincipal _user;
        private readonly bool _isAuthenticated;
        private readonly bool _isLocal;
        private readonly bool _isSecureConnection;

        private readonly string _origin;
        private readonly IEnumerable<string> _secWebSocketProtocols;
        private readonly string _secWebSocketVersion;
        private readonly string _secWebSocketKey;

        private readonly WebSocket _webSocket;

        internal HttpListenerWebSocketContext(
            Uri requestUri,
            NameValueCollection headers,
            CookieCollection cookieCollection,
            IPrincipal user,
            bool isAuthenticated,
            bool isLocal,
            bool isSecureConnection,
            string origin,
            IEnumerable<string> secWebSocketProtocols,
            string secWebSocketVersion,
            string secWebSocketKey,
            WebSocket webSocket)
        {
            Debug.Assert(requestUri != null, "requestUri shouldn't be null");
            Debug.Assert(headers != null, "headers shouldn't be null");
            Debug.Assert(cookieCollection != null, "cookieCollection shouldn't be null");
            Debug.Assert(secWebSocketProtocols != null, "secWebSocketProtocols shouldn't be null");
            Debug.Assert(webSocket != null, "webSocket shouldn't be null");

            _cookieCollection = new CookieCollection();
            _cookieCollection.Add(cookieCollection);

            _headers = new NameValueCollection(headers);
            _user = CopyPrincipal(user)!;

            _requestUri = requestUri;
            _isAuthenticated = isAuthenticated;
            _isLocal = isLocal;
            _isSecureConnection = isSecureConnection;
            _origin = origin;
            _secWebSocketProtocols = secWebSocketProtocols;
            _secWebSocketVersion = secWebSocketVersion;
            _secWebSocketKey = secWebSocketKey;
            _webSocket = webSocket;
        }

        public override Uri RequestUri => _requestUri;

        public override NameValueCollection Headers => _headers;

        public override string Origin => _origin;

        public override IEnumerable<string> SecWebSocketProtocols => _secWebSocketProtocols;

        public override string SecWebSocketVersion => _secWebSocketVersion;

        public override string SecWebSocketKey => _secWebSocketKey;

        public override CookieCollection CookieCollection => _cookieCollection;

        public override IPrincipal User => _user;

        public override bool IsAuthenticated => _isAuthenticated;

        public override bool IsLocal => _isLocal;

        public override bool IsSecureConnection => _isSecureConnection;

        public override WebSocket WebSocket => _webSocket;

        private static IPrincipal? CopyPrincipal(IPrincipal user)
        {
            if (user != null)
            {
                if (!(user is WindowsPrincipal))
                {
                    // AuthenticationSchemes.Basic.
                    if (user.Identity is HttpListenerBasicIdentity basicIdentity)
                    {
                        return new GenericPrincipal(new HttpListenerBasicIdentity(basicIdentity.Name, basicIdentity.Password), null);
                    }
                }
                else
                {
                    // AuthenticationSchemes.Digest, AuthenticationSchemes.Negotiate, AuthenticationSchemes.NTLM.
#if TARGET_WINDOWS
                    WindowsIdentity windowsIdentity = (WindowsIdentity)user.Identity!;
                    return new WindowsPrincipal(new WindowsIdentity(windowsIdentity.Token, windowsIdentity.AuthenticationType!, WindowsAccountType.Normal, true));
#else
                    throw new PlatformNotSupportedException();
#endif

                }
            }

            return null;
        }
    }
}
