// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.WebSockets;
using System.Web;
using System.Web.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer
{
    public class EchoWebSocket : IHttpHandler
    {
        private const int MaxBufferSize = 128 * 1024;
        private bool _replyWithPartialMessages = false;

        public void ProcessRequest(HttpContext context)
        {
            _replyWithPartialMessages = context.Request.Url.Query.Contains("replyWithPartialMessages");

            string subProtocol = context.Request.QueryString["subprotocol"];

            if (context.Request.Url.Query.Contains("delay10sec"))
            {
                Thread.Sleep(10000);
            }

            try
            {
                if (!context.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    context.Response.Write("Not a websocket request");

                    return;
                }

                if (!string.IsNullOrEmpty(subProtocol))
                {
                    var wsOptions = new AspNetWebSocketOptions();
                    wsOptions.SubProtocol = subProtocol;

                    context.AcceptWebSocketRequest(ProcessWebSocketRequest, wsOptions);
                }
                else
                {
                    context.AcceptWebSocketRequest(ProcessWebSocketRequest);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = ex.Message;
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        private async Task ProcessWebSocketRequest(WebSocketContext wsContext)
        {
            WebSocket socket = wsContext.WebSocket;
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
                        await socket.CloseAsync(
                            receiveResult.CloseStatus.GetValueOrDefault(),
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
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, offset);
                    if (receivedMessage == ".close")
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, CancellationToken.None);
                    }
                    if (receivedMessage == ".shutdown")
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
                            !_replyWithPartialMessages,
                            CancellationToken.None);
                }
            }
        }
    }
}
