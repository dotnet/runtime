// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Tests
{
    public static class WebSocketEchoHelper
    {
        private const int MaxBufferSize = 128 * 1024;
        private const int HeadersBufferSize = 1024;

        public static async Task ProcessRequest(WebSocket socket, bool replyWithPartialMessages, bool replyWithEnhancedCloseMessage)
        {
            var receiveBuffer = new byte[MaxBufferSize];
            var throwAwayBuffer = new byte[MaxBufferSize];

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    if (receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                    }
                    else
                    {
                        WebSocketCloseStatus closeStatus = receiveResult.CloseStatus.GetValueOrDefault();
                        await socket.CloseAsync(
                            closeStatus,
                            replyWithEnhancedCloseMessage ?
                                ("Server received: " + (int)closeStatus + " " + receiveResult.CloseStatusDescription) :
                                receiveResult.CloseStatusDescription,
                            CancellationToken.None);
                    }

                    continue;
                }

                // Keep reading until we get an entire message.
                int offset = receiveResult.Count;
                while (receiveResult.EndOfMessage == false)
                {
                    if (offset < MaxBufferSize)
                    {
                        receiveResult = await socket.ReceiveAsync(
                            new ArraySegment<byte>(receiveBuffer, offset, MaxBufferSize - offset),
                            CancellationToken.None);
                    }
                    else
                    {
                        receiveResult = await socket.ReceiveAsync(
                            new ArraySegment<byte>(throwAwayBuffer),
                            CancellationToken.None);
                    }

                    offset += receiveResult.Count;
                }

                // Close socket if the message was too big.
                if (offset > MaxBufferSize)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.MessageTooBig,
                        String.Format("{0}: {1} > {2}", WebSocketCloseStatus.MessageTooBig.ToString(), offset, MaxBufferSize),
                        CancellationToken.None);

                    continue;
                }

                bool sendMessage = false;
                string receivedMessage = null;
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, offset);
                    if (receivedMessage == ".close")
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                    }
                    else if (receivedMessage == ".shutdown")
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                    }
                    else if (receivedMessage == ".abort")
                    {
                        socket.Abort();
                    }
                    else if (receivedMessage == ".delay5sec")
                    {
                        await Task.Delay(5000);
                    }
                    else if (receivedMessage == ".receiveMessageAfterClose")
                    {
                        byte[] buffer = new byte[1024];
                        string message = $"{receivedMessage} {DateTime.Now.ToString("HH:mm:ss")}";
                        buffer = System.Text.Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, message.Length),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                    }
                    else if (socket.State == WebSocketState.Open)
                    {
                        sendMessage = true;
                    }
                }
                else
                {
                    sendMessage = true;
                }

                if (sendMessage)
                {
                    await socket.SendAsync(
                            new ArraySegment<byte>(receiveBuffer, 0, offset),
                            receiveResult.MessageType,
                            !replyWithPartialMessages,
                            CancellationToken.None);
                }
                if (receivedMessage == ".closeafter")
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                }
                else if (receivedMessage == ".shutdownafter")
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                }
            }
        }

        public static async Task ProcessHeadersRequest(WebSocket socket, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var receiveBuffer = new byte[HeadersBufferSize];

            // Reflect all headers and cookies
            var sb = new StringBuilder();
            sb.AppendLine("Headers:");

            foreach (KeyValuePair<string, string> pair in headers)
            {
                sb.Append(pair.Key);
                sb.Append(":");
                sb.AppendLine(pair.Value);
            }

            byte[] sendBuffer = Encoding.UTF8.GetBytes(sb.ToString());
            await socket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, new CancellationToken());

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    if (receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                    }
                    else
                    {
                        await socket.CloseAsync(
                            receiveResult.CloseStatus.GetValueOrDefault(),
                            receiveResult.CloseStatusDescription,
                            CancellationToken.None);
                    }

                    continue;
                }
            }
        }
    }
}
