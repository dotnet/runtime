// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class DevToolsQueue
    {
        protected Task? current_send;
        protected ConcurrentQueue<byte[]> pending;

        public Task? CurrentSend { get { return current_send; } }

        public AbstractConnection Connection { get; init; }

        public DevToolsQueue(AbstractConnection conn)
        {
            Connection = conn;
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

        public bool TryPumpIfCurrentCompleted(CancellationToken token, [NotNullWhen(true)] out Task? sendTask)
        {
            sendTask = null;

            if (current_send?.IsCompleted == false)
                return false;

            current_send = null;
            if (pending.TryDequeue(out byte[]? bytes))
            {
                current_send = Connection.SendAsync(bytes, token);
                sendTask = current_send;
            }

            return sendTask != null;
        }
    }
}
