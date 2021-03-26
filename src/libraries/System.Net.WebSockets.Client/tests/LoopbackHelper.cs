// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Client.Tests
{
    public static class LoopbackHelper
    {
        public static async Task<Dictionary<string, string>> WebSocketHandshakeAsync(LoopbackServer.Connection connection)
        {
            string serverResponse = null;
            List<string> headers = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

            var results = new Dictionary<string, string>();
            foreach (string header in headers)
            {
                string[] tokens = header.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 2)
                {
                    results.Add(tokens[0].Trim(), tokens[1].Trim());

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
                await connection.WriteStringAsync(serverResponse).ConfigureAwait(false);
                return results;
            }

            return null;
        }

        private static string ComputeWebSocketHandshakeSecurityAcceptValue(string secWebSocketKey)
        {
            // GUID specified by RFC 6455.
            const string Rfc6455Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string combinedKey = secWebSocketKey + Rfc6455Guid;

            // Use of SHA1 hash is required by RFC 6455.
            byte[] sha1Hash = SHA1.HashData(Encoding.UTF8.GetBytes(combinedKey));
            return Convert.ToBase64String(sha1Hash);
        }
    }
}
