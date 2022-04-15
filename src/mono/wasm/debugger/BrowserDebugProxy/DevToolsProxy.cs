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
        protected TaskCompletionSource<bool> side_exception = new TaskCompletionSource<bool>();
        protected TaskCompletionSource client_initiated_close = new TaskCompletionSource();
        protected Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>>();
        protected AbstractConnection browser;
        protected AbstractConnection ide;
        internal int next_cmd_id;
        internal readonly ChannelWriter<Task> _channelWriter;
        internal readonly ChannelReader<Task> _channelReader;
        internal List<DevToolsQueue> queues = new List<DevToolsQueue>();

        protected readonly ILogger logger;

        public DevToolsProxy(ILoggerFactory loggerFactory, string loggerId)
        {
            string loggerSuffix = string.IsNullOrEmpty(loggerId) ? string.Empty : $"-{loggerId}";
            logger = loggerFactory.CreateLogger($"{nameof(DevToolsProxy)}{loggerSuffix}");

            var channel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true });
            _channelWriter = channel.Writer;
            _channelReader = channel.Reader;
        }

        protected virtual Task<bool> AcceptEvent(SessionId sessionId, JObject args, CancellationToken token)
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> AcceptCommand(MessageId id, JObject args, CancellationToken token)
        {
            return Task.FromResult(false);
        }

        private DevToolsQueue GetQueueForConnection(AbstractConnection conn)
            => queues.FirstOrDefault(q => q.Connection == conn);

        protected DevToolsQueue GetQueueForTask(Task task)
        {
            return queues.FirstOrDefault(q => q.CurrentSend == task);
        }

        protected async Task Send(AbstractConnection conn, JObject o, CancellationToken token)
        {
            var msg = o.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(msg);

            DevToolsQueue queue = GetQueueForConnection(conn);

            Task task = queue.Send(bytes, token);
            if (task != null)
                await _channelWriter.WriteAsync(task, token);
        }

        internal virtual async Task OnEvent(SessionId sessionId, JObject parms, CancellationToken token)
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
                side_exception.TrySetException(e);
            }
        }

        internal virtual async Task OnCommand(MessageId id, JObject parms, CancellationToken token)
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
                side_exception.TrySetException(e);
            }
        }

        internal virtual void OnResponse(MessageId id, Result result)
        {
            //logger.LogTrace ("got id {0} res {1}", id, result);
            // Fixme
            if (pending_cmds.Remove(id, out TaskCompletionSource<Result> task))
            {
                task.SetResult(result);
                return;
            }
            logger.LogError("Cannot respond to command: {id} with result: {result} - command is not pending", id, result);
        }

        internal virtual Task ProcessBrowserMessage(string msg, CancellationToken token)
        {
            logger.LogDebug($"* ProcessBrowserMessage: {msg}");
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

        internal virtual Task ProcessIdeMessage(string msg, CancellationToken token)
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

        internal virtual async Task<Result> SendCommand(SessionId id, string method, JObject args, CancellationToken token)
        {
            //Log ("verbose", $"sending command {method}: {args}");
            return await SendCommandInternal(id, method, args, token);
        }

        internal virtual async Task<Result> SendCommandInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            int id = Interlocked.Increment(ref next_cmd_id);

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
            //Log ("verbose", $"add cmd id {sessionId}-{id}");
            pending_cmds[msgId] = tcs;

            await Send(browser, o, token);
            return await tcs.Task;
        }

        internal virtual Task SendEvent(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            //Log ("verbose", $"sending event {method}: {args}");
            return SendEventInternal(sessionId, method, args, token);
        }

        internal virtual Task SendEventInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
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

        internal virtual void SendResponse(MessageId id, Result result, CancellationToken token)
        {
            SendResponseInternal(id, result, token);
        }

        internal virtual Task SendResponseInternal(MessageId id, Result result, CancellationToken token)
        {
            JObject o = result.ToJObject(id);
            if (!result.IsOk)
                logger.LogError($"sending error response for id: {id} -> {result}");

            return Send(this.ide, o, token);
        }

        public async Task Run(Uri browserUri, WebSocket ideSocket)
        {
            try
            {
                Log("debug", $"DevToolsProxy: Starting for browser at {browserUri}");
                Log("verbose", $"DevToolsProxy: Proxy waiting for connection to the browser at {browserUri}");

                ClientWebSocket browserSocket = new();
                browserSocket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
                await browserSocket.ConnectAsync(browserUri, CancellationToken.None);

                using var ideConn = new WebSocketConnection(ideSocket, logger);
                using var browserConn = new WebSocketConnection(browserSocket, logger);

                await RunInternal(ideConn: ideConn, browserConn: browserConn);
            }
            catch (Exception ex)
            {
                logger.LogError($"DevToolsProxy.Run: {ex}");
                throw;
            }
        }

        protected async Task RunInternal(AbstractConnection ideConn, AbstractConnection browserConn)
        {
            using (ide = ideConn)
            {
                queues.Add(new DevToolsQueue(ide));
                using (browser = browserConn)
                {
                    queues.Add(new DevToolsQueue(browser));
                    var x = new CancellationTokenSource();

                    List<Task> pending_ops = new();

                    pending_ops.Add(browser.ReadOne(client_initiated_close, x.Token));
                    pending_ops.Add(ide.ReadOne(client_initiated_close, x.Token));
                    pending_ops.Add(side_exception.Task);
                    pending_ops.Add(client_initiated_close.Task);
                    Task<bool> readerTask = _channelReader.WaitToReadAsync(x.Token).AsTask();
                    pending_ops.Add(readerTask);

                    try
                    {
                        while (!x.IsCancellationRequested)
                        {
                            Task completedTask = await Task.WhenAny(pending_ops.ToArray());

                            if (client_initiated_close.Task.IsCompleted)
                            {
                                await client_initiated_close.Task.ConfigureAwait(false);
                                // FIXME: add browseruri to the connection?
                                // Log("verbose", $"DevToolsProxy: Client initiated close from {browserUri}");
                                Log("verbose", $"DevToolsProxy: Client initiated close from browserUri");
                                x.Cancel();

                                break;
                            }

                            if (readerTask.IsCompleted)
                            {
                                while (_channelReader.TryRead(out Task newTask))
                                {
                                    pending_ops.Add(newTask);
                                }

                                pending_ops[4] = _channelReader.WaitToReadAsync(x.Token).AsTask();
                            }

                            //logger.LogTrace ("pump {0} {1}", task, pending_ops.IndexOf (task));
                            if (completedTask == pending_ops[0])
                            {
                                string msg = ((Task<string>)completedTask).Result;
                                if (msg != null)
                                {
                                    // pending_ops[0] = ReadOne(browser, x.Token); //queue next read
                                    pending_ops[0] = browser.ReadOne(client_initiated_close, x.Token);
                                    Task newTask = ProcessBrowserMessage(msg, x.Token);
                                    if (newTask != null)
                                        pending_ops.Add(newTask);
                                }
                            }
                            else if (completedTask == pending_ops[1])
                            {
                                string msg = ((Task<string>)completedTask).Result;
                                if (msg != null)
                                {
                                    // pending_ops[1] = ReadOne(ide, x.Token); //queue next read
                                    pending_ops[1] = ide.ReadOne(client_initiated_close, x.Token);
                                    Task newTask = ProcessIdeMessage(msg, x.Token);
                                    if (newTask != null)
                                        pending_ops.Add(newTask);
                                }
                            }
                            else if (completedTask == pending_ops[2])
                            {
                                bool res = ((Task<bool>)completedTask).Result;
                                throw new Exception("side task must always complete with an exception, what's going on???");
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
                    }
                    catch (Exception e)
                    {
                        Log("error", $"DevToolsProxy::Run: Exception {e}");
                        _channelWriter.Complete(e);
                        //throw;
                    }
                    finally
                    {
                        if (!x.IsCancellationRequested)
                            x.Cancel();
                    }
                }
            }
        }

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
