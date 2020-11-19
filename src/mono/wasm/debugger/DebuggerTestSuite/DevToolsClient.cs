// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class DevToolsClient : IDisposable
    {
        DevToolsQueue _queue;
        ClientWebSocket socket;
        List<Task> pending_ops = new List<Task>();
        TaskCompletionSource<bool> side_exit = new TaskCompletionSource<bool>();
        TaskCompletionSource _pendingOpsChanged = new TaskCompletionSource();
        protected readonly ILogger logger;

        public DevToolsClient(ILogger logger)
        {
            this.logger = logger;
        }

        ~DevToolsClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task Close(CancellationToken cancellationToken)
        {
            if (socket.State == WebSocketState.Open)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                socket.Dispose();
        }

        async Task<string> ReadOne(CancellationToken token)
        {
            byte[] buff = new byte[4000];
            var mem = new MemoryStream();
            while (true)
            {
                var result = await this.socket.ReceiveAsync(new ArraySegment<byte>(buff), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                if (result.EndOfMessage)
                {
                    mem.Write(buff, 0, result.Count);
                    return Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
                }
                else
                {
                    mem.Write(buff, 0, result.Count);
                }
            }
        }

        protected void Send(byte[] bytes, CancellationToken token)
        {
            var sendTask = _queue.Send(bytes, token);
            if (sendTask != null)
                pending_ops.Add(sendTask);
        }

        async Task MarkCompleteAfterward(Func<CancellationToken, Task> send, CancellationToken token)
        {
            try
            {
                await send(token);
                side_exit.SetResult(true);
            }
            catch (Exception e)
            {
                side_exit.SetException(e);
            }
        }

        protected async Task<bool> ConnectWithMainLoops(
            Uri uri,
            Func<string, CancellationToken, Task> receive,
            Func<CancellationToken, Task> send,
            CancellationToken token)
        {

            logger.LogDebug("connecting to {0}", uri);
            this.socket = new ClientWebSocket();
            this.socket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;

            await this.socket.ConnectAsync(uri, token);
            _queue = new DevToolsQueue(socket);

            pending_ops.Add(ReadOne(token));
            pending_ops.Add(_pendingOpsChanged.Task);
            pending_ops.Add(side_exit.Task);
            pending_ops.Add(MarkCompleteAfterward(send, token));

            while (!token.IsCancellationRequested)
            {
                var task = await Task.WhenAny(pending_ops);
                if (task == pending_ops[0])
                { //pending_ops[0] is for message reading
                    var msg = ((Task<string>)task).Result;
                    pending_ops[0] = ReadOne(token);
                    Task tsk = receive(msg, token);
                    if (tsk != null)
                        pending_ops.Add(tsk);
                }
                else if (task == pending_ops[1])
                {
                    // pending ops changed
                    _pendingOpsChanged = new TaskCompletionSource();
                    pending_ops[1] = _pendingOpsChanged.Task;
                }
                else if (task == pending_ops[2])
                {
                    var res = ((Task<bool>)task).Result;
                    //it might not throw if exiting successfull
                    return res;
                }
                else
                { //must be a background task
                    pending_ops.Remove(task);
                    if (_queue.TryPump(token, out Task sendTask))
                        pending_ops.Add(sendTask);
                }
            }

            return false;
        }
    }
}
