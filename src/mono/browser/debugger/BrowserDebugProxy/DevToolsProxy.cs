// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        protected Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>>();
        protected DevToolsQueue browser;
        protected DevToolsQueue ide;
        private int next_cmd_id;
        protected readonly ILogger logger;
        protected RunLoop _runLoop;
        private readonly string _loggerId;

        public event EventHandler<RunLoopExitState> RunLoopStopped;
        public bool IsRunning => _runLoop?.IsRunning == true;
        public RunLoopExitState Stopped => _runLoop?.StoppedState;

        protected readonly ProxyOptions _options;
        public DevToolsProxy(ProxyOptions options, ILogger logger, string loggerId)
        {
            _loggerId = loggerId;
            _options = options;
            this.logger = logger;
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

        protected Task Send(DevToolsQueue queue, JObject o, CancellationToken token)
        {
            Log("protocol", $"to-{queue.Id}: {GetFromOrTo(o)} {o}");
            var msg = o.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(msg);

            return _runLoop.Send(bytes, token, queue);
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
                _runLoop.Fail(e);
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
                _runLoop.Fail(e);
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
                _runLoop.Fail(ex);
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
                _runLoop.Fail(ex);
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
                logger.LogDebug($"sending error response for id: {id} -> {result}");

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
                var proxy = WebRequest.DefaultWebProxy;
                if (_options is not null && _options.IgnoreProxyForLocalAddress && proxy is not null && !proxy.IsBypassed(browserUri)) //only bypass the proxy for local addresses if it is not already an exception in the OS settings
                    browserSocket.Options.Proxy = new WebProxy(proxy.GetProxy(browserUri), true);
                await browserSocket.ConnectAsync(browserUri, cts.Token);

                using var ideConn = new DevToolsDebuggerConnection(ideSocket, "ide", logger);
                using var browserConn = new DevToolsDebuggerConnection(browserSocket, "browser", logger);

                await RunLoopAsync(ideConn: ideConn, browserConn: browserConn, cts);
            }
            catch (Exception ex)
            {
                logger.LogError($"DevToolsProxy.Run: {ex}");
                throw;
            }
        }

        protected async Task RunLoopAsync(WasmDebuggerConnection ideConn, WasmDebuggerConnection browserConn, CancellationTokenSource cts)
        {
            try
            {
                this.ide = new DevToolsQueue(ideConn);
                this.browser = new DevToolsQueue(browserConn);
                ideConn.OnReadAsync = ProcessIdeMessage;
                browserConn.OnReadAsync = ProcessBrowserMessage;
                _runLoop = new(new[] { ide, browser }, logger);
                _runLoop.RunLoopStopped += RunLoopStopped;
                await _runLoop.RunAsync(cts);
            }
            finally
            {
                _runLoop?.Dispose();
                _runLoop = null;
            }
        }

        public virtual void Shutdown() => _runLoop?.Shutdown();
        public void Fail(Exception exception) => _runLoop?.Fail(exception);

        protected virtual string GetFromOrTo(JObject o) => string.Empty;

        protected void Log(string priority, string msg)
        {
            if (priority == "protocol")
                msg = msg.TruncateLogMessage();

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
