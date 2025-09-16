// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public static class WebSocketHandshakeHelper
    {
        public static async Task<WebSocketRequestData> ProcessHttp11RequestAsync(
            LoopbackServer.Connection connection,
            bool skipServerHandshakeResponse = false,
            bool parseEchoOptions = false,
            CancellationToken cancellationToken = default)
        {
            List<string> headers = await connection.ReadRequestHeaderAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

            var data = new WebSocketRequestData()
            {
                HttpVersion = HttpVersion.Version11,
                Http11Connection = connection
            };

            if (parseEchoOptions)
            {
                // extract query with leading '?' from request line
                // e.g. GET /echo?query=string HTTP/1.1 => "?query=string"
                int queryIndex = headers[0].IndexOf('?');
                if (queryIndex != -1)
                {
                    int spaceIndex = headers[0].IndexOf(' ', queryIndex);
                    string query = headers[0].Substring(queryIndex, spaceIndex - queryIndex);

                    // NOTE: ProcessOptions needs to be called before sending the server response
                    // because it may be configured to delay the response.

                    data.EchoOptions = await WebSocketEchoHelper.ProcessOptions(query, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    data.EchoOptions = WebSocketEchoOptions.Default;
                }
            }

            for (int i = 1; i < headers.Count; ++i)
            {
                string[] tokens = headers[i].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length is 1 or 2)
                {
                    data.Headers.Add(
                        tokens[0].Trim(),
                        tokens.Length == 2 ? tokens[1].Trim() : null);
                }
            }

            var isValidOpeningHandshake = data.Headers.TryGetValue("Sec-WebSocket-Key", out var secWebSocketKey);
            Assert.True(isValidOpeningHandshake);

            if (!skipServerHandshakeResponse)
            {
                foreach (string headerName in new[] { "Sec-WebSocket-Extensions", "Sec-WebSocket-Protocol" })
                {
                    if (data.Headers.TryGetValue(headerName, out var headerValue))
                    {
                        Assert.Fail($"Header `{headerName}: {headerValue}` requires a custom server response, use skipServerHandshakeResponse=true");
                    }
                }

                await SendHttp11ServerResponseAsync(connection, secWebSocketKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            data.TransportStream = connection.Stream;
            return data;
        }

        public static async Task SendHttp11ServerResponseAsync(
            LoopbackServer.Connection connection,
            string secWebSocketKey,
            string? negotiatedSubProtocol = null,
            string? negotiatedExtensions = null,
            CancellationToken cancellationToken = default)
        {
            var serverResponse = LoopbackHelper.GetServerResponseString(secWebSocketKey, negotiatedExtensions, negotiatedSubProtocol);
            await connection.WriteStringAsync(serverResponse).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<WebSocketRequestData> ProcessHttp2RequestAsync(
            Http2LoopbackServer server,
            bool skipServerHandshakeResponse = false,
            bool parseEchoOptions = false,
            CancellationToken cancellationToken = default)
        {
            var connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 })
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            connection.IgnoreWindowUpdates();

            (int streamId, var httpRequestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false)
                .WaitAsync(cancellationToken).ConfigureAwait(false);

            var data = new WebSocketRequestData
            {
                HttpVersion = HttpVersion.Version20,
                Http2Connection = connection,
                Http2StreamId = streamId
            };

            foreach (var header in httpRequestData.Headers)
            {
                Assert.NotNull(header.Name);
                data.Headers.Add(header.Name, header.Value);
            }

            var isValidOpeningHandshake = httpRequestData.Method == HttpMethod.Connect.ToString() && data.Headers.ContainsKey(":protocol");
            Assert.True(isValidOpeningHandshake);

            if (parseEchoOptions)
            {
                // RFC 7540, section 8.3. The CONNECT Method:
                //  > The ":scheme" and ":path" pseudo-header fields MUST be omitted.
                //
                // HTTP/2 CONNECT requests must drop query (containing echo options) from the request URI.
                // The information needs to be passed in a different way, e.g. in a custom header.

                if (data.Headers.TryGetValue(WebSocketHelper.OriginalQueryStringHeader, out var query))
                {
                    // NOTE: ProcessOptions needs to be called before sending the server response
                    // because it may be configured to delay the response.
                    data.EchoOptions = await WebSocketEchoHelper.ProcessOptions(query, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    data.EchoOptions = WebSocketEchoOptions.Default;
                }
            }

            if (!skipServerHandshakeResponse)
            {
                foreach (string headerName in new[] { "Sec-WebSocket-Extensions", "Sec-WebSocket-Protocol" })
                {
                    if (data.Headers.TryGetValue(headerName, out var headerValue))
                    {
                        Assert.Fail($"Header `{headerName}: {headerValue}` requires a custom server response, use skipServerHandshakeResponse=true");
                    }
                }

                await SendHttp2ServerResponseAsync(connection, streamId, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            data.TransportStream = new Http2LoopbackStream(connection, streamId, sendResetOnDispose: false);
            return data;
        }

        public static async Task SendHttp2ServerResponseAsync(
            Http2LoopbackConnection connection,
            int streamId,
            string? negotiatedSubProtocol = null,
            string? negotiatedExtensions = null,
            bool endStream = false,
            CancellationToken cancellationToken = default)
        {
            var negotiatedValues = new List<HttpHeaderData>();
            if (negotiatedExtensions is not null)
            {
                negotiatedValues.Add(new HttpHeaderData("Sec-WebSocket-Extensions", negotiatedExtensions));
            }
            if (negotiatedSubProtocol is not null)
            {
                negotiatedValues.Add(new HttpHeaderData("Sec-WebSocket-Protocol", negotiatedSubProtocol));
            }

            // send status 200 OK to establish websocket
            // we don't need to send Sec-WebSocket-Accept as Sec-WebSocket-Key is not used for HTTP/2
            // note: endStream=true is abnormal and used for testing premature EOS scenarios only
            await connection.SendResponseHeadersAsync(streamId, endStream: endStream, headers: negotiatedValues).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task SendHttp11ServerResponseAndEosAsync(WebSocketRequestData requestData, Func<WebSocketRequestData, CancellationToken, Task>? requestDataCallback, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpVersion.Version11, requestData.HttpVersion);

            // sending default handshake response
            await SendHttp11ServerResponseAsync(requestData.Http11Connection!, requestData.Headers["Sec-WebSocket-Key"], cancellationToken: cancellationToken).ConfigureAwait(false);

            if (requestDataCallback is not null)
            {
                await requestDataCallback(requestData, cancellationToken).ConfigureAwait(false);
            }

            // send server EOS (half-closing from server side)
            requestData.Http11Connection!.Socket.Shutdown(SocketShutdown.Send);
        }

        public static async Task SendHttp2ServerResponseAndEosAsync(WebSocketRequestData requestData, bool eosInHeadersFrame, Func<WebSocketRequestData, CancellationToken, Task>? requestDataCallback, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpVersion.Version20, requestData.HttpVersion);

            var connection = requestData.Http2Connection!;
            var streamId = requestData.Http2StreamId!.Value;

            await SendHttp2ServerResponseAsync(connection, streamId, endStream: eosInHeadersFrame, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (requestDataCallback is not null)
            {
                await requestDataCallback(requestData, cancellationToken).ConfigureAwait(false);
            }

            if (!eosInHeadersFrame)
            {
                // send server EOS (half-closing from server side)
                await connection.SendResponseDataAsync(streamId, Array.Empty<byte>(), endStream: true).ConfigureAwait(false);
            }
        }
    }
}
