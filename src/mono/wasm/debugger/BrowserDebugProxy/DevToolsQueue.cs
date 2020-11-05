// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class DevToolsQueue
    {
        private Task current_send;
        private List<byte[]> pending;

        public WebSocket Ws { get; private set; }
        public Task CurrentSend { get { return current_send; } }
        public DevToolsQueue(WebSocket sock)
        {
            this.Ws = sock;
            pending = new List<byte[]>();
        }

        public Task Send(byte[] bytes, CancellationToken token)
        {
            pending.Add(bytes);
            if (pending.Count == 1)
            {
                if (current_send != null)
                    throw new Exception("current_send MUST BE NULL IF THERE'S no pending send");
                //logger.LogTrace ("sending {0} bytes", bytes.Length);
                current_send = Ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                return current_send;
            }
            return null;
        }

        public Task Pump(CancellationToken token)
        {
            current_send = null;
            pending.RemoveAt(0);

            if (pending.Count > 0)
            {
                if (current_send != null)
                    throw new Exception("current_send MUST BE NULL IF THERE'S no pending send");

                current_send = Ws.SendAsync(new ArraySegment<byte>(pending[0]), WebSocketMessageType.Text, true, token);
                return current_send;
            }
            return null;
        }
    }
}
