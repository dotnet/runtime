// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public static class WebSocketHelper
    {
        public const string OriginalQueryStringHeader = "x-original-query-string";

        private static readonly Lazy<bool> s_WebSocketSupported = new Lazy<bool>(InitWebSocketSupported);
        public static bool WebSocketsSupported { get { return s_WebSocketSupported.Value; } }

        public static async Task TestEcho(
            ClientWebSocket cws,
            WebSocketMessageType type,
            CancellationToken cancellationToken)
        {
            string message = "Hello WebSockets!";
            string closeMessage = "Good bye!";
            var receiveBuffer = new byte[100];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            await cws.SendAsync(message.ToUtf8(), type, true, cancellationToken);
            Assert.Equal(WebSocketState.Open, cws.State);

            WebSocketReceiveResult recvRet = await cws.ReceiveAsync(receiveSegment, cancellationToken);
            Assert.Equal(WebSocketState.Open, cws.State);
            Assert.Equal(message.Length, recvRet.Count);
            Assert.Equal(type, recvRet.MessageType);
            Assert.True(recvRet.EndOfMessage);
            Assert.Null(recvRet.CloseStatus);
            Assert.Null(recvRet.CloseStatusDescription);

            var recvSegment = new ArraySegment<byte>(receiveSegment.Array, receiveSegment.Offset, recvRet.Count);
            Assert.Equal(message, recvSegment.Utf8ToString());

            Task taskClose = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeMessage, cancellationToken);
            Assert.True(
                (cws.State == WebSocketState.Open) || (cws.State == WebSocketState.CloseSent) ||
                (cws.State == WebSocketState.CloseReceived) || (cws.State == WebSocketState.Closed),
                "State immediately after CloseAsync : " + cws.State);
            await taskClose;
            Assert.Equal(WebSocketState.Closed, cws.State);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
            Assert.Equal(closeMessage, cws.CloseStatusDescription);
        }

        public static async Task<T> Retry<T>(Func<Task<T>> func)
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
                    if (i == MaxTries)
                    {
                        Assert.Fail($"Failed after {MaxTries} attempts with exception: {exc}");
                    }

                    await Task.Delay(betweenTryDelayMilliseconds);
                    betweenTryDelayMilliseconds *= 2;
                }
            }
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
                cws?.Dispose();
            }
        }

        public static ArraySegment<byte> ToUtf8(this string text)
            => new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));

        public static string Utf8ToString(this ArraySegment<byte> buffer)
            => Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
    }
}
