// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Net.WebSockets.Tests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EchoWebSocketHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            WebSocketEchoOptions options = WebSocketEchoOptions.Parse(context.Request.QueryString.Value);
            if (options.Delay is TimeSpan d)
            {
                await Task.Delay(d);
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
                if (!string.IsNullOrEmpty(options.SubProtocol))
                {
                    socket = await context.WebSockets.AcceptWebSocketAsync(options.SubProtocol);
                }
                else
                {
                    socket = await context.WebSockets.AcceptWebSocketAsync();
                }

                await WebSocketEchoHelper.ProcessRequest(
                    socket,
                    options.ReplyWithPartialMessages,
                    options.ReplyWithEnhancedCloseMessage);
            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }
    }
}
