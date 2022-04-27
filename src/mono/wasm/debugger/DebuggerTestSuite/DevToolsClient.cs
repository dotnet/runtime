// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

namespace DebuggerTests
{
    internal class DevToolsClient : IDisposable
    {
        DevToolsQueue _queue;
        protected WasmDebuggerConnection _conn;
        TaskCompletionSource _shutdownRequested = new TaskCompletionSource();
        readonly TaskCompletionSource<Exception> _failRequested = new();
        TaskCompletionSource _newSendTaskAvailable = new ();
        protected readonly ILogger logger;

        public event EventHandler<RunLoopExitState> RunLoopStopped;

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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _conn.Dispose();
        }

        public async Task Shutdown(CancellationToken cancellationToken)
        {
            if (_shutdownRequested.Task.IsCompleted)
            {
                logger.LogDebug($"Shutdown was already requested once. Ignoring");
                return;
            }

            await _conn.ShutdownAsync(cancellationToken);
            _shutdownRequested.TrySetResult();
       }

        public void Fail(Exception exception)
        {
            if (_failRequested.Task.IsCompleted)
                logger.LogError($"Fail requested again with {exception}");
            else
                _failRequested.TrySetResult(exception);
        }

        protected void Send(byte[] bytes, CancellationToken token)
        {
            Task sendTask = _queue.Send(bytes, token);
            if (sendTask != null)
                _newSendTaskAvailable.TrySetResult();
        }

        protected async Task<ClientWebSocket> ConnectToWebServer(Uri uri, CancellationToken token)
        {
            // connects to the webserver to start the proxy
            ClientWebSocket clientSocket = new ();
            clientSocket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            logger.LogDebug("Client connecting to {0}", uri);
            await clientSocket.ConnectAsync(uri, token);
            return clientSocket;
        }

        protected virtual Task<WasmDebuggerConnection> SetupConnection(Uri webserverUri, CancellationToken token)
            => throw new NotImplementedException();

        protected async Task ConnectWithMainLoops(
            Uri uri,
            Func<string, CancellationToken, Task> receive,
            CancellationTokenSource cts)
        {
            CancellationToken token = cts.Token;
            _conn = await SetupConnection(uri, token);
            _queue = new DevToolsQueue(_conn);

            _ = Task.Run(async () =>
            {
                try
                {
                    RunLoopExitState exitState;

                    try
                    {
                        exitState = await RunLoop(receive, cts);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"RunLoop threw an exception. (parentToken: {token.IsCancellationRequested}, linked: {cts.IsCancellationRequested}): {ex} ");
                        RunLoopStopped?.Invoke(this, new(RunLoopStopReason.Exception, ex));
                        return;
                    }

                    try
                    {
                        logger.LogDebug($"RunLoop stopped, reason: {exitState}. (parentToken: {token.IsCancellationRequested}, linked: {cts.IsCancellationRequested}): {exitState.exception?.Message}");
                        RunLoopStopped?.Invoke(this, exitState);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Invoking RunLoopStopped event failed for ({exitState}) with {ex}");
                    }
                }
                finally
                {
                    cts.Cancel();
                    _conn?.Dispose();
                    if (_conn is DevToolsDebuggerConnection wsc)
                        logger.LogDebug($"Loop ended with socket: {wsc.WebSocket.State}");
                    else
                        logger.LogDebug($"Loop ended");
                }
            });
        }

        private async Task<RunLoopExitState> RunLoop(
            Func<string, CancellationToken, Task> receive,
            CancellationTokenSource cts)
        {
            var pending_ops = new List<Task>
            {
                _conn.ReadOneAsync(cts.Token),
                _newSendTaskAvailable.Task,
                _shutdownRequested.Task,
                _failRequested.Task
            };

            // In case we had a Send called already
            if (_queue.TryPumpIfCurrentCompleted(cts.Token, out Task sendTask))
                pending_ops.Add(sendTask);

            while (!cts.IsCancellationRequested)
            {
                var task = await Task.WhenAny(pending_ops).ConfigureAwait(false);

                if (_shutdownRequested.Task.IsCompleted)
                    return new(RunLoopStopReason.Shutdown, null);

                if (task.IsCanceled && cts.IsCancellationRequested)
                    return new(RunLoopStopReason.Cancelled, null);

                if (task.IsFaulted)
                {
                    if (task == pending_ops[0] && !_conn.IsConnected)
                        return new(RunLoopStopReason.ProxyConnectionClosed, task.Exception);

                    return new(RunLoopStopReason.Exception, task.Exception);
                }
                if (_failRequested.Task.IsCompleted)
                    return new(RunLoopStopReason.Exception, _failRequested.Task.Result);

                // FIXME: instead of this, iterate through pending_ops, and clear it
                // out every time we wake up
                if (pending_ops.Where(t => t.IsFaulted).FirstOrDefault() is Task faultedTask)
                    return new(RunLoopStopReason.Exception, faultedTask.Exception);

                if (_newSendTaskAvailable.Task.IsCompleted)
                {
                    // Just needed to wake up. the new task has already
                    // been added to pending_ops
                    _newSendTaskAvailable = new ();
                    pending_ops[1] = _newSendTaskAvailable.Task;

                    _queue.TryPumpIfCurrentCompleted(cts.Token, out _);
                    if (_queue.CurrentSend != null)
                        pending_ops.Add(_queue.CurrentSend);
                }

                if (task == pending_ops[0])
                {
                    var msg = await (Task<string>)pending_ops[0];
                    pending_ops[0] = _conn.ReadOneAsync(cts.Token);

                    if (msg != null)
                    {
                        Task tsk = receive(msg, cts.Token);
                        if (tsk != null)
                            pending_ops.Add(tsk);
                    }
                }
                else
                {
                    //must be a background task
                    pending_ops.Remove(task);
                    if (task == _queue.CurrentSend && _queue.TryPumpIfCurrentCompleted(cts.Token, out sendTask))
                        pending_ops.Add(sendTask);
                }
            }

            if (cts.IsCancellationRequested)
                return new(RunLoopStopReason.Cancelled, null);

            return new(RunLoopStopReason.Exception, new InvalidOperationException($"This shouldn't ever get thrown. Unsure why the loop stopped"));
        }
    }
}
