// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class DevToolsQueueBase
    {
        protected Task? current_send;
        protected ConcurrentQueue<byte[]> pending;

        public Task? CurrentSend { get { return current_send; } }

        public static DevToolsQueueBase Create(TcpClient tcpClient, WebSocket webSocket)
        {
            if (tcpClient is null && webSocket is null)
                throw new ArgumentException($"Both {nameof(tcpClient)}, and {nameof(webSocket)} cannot be null");
            if (tcpClient is not null && webSocket is not null)
                throw new ArgumentException($"Both {nameof(tcpClient)}, and {nameof(webSocket)} cannot be non-null");

            return tcpClient is not null
                        ? new DevToolsQueueFirefox(tcpClient)
                        : new DevToolsQueue(webSocket!);
        }

        protected DevToolsQueueBase()
        {
            pending = new ConcurrentQueue<byte[]>();
        }

        public Task? Send(byte[] bytes, CancellationToken token)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            pending.Enqueue(bytes);
            TryPumpIfCurrentCompleted(token, out Task? sendTask);
            return sendTask;
        }

        public virtual bool TryPumpIfCurrentCompleted(CancellationToken token, [NotNullWhen(true)] out Task? sendTask)
        {
            sendTask = null;
            return false;
        }
    }

    internal class DevToolsQueue : DevToolsQueueBase
    {
        public WebSocket Ws { get; private set; }

        public DevToolsQueue(WebSocket sock)
        {
            this.Ws = sock;
            // pending = new ConcurrentQueue<byte[]>();
        }

        public override bool TryPumpIfCurrentCompleted(CancellationToken token, [NotNullWhen(true)] out Task? sendTask)
        {
            sendTask = null;

            if (current_send?.IsCompleted == false)
                return false;

            current_send = null;
            if (pending.TryDequeue(out byte[]? bytes))
            {
                current_send = Ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                sendTask = current_send;
            }

            return sendTask != null;
        }
    }

    internal class DevToolsQueueFirefox : DevToolsQueueBase
    {
        public TcpClient Tc { get; private set; }

        public DevToolsQueueFirefox(TcpClient tc)
        {
            this.Tc = tc;
        }

        public override bool TryPumpIfCurrentCompleted(CancellationToken token, [NotNullWhen(true)] out Task? sendTask)
        {
            sendTask = null;

            if (current_send?.IsCompleted == false)
                return false;

            current_send = null;
            if (pending.TryDequeue(out byte[]? bytes))
            {
                NetworkStream toStream = Tc.GetStream();

                current_send = toStream.WriteAsync(bytes, token).AsTask();
                sendTask = current_send;
            }

            return sendTask != null;
        }
    }
}
