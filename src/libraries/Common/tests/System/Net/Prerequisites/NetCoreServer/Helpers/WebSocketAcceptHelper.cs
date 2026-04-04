// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public static class WebSocketAcceptHelper
    {
        public static async Task<WebSocket> AcceptAsync(HttpContext context, string subProtocol = null)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Not a websocket request");
                return null;
            }

            if (!string.IsNullOrEmpty(subProtocol))
            {
                return await context.WebSockets.AcceptWebSocketAsync(subProtocol);
            }

            return await context.WebSockets.AcceptWebSocketAsync();
        }
    }
}
