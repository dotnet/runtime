// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Client.Tests
{
    public static class LoopbackHelper
    {
        public static async Task<bool> WebSocketHandshakeAsync(LoopbackServer.Connection connection)
        {
            string serverResponse = null;
            string currentRequestLine;
            while (!string.IsNullOrEmpty(currentRequestLine = await connection.ReadLineAsync().ConfigureAwait(false)))
            {
                string[] tokens = currentRequestLine.Split(new char[] { ':' }, 2);
                if (tokens.Length == 2)
                {
                    string headerName = tokens[0];
                    if (headerName == "Sec-WebSocket-Key")
                    {
                        string headerValue = tokens[1].Trim();
                        string responseSecurityAcceptValue = ComputeWebSocketHandshakeSecurityAcceptValue(headerValue);
                        serverResponse =
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Content-Length: 0\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Sec-WebSocket-Accept: " + responseSecurityAcceptValue + "\r\n\r\n";
                    }
                }
            }

            if (serverResponse != null)
            {
                // We received a valid WebSocket opening handshake. Send the appropriate response.
                await connection.Writer.WriteAsync(serverResponse).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private static string ComputeWebSocketHandshakeSecurityAcceptValue(string secWebSocketKey)
        {
            // GUID specified by RFC 6455.
            const string Rfc6455Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string combinedKey = secWebSocketKey + Rfc6455Guid;

            // Use of SHA1 hash is required by RFC 6455.
            SHA1 sha1Provider = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
            return Convert.ToBase64String(sha1Hash);
        }
    }
}
