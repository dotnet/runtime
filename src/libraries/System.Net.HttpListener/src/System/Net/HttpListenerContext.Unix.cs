// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.WebSockets;
using System.Security.Principal;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        private HttpListenerRequest _request;
        private HttpListenerResponse _response;
        private IPrincipal _user;
        private HttpConnection _cnc;
        private string _error;
        private int _err_status = 400;
        internal HttpListener Listener;

        internal HttpListenerContext(HttpConnection cnc)
        {
            _cnc = cnc;
            _request = new HttpListenerRequest(this);
            _response = new HttpListenerResponse(this);
        }

        internal int ErrorStatus
        {
            get { return _err_status; }
            set { _err_status = value; }
        }

        internal string ErrorMessage
        {
            get { return _error; }
            set { _error = value; }
        }

        internal bool HaveError
        {
            get { return (_error != null); }
        }

        internal HttpConnection Connection
        {
            get { return _cnc; }
        }

        public HttpListenerRequest Request
        {
            get { return _request; }
        }

        public HttpListenerResponse Response
        {
            get { return _response; }
        }

        public IPrincipal User
        {
            get { return _user; }
        }

        internal void ParseAuthentication(AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == AuthenticationSchemes.Anonymous)
                return;

            string header = _request.Headers[HttpHeaderStrings.Authorization];
            if (header == null || header.Length < 2)
                return;

            string[] authenticationData = header.Split(new char[] { ' ' }, 2);
            if (string.Compare(authenticationData[0], AuthenticationTypes.Basic, true) == 0)
            {
                _user = ParseBasicAuthentication(authenticationData[1]);
            }
        }

        internal IPrincipal ParseBasicAuthentication(string authData)
        {
            try
            {
                // Basic AUTH Data is a formatted Base64 String
                string user = null;
                string password = null;
                int pos = -1;
                string authString = Text.Encoding.Default.GetString(Convert.FromBase64String(authData));

                // The format is DOMAIN\username:password
                // Domain is optional

                pos = authString.IndexOf(':');

                // parse the password off the end
                password = authString.Substring(pos + 1);

                // discard the password
                authString = authString.Substring(0, pos);

                // check if there is a domain
                pos = authString.IndexOf('\\');

                if (pos > 0)
                {
                    user = authString.Substring(pos);
                }
                else
                {
                    user = authString;
                }

                HttpListenerBasicIdentity identity = new HttpListenerBasicIdentity(user, password);
                return new GenericPrincipal(identity, new string[0]);
            }
            catch (Exception)
            {
                // Invalid auth data is swallowed silently
                return null;
            }
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol)
        {
            throw new NotImplementedException();
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            throw new NotImplementedException();
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            throw new NotImplementedException();
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
        {
            throw new NotImplementedException();
        }
    }
}
