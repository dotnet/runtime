// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Web;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer
{
    public class EchoWebSocketHeaders : IHttpHandler
    {
        private const int MaxBufferSize = 1024;

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                if (!context.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    context.Response.Write("Not a websocket request");

                    return;
                }

                context.AcceptWebSocketRequest(ProcessWebSocketRequest);
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

            // Reflect all headers and cookies
            var sb = new StringBuilder();
            sb.AppendLine("Headers:");

            foreach (string header in wsContext.Headers.AllKeys)
            {
                sb.Append(header);
                sb.Append(":");
                sb.AppendLine(wsContext.Headers[header]);
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
