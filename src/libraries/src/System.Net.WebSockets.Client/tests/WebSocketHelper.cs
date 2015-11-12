// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class WebSocketHelper
    {
        private static Lazy<bool> s_WebSocketSupported = new Lazy<bool>(InitWebSocketSupported);
        private static int s_TimeOutMilliseconds;
        private Uri _echoService;

        public WebSocketHelper(Uri echoService, int timeOut)
        {
            _echoService = echoService;
            s_TimeOutMilliseconds = timeOut;
        }

        public static bool ShouldSkipTestOSNotSupported(ITestOutputHelper output)
        {
            if (!s_WebSocketSupported.Value)
            {
                output.WriteLine("WinHttp/WebSockets are not supported on this platform.");
            }

            return !s_WebSocketSupported.Value;
        }

        public async Task TestEcho(WebSocketMessageType type)
        {
            var cts = new CancellationTokenSource(s_TimeOutMilliseconds);
            string message = "Hello WebSockets!";
            string closeMessage = "Good bye!";
            var receiveBuffer = new byte[100];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            ClientWebSocket cws = await GetConnectedWebSocket(_echoService);

            await cws.SendAsync(WebSocketData.GetBufferFromText(message), type, true, cts.Token);
            Assert.Equal(WebSocketState.Open, cws.State);

            WebSocketReceiveResult recvRet = await cws.ReceiveAsync(receiveSegment, cts.Token);
            Assert.Equal(WebSocketState.Open, cws.State);
            Assert.Equal(message.Length, recvRet.Count);
            Assert.Equal(type, recvRet.MessageType);
            Assert.Equal(true, recvRet.EndOfMessage);
            Assert.Equal(null, recvRet.CloseStatus);
            Assert.Equal(null, recvRet.CloseStatusDescription);

            var recvSegment = new ArraySegment<byte>(receiveSegment.Array, receiveSegment.Offset, recvRet.Count);
            Assert.Equal(message, WebSocketData.GetTextFromBuffer(recvSegment));

            Task taskClose = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeMessage, cts.Token);
            Assert.True(
                (cws.State == WebSocketState.Open) || (cws.State == WebSocketState.CloseSent) ||
                (cws.State == WebSocketState.CloseReceived) || (cws.State == WebSocketState.Closed),
                "State immediately after CloseAsync : " + cws.State);
            await taskClose;
            Assert.Equal(WebSocketState.Closed, cws.State);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
            Assert.Equal(closeMessage, cws.CloseStatusDescription);

            cws.Dispose();
            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        public static async Task<ClientWebSocket> GetConnectedWebSocket(Uri u)
        {
            var cws = new ClientWebSocket();
            var cts = new CancellationTokenSource(s_TimeOutMilliseconds);

            Task taskConnect = cws.ConnectAsync(u, cts.Token);
            Assert.True(
                (cws.State == WebSocketState.None) ||
                (cws.State == WebSocketState.Connecting) ||
                (cws.State == WebSocketState.Open),
                "State immediately after ConnectAsync incorrect: " + cws.State);
            await taskConnect;
            Assert.Equal(WebSocketState.Open, cws.State);

            return cws;
        }

        private static bool InitWebSocketSupported()
        {
            ClientWebSocket cws = null;

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
