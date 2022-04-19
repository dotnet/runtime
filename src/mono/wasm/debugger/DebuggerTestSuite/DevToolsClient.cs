// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        protected TaskCompletionSource _clientInitiatedClose = new TaskCompletionSource();
        TaskCompletionSource _shutdownRequested = new TaskCompletionSource();
        readonly TaskCompletionSource<Exception> _failRequested = new();
        TaskCompletionSource _newSendTaskAvailable = new ();
        protected readonly ILogger logger;
        protected bool _useWebSockets = true;

        public event EventHandler<(RunLoopStopReason reason, Exception ex)> RunLoopStopped;

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
            _shutdownRequested.SetResult();
       }

        public void Fail(Exception exception)
        {
            if (_failRequested.Task.IsCompleted)
                logger.LogError($"Fail requested again with {exception}");
            else
                _failRequested.TrySetResult(exception);
        }

        // FIXME: AJ: shutdownrequested - handle that in ReadOne also

#if false
        protected async Task<string> ReadOne(CancellationToken token)
        {
            byte[] buff = new byte[4000];
            var mem = new MemoryStream();
            while (true)
            {
                if (socket.State != WebSocketState.Open)
                {
                    logger.LogDebug($"Socket is no longer open");
                    _clientInitiatedClose.TrySetResult();
                    return null;
                }

                WebSocketReceiveResult result;
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buff), token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested || _shutdownRequested.Task.IsCompletedSuccessfully)
                        return null;

                    logger.LogDebug($"DevToolsClient.ReadOne threw {ex.Message}, token: {token.IsCancellationRequested}, _shutdown: {_shutdownRequested.Task.Status}, clientInitiated: {_clientInitiatedClose.Task.Status}");
                    throw;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _clientInitiatedClose.TrySetResult();
                    return null;
                }

                mem.Write(buff, 0, result.Count);
                if (result.EndOfMessage)
                {
                    return Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
                }
            }
        }
#endif

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
            CancellationToken token)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _conn = await SetupConnection(uri, token);
            _queue = new DevToolsQueue(_conn);

            _ = Task.Run(async () =>
            {
                try
                {
                    RunLoopStopReason reason;
                    Exception exception;

                    try
                    {
                        (reason, exception) = await RunLoop(receive, linkedCts);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"RunLoop threw an exception. (parentToken: {token.IsCancellationRequested}, linked: {linkedCts.IsCancellationRequested}): {ex} ");
                        RunLoopStopped?.Invoke(this, (RunLoopStopReason.Exception, ex));
                        return;
                    }

                    try
                    {
                        logger.LogDebug($"RunLoop stopped, reason: {reason}. (parentToken: {token.IsCancellationRequested}, linked: {linkedCts.IsCancellationRequested}): {exception?.Message}");
                        RunLoopStopped?.Invoke(this, (reason, exception));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Invoking RunLoopStopped event failed for (reason: {reason}, exception: {exception})");
                    }
                }
                finally
                {
                    linkedCts.Cancel();
                    _conn?.Dispose();
                    if (_conn is DevToolsDebuggerConnection wsc)
                        logger.LogDebug($"Loop ended with socket: {wsc.WebSocket.State}");
                    else
                        logger.LogDebug($"Loop ended");
                }
            });
        }

        private async Task<(RunLoopStopReason, Exception)> RunLoop(
            Func<string, CancellationToken, Task> receive,
            CancellationTokenSource linkedCts)
        {
            var pending_ops = new List<Task>
            {
                _conn.ReadOne(_clientInitiatedClose, _failRequested, linkedCts.Token),
                _newSendTaskAvailable.Task,
                _clientInitiatedClose.Task,
                _shutdownRequested.Task,
                _failRequested.Task
            };

            // In case we had a Send called already
            if (_queue.TryPumpIfCurrentCompleted(linkedCts.Token, out Task sendTask))
                pending_ops.Add(sendTask);

            while (!linkedCts.IsCancellationRequested)
            {
                var task = await Task.WhenAny(pending_ops).ConfigureAwait(false);

                if (task.IsCanceled && linkedCts.IsCancellationRequested)
                    return (RunLoopStopReason.Cancelled, null);

                if (_shutdownRequested.Task.IsCompleted)
                    return (RunLoopStopReason.Shutdown, null);

                if (_clientInitiatedClose.Task.IsCompleted)
                    return (RunLoopStopReason.ClientInitiatedClose, new TaskCanceledException("Proxy closed the connection"));

                if (_failRequested.Task.IsCompleted)
                    return (RunLoopStopReason.Exception, _failRequested.Task.Result);

                if (_newSendTaskAvailable.Task.IsCompleted)
                {
                    // Just needed to wake up. the new task has already
                    // been added to pending_ops
                    _newSendTaskAvailable = new ();
                    pending_ops[1] = _newSendTaskAvailable.Task;

                    _queue.TryPumpIfCurrentCompleted(linkedCts.Token, out _);
                    if (_queue.CurrentSend != null)
                        pending_ops.Add(_queue.CurrentSend);
                }

                if (task == pending_ops[0])
                {
                    var msg = await (Task<string>)pending_ops[0];
                    pending_ops[0] = _conn.ReadOne(_clientInitiatedClose, _failRequested, linkedCts.Token);

                    if (msg != null)
                    {
                        Task tsk = receive(msg, linkedCts.Token);
                        if (tsk != null)
                            pending_ops.Add(tsk);
                    }
                }
                else
                {
                    //must be a background task
                    pending_ops.Remove(task);
                    if (task == _queue.CurrentSend && _queue.TryPumpIfCurrentCompleted(linkedCts.Token, out sendTask))
                        pending_ops.Add(sendTask);
                }
            }

            if (linkedCts.IsCancellationRequested)
                return (RunLoopStopReason.Cancelled, null);

            return (RunLoopStopReason.Exception, new InvalidOperationException($"This shouldn't ever get thrown. Unsure why the loop stopped"));
        }
    }

    internal enum RunLoopStopReason
    {
        Shutdown,
        Cancelled,
        Exception,
        ClientInitiatedClose
    }
}
