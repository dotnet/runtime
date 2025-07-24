// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EchoWebSocketHeadersHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            try
            {
                WebSocket socket = await WebSocketAcceptHelper.AcceptAsync(context);
                if (socket is null)
                {
                    return;
                }

                var headers = context.Request.Headers.Select(
                        h => new KeyValuePair<string, string>(h.Key, h.Value.ToString()));

                await WebSocketEchoHelper.RunEchoHeaders(socket, headers);
            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }
    }
}
