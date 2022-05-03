// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class DevToolsProxy
    {
        protected TaskCompletionSource<Exception> side_exception = new();
        protected TaskCompletionSource shutdown_requested = new();
        protected Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>>();
        protected WasmDebuggerConnection browser;
        protected WasmDebuggerConnection ide;
        private int next_cmd_id;
        private readonly ChannelWriter<Task> _channelWriter;
        private readonly ChannelReader<Task> _channelReader;
        protected List<DevToolsQueue> queues = new List<DevToolsQueue>();

        protected readonly ILogger logger;
        private readonly string _loggerId;

        public event EventHandler<RunLoopExitState> RunLoopStopped;
        public bool IsRunning => Stopped is null;
        public RunLoopExitState Stopped { get; private set; }

        public DevToolsProxy(ILoggerFactory loggerFactory, string loggerId)
        {
            _loggerId = loggerId;
            string loggerSuffix = string.IsNullOrEmpty(loggerId) ? string.Empty : $"-{loggerId}";
            logger = loggerFactory.CreateLogger($"DevToolsProxy{loggerSuffix}");

            var channel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true });
            _channelWriter = channel.Writer;
            _channelReader = channel.Reader;
        }

        protected int GetNewCmdId() => Interlocked.Increment(ref next_cmd_id);
        protected int ResetCmdId() => next_cmd_id = 0;
        protected virtual Task<bool> AcceptEvent(SessionId sessionId, JObject args, CancellationToken token)
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> AcceptCommand(MessageId id, JObject args, CancellationToken token)
        {
            return Task.FromResult(false);
        }

        private DevToolsQueue GetQueueForConnection(WasmDebuggerConnection conn)
            => queues.FirstOrDefault(q => q.Connection == conn);

        protected DevToolsQueue GetQueueForTask(Task task)
        {
            return queues.FirstOrDefault(q => q.CurrentSend == task);
        }

        protected async Task Send(WasmDebuggerConnection conn, JObject o, CancellationToken token)
        {
            logger.LogTrace($"to-{conn.Id}: {GetFromOrTo(o)} {o}");
            var msg = o.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(msg);

            DevToolsQueue queue = GetQueueForConnection(conn);

            Task task = queue.Send(bytes, token);
            if (task != null)
                await _channelWriter.WriteAsync(task, token);
        }

        protected virtual async Task OnEvent(SessionId sessionId, JObject parms, CancellationToken token)
        {
            try
            {
                if (!await AcceptEvent(sessionId, parms, token))
                {
                    var method = parms["method"].Value<string>();
                    var args = parms["params"] as JObject;
                    //logger.LogDebug ("proxy browser: {0}::{1}",method, args);
                    await SendEventInternal(sessionId, method, args, token);
                }
            }
            catch (Exception e)
            {
                side_exception.TrySetResult(e);
            }
        }

        protected virtual async Task OnCommand(MessageId id, JObject parms, CancellationToken token)
        {
            try
            {
                if (!await AcceptCommand(id, parms, token))
                {
                    var method = parms["method"].Value<string>();
                    var args = parms["params"] as JObject;
                    Result res = await SendCommandInternal(id, method, args, token);
                    await SendResponseInternal(id, res, token);
                }
            }
            catch (Exception e)
            {
                side_exception.TrySetResult(e);
            }
        }

        protected virtual void OnResponse(MessageId id, Result result)
        {
            //logger.LogTrace ("got id {0} res {1}", id, result);
            // Fixme
            if (pending_cmds.Remove(id, out TaskCompletionSource<Result> task))
            {
                task.SetResult(result);
                return;
            }
            logger.LogError($"Cannot respond to command: {id} with result: {result} - command is not pending");
        }

        protected virtual Task ProcessBrowserMessage(string msg, CancellationToken token)
        {
            try
            {
                var res = JObject.Parse(msg);

                //if (method != "Debugger.scriptParsed" && method != "Runtime.consoleAPICalled")
                Log("protocol", $"browser: {msg}");

                if (res["id"] == null)
                {
                    return OnEvent(res.ToObject<SessionId>(), res, token);
                }
                else
                {
                    OnResponse(res.ToObject<MessageId>(), Result.FromJson(res));
                    return null;
                }
            }
            catch (Exception ex)
            {
                side_exception.TrySetResult(ex);
                throw;
            }
        }

        protected virtual Task ProcessIdeMessage(string msg, CancellationToken token)
        {
            try
            {
                Log("protocol", $"ide: {msg}");
                if (!string.IsNullOrEmpty(msg))
                {
                    var res = JObject.Parse(msg);
                    var id = res.ToObject<MessageId>();
                    return OnCommand(
                        id,
                        res,
                        token);
                }

                return null;
            }
            catch (Exception ex)
            {
                side_exception.TrySetResult(ex);
                throw;
            }
        }

        public virtual async Task<Result> SendCommand(SessionId id, string method, JObject args, CancellationToken token)
        {
            // Log ("protocol", $"sending command {method}: {args}");
            return await SendCommandInternal(id, method, args, token);
        }

        protected virtual async Task<Result> SendCommandInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            int id = GetNewCmdId();

            var o = JObject.FromObject(new
            {
                id,
                method,
                @params = args
            });
            if (sessionId.sessionId != null)
                o["sessionId"] = sessionId.sessionId;
            var tcs = new TaskCompletionSource<Result>();

            var msgId = new MessageId(sessionId.sessionId, id);
            pending_cmds[msgId] = tcs;

            await Send(browser, o, token);
            return await tcs.Task;
        }

        public virtual Task SendEvent(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            // logger.LogTrace($"sending event {method}: {args}");
            return SendEventInternal(sessionId, method, args, token);
        }

        protected virtual Task SendEventInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            var o = JObject.FromObject(new
            {
                method,
                @params = args
            });
            if (sessionId.sessionId != null)
                o["sessionId"] = sessionId.sessionId;

            return Send(ide, o, token);
        }

        public virtual void SendResponse(MessageId id, Result result, CancellationToken token)
        {
            SendResponseInternal(id, result, token);
        }

        protected virtual Task SendResponseInternal(MessageId id, Result result, CancellationToken token)
        {
            JObject o = result.ToJObject(id);
            if (!result.IsOk)
                logger.LogError($"sending error response for id: {id} -> {result}");

            return Send(this.ide, o, token);
        }

        public virtual Task ForwardMessageToIde(JObject msg, CancellationToken token)
        {
            // logger.LogTrace($"to-ide: forwarding {GetFromOrTo(msg)} {msg}");
            return Send(ide, msg, token);
        }

        public virtual Task ForwardMessageToBrowser(JObject msg, CancellationToken token)
        {
            // logger.LogTrace($"to-browser: forwarding {GetFromOrTo(msg)} {msg}");
            return Send(this.browser, msg, token);
        }

        public async Task RunForDevTools(Uri browserUri, WebSocket ideSocket, CancellationTokenSource cts)
        {
            try
            {
                logger.LogDebug($"DevToolsProxy: Starting for browser at {browserUri}");
                logger.LogDebug($"DevToolsProxy: Proxy waiting for connection to the browser at {browserUri}");

                ClientWebSocket browserSocket = new();
                browserSocket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
                await browserSocket.ConnectAsync(browserUri, cts.Token);

                using var ideConn = new DevToolsDebuggerConnection(ideSocket, "ide", logger);
                using var browserConn = new DevToolsDebuggerConnection(browserSocket, "browser", logger);

                await StartRunLoop(ideConn: ideConn, browserConn: browserConn, cts);
            }
            catch (Exception ex)
            {
                logger.LogError($"DevToolsProxy.Run: {ex}");
                throw;
            }
        }

        protected Task StartRunLoop(WasmDebuggerConnection ideConn, WasmDebuggerConnection browserConn, CancellationTokenSource cts)
            => Task.Run(async () =>
            {
                try
                {
                    RunLoopExitState exitState;

                    try
                    {
                        Stopped = await RunLoopActual(ideConn, browserConn, cts);
                        exitState = Stopped;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"RunLoop threw an exception: {ex}");
                        Stopped = new(RunLoopStopReason.Exception, ex);
                        RunLoopStopped?.Invoke(this, Stopped);
                        return;
                    }

                    try
                    {
                        logger.LogDebug($"RunLoop stopped, reason: {exitState}");
                        RunLoopStopped?.Invoke(this, Stopped);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Invoking RunLoopStopped event ({exitState}) failed with {ex}");
                    }
                }
                finally
                {
                    ideConn?.Dispose();
                    browserConn?.Dispose();
                }
            });


        private async Task<RunLoopExitState> RunLoopActual(WasmDebuggerConnection ideConn,
                                                           WasmDebuggerConnection browserConn,
                                                           CancellationTokenSource cts)
        {
            using (ide = ideConn)
            {
                queues.Add(new DevToolsQueue(ide));
                using (browser = browserConn)
                {
                    queues.Add(new DevToolsQueue(browser));
                    var x = cts;

                    List<Task> pending_ops = new();

                    pending_ops.Add(browser.ReadOneAsync(x.Token));
                    pending_ops.Add(ide.ReadOneAsync(x.Token));
                    pending_ops.Add(side_exception.Task);
                    pending_ops.Add(shutdown_requested.Task);
                    Task<bool> readerTask = _channelReader.WaitToReadAsync(x.Token).AsTask();
                    pending_ops.Add(readerTask);

                    try
                    {
                        while (!x.IsCancellationRequested)
                        {
                            Task completedTask = await Task.WhenAny(pending_ops.ToArray()).ConfigureAwait(false);

                            if (shutdown_requested.Task.IsCompleted)
                            {
                                x.Cancel();
                                return new(RunLoopStopReason.Shutdown, null);
                            }

                            if (side_exception.Task.IsCompleted)
                                return new(RunLoopStopReason.Exception, await side_exception.Task);

                            if (completedTask.IsFaulted)
                            {
                                if (completedTask == pending_ops[0] && !browser.IsConnected)
                                    return new(RunLoopStopReason.HostConnectionClosed, completedTask.Exception);
                                else if (completedTask == pending_ops[1] && !ide.IsConnected)
                                    return new(RunLoopStopReason.IDEConnectionClosed, completedTask.Exception);

                                return new(RunLoopStopReason.Exception, completedTask.Exception);
                            }

                            if (x.IsCancellationRequested)
                                return new(RunLoopStopReason.Cancelled, null);

                            // FIXME: instead of this, iterate through pending_ops, and clear it
                            // out every time we wake up
                            if (pending_ops.Where(t => t.IsFaulted).FirstOrDefault() is Task faultedTask)
                                return new(RunLoopStopReason.Exception, faultedTask.Exception);

                            if (readerTask.IsCompleted)
                            {
                                while (_channelReader.TryRead(out Task newTask))
                                {
                                    pending_ops.Add(newTask);
                                }

                                pending_ops[4] = _channelReader.WaitToReadAsync(x.Token).AsTask();
                            }

                            // logger.LogDebug("pump {0} {1}", completedTask, pending_ops.IndexOf (completedTask));
                            if (completedTask == pending_ops[0])
                            {
                                string msg = await (Task<string>)completedTask;
                                if (msg != null)
                                {
                                    pending_ops[0] = browser.ReadOneAsync(x.Token);
                                    Task newTask = ProcessBrowserMessage(msg, x.Token);
                                    if (newTask != null)
                                        pending_ops.Add(newTask);
                                }
                            }
                            else if (completedTask == pending_ops[1])
                            {
                                string msg = await (Task<string>)completedTask;
                                if (msg != null)
                                {
                                    pending_ops[1] = ide.ReadOneAsync(x.Token);
                                    Task newTask = ProcessIdeMessage(msg, x.Token);
                                    if (newTask != null)
                                        pending_ops.Add(newTask);
                                }
                            }
                            else if (completedTask == pending_ops[2])
                            {
                                throw await (Task<Exception>)completedTask;
                            }
                            else
                            {
                                //must be a background task
                                pending_ops.Remove(completedTask);
                                DevToolsQueue queue = GetQueueForTask(completedTask);
                                if (queue != null)
                                {
                                    if (queue.TryPumpIfCurrentCompleted(x.Token, out Task tsk))
                                        pending_ops.Add(tsk);
                                }
                            }
                        }

                        _channelWriter.Complete();
                        if (shutdown_requested.Task.IsCompleted)
                            return new(RunLoopStopReason.Shutdown, null);
                        if (x.IsCancellationRequested)
                            return new(RunLoopStopReason.Cancelled, null);

                        return new(RunLoopStopReason.Exception, new InvalidOperationException($"This shouldn't ever get thrown. Unsure why the loop stopped"));
                    }
                    catch (Exception e)
                    {
                        _channelWriter.Complete(e);
                        throw;
                    }
                    finally
                    {
                        if (!x.IsCancellationRequested)
                            x.Cancel();
                        foreach (Task t in pending_ops)
                            logger.LogDebug($"\t{t}: {t.Status}");
                        logger.LogDebug($"browser: {browser.IsConnected}, ide: {ide.IsConnected}");

                        queues?.Clear();
                    }
                }
            }
        }

        public virtual void Shutdown()
        {
            logger.LogDebug($"Proxy.Shutdown, browser: {browser.IsConnected}, ide: {ide.IsConnected}");
            shutdown_requested.TrySetResult();
        }

        public void Fail(Exception exception)
        {
            if (side_exception.Task.IsCompleted)
                logger.LogError($"Fail requested again with {exception}");
            else
                side_exception.TrySetResult(exception);
        }

        protected virtual string GetFromOrTo(JObject o) => string.Empty;

        protected void Log(string priority, string msg)
        {
            switch (priority)
            {
                case "protocol":
                    logger.LogTrace(msg);
                    break;
                case "verbose":
                    logger.LogDebug(msg);
                    break;
                case "error":
                    logger.LogError(msg);
                    break;
                case "info":
                    logger.LogInformation(msg);
                    break;
                case "warning":
                    logger.LogWarning(msg);
                    break;
                default:
                    logger.LogDebug(msg);
                    break;
            }
        }
    }
}
