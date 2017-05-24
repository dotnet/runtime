// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        private HttpConnection _connection;

        internal HttpListenerContext(HttpConnection connection)
        {
            _connection = connection;
            _response = new HttpListenerResponse(this);
            Request = new HttpListenerRequest(this);
            ErrorStatus = 400;
        }

        internal int ErrorStatus { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection => _connection;
        
        internal void ParseAuthentication(AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == AuthenticationSchemes.Anonymous)
                return;

            string header = Request.Headers[HttpKnownHeaderNames.Authorization];
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

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            return HttpWebSocket.AcceptWebSocketAsyncCore(this, subProtocol, receiveBufferSize, keepAliveInterval);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
        {
            WebSocketValidate.ValidateArraySegment(internalBuffer, nameof(internalBuffer));
            HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, HttpWebSocket.MinSendBufferSize, keepAliveInterval);
            return HttpWebSocket.AcceptWebSocketAsyncCore(this, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }
    }
}
