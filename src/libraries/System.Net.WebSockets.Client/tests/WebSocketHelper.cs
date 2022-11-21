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
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public static class WebSocketHelper
    {
        private static readonly Lazy<bool> s_WebSocketSupported = new Lazy<bool>(InitWebSocketSupported);
        public static bool WebSocketsSupported { get { return s_WebSocketSupported.Value; } }

        public static async Task TestEcho(
            Uri server,
            WebSocketMessageType type,
            int timeOutMilliseconds,
            ITestOutputHelper output,
            Version version,
            HttpMessageInvoker? invoker = null)
        {
            var cts = new CancellationTokenSource(timeOutMilliseconds);
            string message = "Hello WebSockets!";
            string closeMessage = "Good bye!";
            var receiveBuffer = new byte[100];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            using (ClientWebSocket cws = await GetConnectedWebSocket(server, timeOutMilliseconds, output, version, invoker: invoker))
            {
                output.WriteLine("TestEcho: SendAsync starting.");
                await cws.SendAsync(WebSocketData.GetBufferFromText(message), type, true, cts.Token);
                output.WriteLine("TestEcho: SendAsync done.");
                Assert.Equal(WebSocketState.Open, cws.State);

                output.WriteLine("TestEcho: ReceiveAsync starting.");
                WebSocketReceiveResult recvRet = await cws.ReceiveAsync(receiveSegment, cts.Token);
                output.WriteLine("TestEcho: ReceiveAsync done.");
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(message.Length, recvRet.Count);
                Assert.Equal(type, recvRet.MessageType);
                Assert.True(recvRet.EndOfMessage);
                Assert.Null(recvRet.CloseStatus);
                Assert.Null(recvRet.CloseStatusDescription);

                var recvSegment = new ArraySegment<byte>(receiveSegment.Array, receiveSegment.Offset, recvRet.Count);
                Assert.Equal(message, WebSocketData.GetTextFromBuffer(recvSegment));

                output.WriteLine("TestEcho: CloseAsync starting.");
                Task taskClose = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeMessage, cts.Token);
                Assert.True(
                    (cws.State == WebSocketState.Open) || (cws.State == WebSocketState.CloseSent) ||
                    (cws.State == WebSocketState.CloseReceived) || (cws.State == WebSocketState.Closed),
                    "State immediately after CloseAsync : " + cws.State);
                await taskClose;
                output.WriteLine("TestEcho: CloseAsync done.");
                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(closeMessage, cws.CloseStatusDescription);
            }
        }

        public static Task<ClientWebSocket> GetConnectedWebSocket(
            Uri server,
            int timeOutMilliseconds,
            ITestOutputHelper output,
            Version version,
            TimeSpan keepAliveInterval = default,
            IWebProxy proxy = null,
            HttpMessageInvoker? invoker = null) =>
            Retry(output, async () =>
            {
                var cws = new ClientWebSocket();
                if (proxy != null)
                {
                    cws.Options.Proxy = proxy;
                }

                if (keepAliveInterval.TotalSeconds > 0)
                {
                    cws.Options.KeepAliveInterval = keepAliveInterval;
                }

                if (invoker == null)
                {
                    cws.Options.ClientCertificates.Add(Test.Common.Configuration.Certificates.GetClientCertificate());
                    cws.Options.RemoteCertificateValidationCallback = delegate { return true; };
                }

                cws.Options.HttpVersion = version;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                using (var cts = new CancellationTokenSource(timeOutMilliseconds))
                {
                    output.WriteLine("GetConnectedWebSocket: ConnectAsync starting.");
                    Task taskConnect = invoker == null ? cws.ConnectAsync(server, cts.Token) : cws.ConnectAsync(server, invoker, cts.Token);
                    Assert.True(
                        (cws.State == WebSocketState.None) ||
                        (cws.State == WebSocketState.Connecting) ||
                        (cws.State == WebSocketState.Open) ||
                        (cws.State == WebSocketState.Aborted),
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;
                    output.WriteLine("GetConnectedWebSocket: ConnectAsync done.");
                    Assert.Equal(WebSocketState.Open, cws.State);
                }
                return cws;
            });

        public static async Task<T> Retry<T>(ITestOutputHelper output, Func<Task<T>> func)
        {
            const int MaxTries = 5;
            int betweenTryDelayMilliseconds = 1000;

            for (int i = 1; ; i++)
            {
                try
                {
                    return await func();
                }
                catch (WebSocketException exc)
                {
                    output.WriteLine($"Retry after attempt #{i} failed with {exc}");
                    if (i == MaxTries)
                    {
                        throw;
                    }

                    await Task.Delay(betweenTryDelayMilliseconds);
                    betweenTryDelayMilliseconds *= 2;
                }
            }
        }

        public static (Uri, Task) GetEchoHttp2LoopbackServer()
        {
            return GetEchoHttp2LoopbackServer(new Http2Options());
        }

        public static (Uri, Task) GetEchoHttp2LoopbackServer(Http2Options options)
        {
            Http2LoopbackServer server = Http2LoopbackServer.CreateServer(options);

            Task serverTask = Task.Run(async () =>
            {
                var buffer = new byte[128 * 1024];

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                // send status 200 OK to establish websocket
                await connection.SendResponseHeadersAsync(streamId, endStream: false).ConfigureAwait(false);

                var webSocketStream = new Http2Stream(connection, streamId);
                using WebSocket websocket = WebSocket.CreateFromStream(webSocketStream, true, null, TimeSpan.FromSeconds(30));

                while (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.CloseSent)
                {
                    var result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (result.CloseStatus == WebSocketCloseStatus.Empty)
                        {
                            await websocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                        }
                        else
                        {
                            WebSocketCloseStatus closeStatus = result.CloseStatus.GetValueOrDefault();
                            await websocket.CloseAsync(
                                closeStatus,
                                result.CloseStatusDescription,
                                CancellationToken.None);
                        }

                        continue;
                    }

                    int offset = result.Count;
                    await websocket.SendAsync(new ArraySegment<byte>(buffer, 0, offset), result.MessageType, true, CancellationToken.None);
                }
                await connection.DisposeAsync();
            });

            return (server.Address, serverTask);
        }

        public static (Uri, Task) GetEchoLoopbackServer(LoopbackServer.Options options = null)
        {
            LoopbackServer server = new LoopbackServer(options);
            Task.WaitAll(server.ListenAsync());

            Task serverTask = server.AcceptConnectionAsync(async connection =>
            {
                var buffer = new byte[128 * 1024];
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                using WebSocket websocket = WebSocket.CreateFromStream(connection.Stream, true, null, TimeSpan.FromSeconds(30));
                while (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.CloseSent)
                {
                    var result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (result.CloseStatus == WebSocketCloseStatus.Empty)
                        {
                            await websocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                        }
                        else
                        {
                            WebSocketCloseStatus closeStatus = result.CloseStatus.GetValueOrDefault();
                            await websocket.CloseAsync(
                                closeStatus,
                                result.CloseStatusDescription,
                                CancellationToken.None);
                        }

                        continue;
                    }

                    int offset = result.Count;
                    await websocket.SendAsync(new ArraySegment<byte>(buffer, 0, offset), result.MessageType, true, CancellationToken.None);
                }

                await connection.DisposeAsync();
            });

            return (server.Address, serverTask);
        }

        private static bool InitWebSocketSupported()
        {
            ClientWebSocket cws = null;
            if (PlatformDetection.IsBrowser && !PlatformDetection.IsWebSocketSupported)
            {
                return false;
            }

            try
            {
                cws = new ClientWebSocket();
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
            finally
            {
                if (cws != null)
                {
                    cws.Dispose();
                }
            }
        }
    }
}
