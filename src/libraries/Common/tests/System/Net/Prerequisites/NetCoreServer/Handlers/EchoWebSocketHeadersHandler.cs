// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net.WebSockets.Tests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace NetCoreServer
{
    public class EchoWebSocketHeadersHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Not a websocket request");

                    return;
                }

                WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                await WebSocketEchoHelper.ProcessHeadersRequest(
                    socket,
                    context.Request.Headers.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.ToString())));

            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }
    }
}
