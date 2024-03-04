// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EchoWebSocketHandler
    {
        private const int MaxBufferSize = 128 * 1024;

        public static async Task InvokeAsync(HttpContext context)
        {
            QueryString queryString = context.Request.QueryString;
            bool  replyWithPartialMessages = queryString.HasValue && queryString.Value.Contains("replyWithPartialMessages");
            bool replyWithEnhancedCloseMessage = queryString.HasValue && queryString.Value.Contains("replyWithEnhancedCloseMessage");

            string subProtocol = context.Request.Query["subprotocol"];

            if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay10sec"))
            {
                await Task.Delay(10000);
            }
            else if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay20sec"))
            {
                await Task.Delay(20000);
            }

            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Not a websocket request");

                    return;
                }

                WebSocket socket;
                if (!string.IsNullOrEmpty(subProtocol))
                {
                    socket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
                }
                else
                {
                    socket = await context.WebSockets.AcceptWebSocketAsync();
                }

                await ProcessWebSocketRequest(socket, replyWithPartialMessages, replyWithEnhancedCloseMessage);
            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }

        private static async Task ProcessWebSocketRequest(
            WebSocket socket,
            bool replyWithPartialMessages,
            bool replyWithEnhancedCloseMessage)
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
                    else if (receivedMessage == ".shutdownAfterTwoMessages")
                    {
                        byte[] buffer = new byte[1024];
                        string message1 = $"{receivedMessage} 1 {DateTime.Now.ToString("HH:mm:ss")}";
                        buffer = System.Text.Encoding.UTF8.GetBytes(message1);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, message1.Length),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                        await Task.Delay(1990); // try to sync with receive request from the client: 1.9k is too little, 2k too much

                        string message2 = $"{receivedMessage} 2 {DateTime.Now.ToString("HH:mm:ss")}";
                        buffer = System.Text.Encoding.UTF8.GetBytes(message2);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, message2.Length),
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
    }
}
