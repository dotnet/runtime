// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
        public static async Task<WebSocketRequestData> ProcessHttp11RequestAsync(LoopbackServer.Connection connection, bool sendServerResponse = true, CancellationToken cancellationToken = default)
        {
            List<string> headers = await connection.ReadRequestHeaderAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

            var data = new WebSocketRequestData()
            {
                HttpVersion = HttpVersion.Version11,
                Http11Connection = connection
            };

            foreach (string header in headers.Skip(1))
            {
                string[] tokens = header.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length is 1 or 2)
                {
                    data.Headers.Add(
                        tokens[0].Trim(),
                        tokens.Length == 2 ? tokens[1].Trim() : null);
                }
            }

            var isValidOpeningHandshake = data.Headers.TryGetValue("Sec-WebSocket-Key", out var secWebSocketKey);
            Assert.True(isValidOpeningHandshake);

            if (sendServerResponse)
            {
                await SendHttp11ServerResponseAsync(connection, secWebSocketKey, cancellationToken).ConfigureAwait(false);
            }

            data.WebSocketStream = connection.Stream;
            return data;
        }

        private static async Task SendHttp11ServerResponseAsync(LoopbackServer.Connection connection, string secWebSocketKey, CancellationToken cancellationToken)
        {
            var serverResponse = LoopbackHelper.GetServerResponseString(secWebSocketKey);
            await connection.WriteStringAsync(serverResponse).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<WebSocketRequestData> ProcessHttp2RequestAsync(Http2LoopbackServer server, bool sendServerResponse = true, CancellationToken cancellationToken = default)
        {
            var connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 })
                .WaitAsync(cancellationToken).ConfigureAwait(false);

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

            if (sendServerResponse)
            {
                await SendHttp2ServerResponseAsync(connection, streamId, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            data.WebSocketStream = new Http2LoopbackStream(connection, streamId);
            return data;
        }

        private static async Task SendHttp2ServerResponseAsync(Http2LoopbackConnection connection, int streamId, bool endStream = false, CancellationToken cancellationToken = default)
        {
            // send status 200 OK to establish websocket
            // we don't need to send anything additional as Sec-WebSocket-Key is not used for HTTP/2
            // note: endStream=true is abnormal and used for testing premature EOS scenarios only
            await connection.SendResponseHeadersAsync(streamId, endStream: endStream).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task SendHttp11ServerResponseAndEosAsync(WebSocketRequestData requestData, Func<WebSocketRequestData, CancellationToken, Task>? requestDataCallback, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpVersion.Version11, requestData.HttpVersion);

            // sending default handshake response
            await SendHttp11ServerResponseAsync(requestData.Http11Connection!, requestData.Headers["Sec-WebSocket-Key"], cancellationToken).ConfigureAwait(false);

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

            await SendHttp2ServerResponseAsync(connection, streamId, endStream: eosInHeadersFrame, cancellationToken).ConfigureAwait(false);

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
