// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace System.Net.WebSockets
{
    internal static partial class WebSocketValidate
    {
        internal const string SecWebSocketKeyGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        internal const string WebSocketUpgradeToken = "websocket";
        internal const int DefaultReceiveBufferSize = 16 * 1024;
        internal const int DefaultClientSendBufferSize = 16 * 1024;

        // RFC 6455 requests WebSocket clients to let the server initiate the TCP close to avoid that client sockets 
        // end up in TIME_WAIT-state
        //
        // After both sending and receiving a Close message, an endpoint considers the WebSocket connection closed and 
        // MUST close the underlying TCP connection.  The server MUST close the underlying TCP connection immediately; 
        // the client SHOULD wait for the server to close the connection but MAY close the connection at any time after
        // sending and receiving a Close message, e.g., if it has not received a TCP Close from the server in a 
        // reasonable time period.
        internal const int ClientTcpCloseTimeout = 1000; // 1s

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 used only for hashing purposes, not for crypto.")]
        internal static string GetSecWebSocketAcceptString(string secWebSocketKey)
        {
            string retVal;

            // SHA1 used only for hashing purposes, not for crypto. Check here for FIPS compat.
            using (SHA1 sha1 = SHA1.Create())
            {
                string acceptString = string.Concat(secWebSocketKey, WebSocketValidate.SecWebSocketKeyGuid);
                byte[] toHash = Encoding.UTF8.GetBytes(acceptString);
                retVal = Convert.ToBase64String(sha1.ComputeHash(toHash));
            }

            return retVal;
        }

        // return value here signifies if a Sec-WebSocket-Protocol header should be returned by the server. 
        internal static bool ProcessWebSocketProtocolHeader(string clientSecWebSocketProtocol,
            string subProtocol,
            out string acceptProtocol)
        {
            acceptProtocol = string.Empty;
            if (string.IsNullOrEmpty(clientSecWebSocketProtocol))
            {
                // client hasn't specified any Sec-WebSocket-Protocol header
                if (subProtocol != null)
                {
                    // If the server specified _anything_ this isn't valid.
                    throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                        SR.Format(SR.net_WebSockets_ClientAcceptingNoProtocols, subProtocol));
                }
                // Treat empty and null from the server as the same thing here, server should not send headers. 
                return false;
            }

            // here, we know the client specified something and it's non-empty.

            if (subProtocol == null)
            {
                // client specified some protocols, server specified 'null'. So server should send headers.                 
                return true;
            }

            // here, we know that the client has specified something, it's not empty
            // and the server has specified exactly one protocol

            string[] requestProtocols = clientSecWebSocketProtocol.Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);
            acceptProtocol = subProtocol;

            // client specified protocols, serverOptions has exactly 1 non-empty entry. Check that 
            // this exists in the list the client specified. 
            for (int i = 0; i < requestProtocols.Length; i++)
            {
                string currentRequestProtocol = requestProtocols[i].Trim();
                if (string.Compare(acceptProtocol, currentRequestProtocol, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                SR.Format(SR.net_WebSockets_AcceptUnsupportedProtocol,
                    clientSecWebSocketProtocol,
                    subProtocol));
        }


        internal static void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
    }
}
