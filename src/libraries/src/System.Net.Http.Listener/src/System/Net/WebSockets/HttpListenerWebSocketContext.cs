// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Principal;

namespace System.Net.WebSockets
{
    public class HttpListenerWebSocketContext : WebSocketContext
    {
        private Uri _requestUri;
        private NameValueCollection _headers;
        private CookieCollection _cookieCollection;
        private IPrincipal _user;
        private bool _isAuthenticated;
        private bool _isLocal;
        private bool _isSecureConnection;

        private string _origin;
        private IEnumerable<string> _secWebSocketProtocols;
        private string _secWebSocketVersion;
        private string _secWebSocketKey;

        private WebSocket _webSocket;

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
            _user = CopyPrincipal(user);

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

        public override Uri RequestUri
        {
            get { return _requestUri; }
        }

        public override NameValueCollection Headers
        {
            get { return _headers; }
        }

        public override string Origin
        {
            get { return _origin; }
        }

        public override IEnumerable<string> SecWebSocketProtocols
        {
            get { return _secWebSocketProtocols; }
        }

        public override string SecWebSocketVersion
        {
            get { return _secWebSocketVersion; }
        }

        public override string SecWebSocketKey
        {
            get { return _secWebSocketKey; }
        }

        public override CookieCollection CookieCollection
        {
            get { return _cookieCollection; }
        }

        public override IPrincipal User
        {
            get { return _user; }
        }

        public override bool IsAuthenticated
        {
            get { return _isAuthenticated; }
        }

        public override bool IsLocal
        {
            get { return _isLocal; }
        }

        public override bool IsSecureConnection
        {
            get { return _isSecureConnection; }
        }

        public override WebSocket WebSocket
        {
            get { return _webSocket; }
        }

        private static IPrincipal CopyPrincipal(IPrincipal user)
        {
            if (user != null)
            {
                throw new NotImplementedException();
            }

            return null;
        }
    }
}
