// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EchoWebSocketHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            var queryString = context.Request.QueryString.ToUriComponent(); // Returns empty string if request URI has no query
            WebSocketEchoOptions options = await WebSocketEchoHelper.ProcessOptions(queryString);
            try
            {
                WebSocket socket = await WebSocketAcceptHelper.AcceptAsync(context, options.SubProtocol);
                if (socket is null)
                {
                    return;
                }

                await WebSocketEchoHelper.RunEchoAll(
                    socket, options.ReplyWithPartialMessages, options.ReplyWithEnhancedCloseMessage);
            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }
    }
}
