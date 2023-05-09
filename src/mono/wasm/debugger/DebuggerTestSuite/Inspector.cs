// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Runtime.ExceptionServices;
using Xunit.Abstractions;
using System.Collections.Concurrent;

#nullable enable

namespace DebuggerTests
{
    class Inspector
    {
        // https://console.spec.whatwg.org/#formatting-specifiers
        private static Regex _consoleArgsRegex = new(@"(%[sdifoOc])", RegexOptions.Compiled);

        ConcurrentDictionary<string, TaskCompletionSource<JObject>> notifications = new ();
        ConcurrentDictionary<string, Func<JObject, CancellationToken, Task<ProtocolEventHandlerReturn>>> eventListeners = new ();
        ConcurrentQueue<(string, JObject)> nextNotifications = new (); //in a multithreaded runtime we can receive more than one pause at same time
        public const string PAUSE = "pause";
        public const string APP_READY = "app-ready";
        public CancellationToken Token { get; set; }
        public InspectorClient Client { get; }
        public DebuggerProxyBase? Proxy { get; }
        public bool DetectAndFailOnAssertions { get; set; } = true;

        private CancellationTokenSource _cancellationTokenSource;
        private Exception? _isFailingWithException;
        private bool _gotRuntimeReady = false;
        private bool _gotAppReady = false;

        protected static Lazy<ILoggerFactory> s_loggerFactory = new(() =>
            LoggerFactory.Create(builder =>
            {
                if (TestOptions.LogToConsole)
                {
                    builder
                        .AddSimpleConsole(options =>
                            {
                                options.SingleLine = true;
                                options.TimestampFormat = "[HH:mm:ss] ";
                            })
                           .AddFilter(null, LogLevel.Debug);
                        // .AddFile(logFilePath, minimumLevel: LogLevel.Debug)
                }
            }));

        protected ILogger _logger;
        public int Id { get; init; }

        public Inspector(int testId, ITestOutputHelper testOutput)
        {
            Id = testId;
            _cancellationTokenSource = new CancellationTokenSource();
            Token = _cancellationTokenSource.Token;

            if (Id == 0)
                s_loggerFactory.Value.AddXunit(testOutput);

            _logger = s_loggerFactory.Value
                            .CreateLogger($"{nameof(Inspector)}-{Id}");
            if (DebuggerTestBase.RunningOnChrome)
                Client = new InspectorClient(_logger);
            else
                Client = new FirefoxInspectorClient(_logger);
        }

        public Task<JObject> WaitFor(string what)
        {
            if (notifications.TryGetValue(what, out TaskCompletionSource<JObject>? tcs))
            {
                if (tcs.Task.IsCompleted)
                {
                    Client.CurrentSessionId = new SessionId(tcs.Task.Result["sessionId"]?.Value<string>());
                    notifications.Remove(what, out _);
                    return tcs.Task;
                }

                throw new Exception($"Invalid internal state, waiting for {what} while another wait is already setup");
            }
            else if (nextNotifications.TryDequeue(out (string what, JObject args) notification))
            {
                var n = new TaskCompletionSource<JObject>();
                Client.CurrentSessionId = new SessionId(notification.args["sessionId"]?.Value<string>());
                if (what != notification.what)
                    throw new Exception($"Unexpected different notification type");
                n.SetResult(notification.args);
                return n.Task;
            }
            else
            {
                var n = new TaskCompletionSource<JObject>();
                notifications[what] = n;
                return n.Task;
            }
        }

        public void ClearWaiterFor(string what)
        {
            if (notifications.ContainsKey(what))
                notifications.Remove(what, out _);
        }

        void NotifyOf(string what, JObject args)
        {
            if (notifications.TryGetValue(what, out TaskCompletionSource<JObject>? tcs))
            {
                if (tcs.Task.IsCompleted)
                {
                    nextNotifications.Enqueue((what, args));
                    return;
                    //throw new Exception($"Invalid internal state. Notifying for {what} again, but the previous one hasn't been read.");
                }
                Client.CurrentSessionId = new SessionId(args["sessionId"]?.Value<string>());
                notifications[what].SetResult(args);
                notifications.Remove(what, out _);
            }
            else
            {
                var n = new TaskCompletionSource<JObject>();
                notifications[what] = n;
                n.SetResult(args);
            }
        }

        public void On(string evtName, Func<JObject, CancellationToken, Task<ProtocolEventHandlerReturn>> cb)
        {
            eventListeners[evtName] = cb;
        }

        public Task<JObject> WaitForEvent(string evtName)
        {
            var eventReceived = new TaskCompletionSource<JObject>();
            On(evtName, async (args, token) =>
            {
                eventReceived.SetResult(args);
                return await Task.FromResult(ProtocolEventHandlerReturn.RemoveHandler);
            });

            return eventReceived.Task.WaitAsync(Token);
        }

        void FailAllWaiters(Exception? exception = null)
        {
            // Because we can create already completed tasks,
            // when we get a NotifyOf before the corresponding
            // WaitFor, it might already be completed. So, use
            // *Try* methods

            if (exception != null)
            {
                _isFailingWithException = exception;
                foreach (var tcs in notifications.Values)
                    tcs.TrySetException(exception);
            }
            else
            {
                foreach (var tcs in notifications.Values)
                    tcs.TrySetCanceled();
            }
        }

        internal (string line, string type) FormatConsoleAPICalled(JObject args)
        {
            string? type = args?["type"]?.Value<string>();
            List<string> consoleArgs = new();
            foreach (JToken? arg in args?["args"] ?? Enumerable.Empty<JToken?>())
            {
                if (arg?["value"] != null)
                    consoleArgs.Add(arg!["value"]!.ToString());
            }

            type ??= "log";
            if (consoleArgs.Count == 0)
                return (line: "console: <message missing>", type);

            int position = 1;
            string first = consoleArgs[0];
            string output = _consoleArgsRegex.Replace(first, (_) => $"{consoleArgs[position++]}");
            if (position == 1)
            {
                // first arg wasn't a format string so concat things together
                // with a space instead.
                StringBuilder builder = new StringBuilder(first);
                for (position = 1; position < consoleArgs.Count(); position++)
                {
                    builder.Append(" ");
                    builder.Append(consoleArgs[position]);
                }
                output = builder.ToString();
            }
            else
            {
                if (output.EndsWith('\n'))
                    output = output[..^1];
            }

            return ($"console.{type}: {output}", type);
        }

        async Task OnMessage(string sessionId, string method, JObject args, CancellationToken token)
        {
            bool fail = false;
            switch (method)
            {
                case "Target.attachedToTarget":
                {
                    var sessionIdNewTarget = new SessionId(args["sessionId"]?.Value<string>());
                    await Client.SendCommand(sessionIdNewTarget, "Profiler.enable", null, token);
                    await Client.SendCommand(sessionIdNewTarget, "Runtime.enable", null, token);
                    await Client.SendCommand(sessionIdNewTarget, "Debugger.enable", null, token);
                    await Client.SendCommand(sessionIdNewTarget, "Runtime.runIfWaitingForDebugger", null, token);
                    await Client.SendCommand(sessionIdNewTarget, "Debugger.setAsyncCallStackDepth", JObject.FromObject(new { maxDepth = 32}), token);
                    break;
                }
                case "Debugger.paused":
                {
                    if (sessionId != "")
                        args.Add("sessionId", sessionId);
                    NotifyOf(PAUSE, args);
                    break;
                }
                case "Mono.runtimeReady":
                {
                    _gotRuntimeReady = true;
                    if (_gotAppReady || !DebuggerTestBase.RunningOnChrome)
                    {
                        // got both the events
                        NotifyOf(APP_READY, args);
                    }
                    break;
                }
                case "Runtime.consoleAPICalled":
                {
                    (string line, string type) = FormatConsoleAPICalled(args);
                    switch (type)
                    {
                        case "log": _logger.LogInformation(line); break;
                        case "debug": _logger.LogDebug(line); break;
                        case "error": _logger.LogError(line); break;
                        case "warn": _logger.LogWarning(line); break;
                        case "trace": _logger.LogTrace(line); break;
                        default: _logger.LogInformation(line); break;
                    }
                    if (line == "console.debug: #debugger-app-ready#")
                        await Client.SendCommand("DotnetDebugger.runTests", JObject.FromObject(new { type = "DotnetDebugger.runTests", to = "root" }), token);

                    if (!_gotAppReady && line == "console.debug: #debugger-app-ready#")
                    {
                        _gotAppReady = true;

                        if (_gotRuntimeReady)
                        {
                            // got both the events
                            NotifyOf(APP_READY, args);
                        }
                    }

                    if (DetectAndFailOnAssertions &&
                            (line.Contains("console.error: [MONO]") || line.Contains("console.warning: [MONO]")))
                    {
                        args["__forMethod"] = method;
                        Client.Fail(new ArgumentException($"Unexpected runtime error/warning message detected: {line}{Environment.NewLine}{args}"));
                        TestHarnessProxy.ShutdownProxy(Id.ToString());
                        return;
                    }

                    break;
                }
                case "Inspector.detached":
                case "Inspector.targetCrashed":
                case "Inspector.targetReloadedAfterCrash":
                    fail = true;
                    break;
                case "Runtime.exceptionThrown":
                    _logger.LogDebug($"Failing all waiters because: {method}: {args}");
                    fail = true;
                    break;
            }
            if (eventListeners.TryGetValue(method, out Func<JObject, CancellationToken, Task<ProtocolEventHandlerReturn>>? listener)
                    && listener != null)
            {
                if (sessionId != "")
                    args.Add("sessionId", sessionId);
                ProtocolEventHandlerReturn result = await listener(args, token).ConfigureAwait(false);
                if (result is ProtocolEventHandlerReturn.RemoveHandler)
                    eventListeners.Remove(method, out _);
            }
            else if (fail)
            {
                args["__forMethod"] = method;
                FailAllWaiters(new ArgumentException(args.ToString()));
            }
        }

        public async Task LaunchBrowser(DateTime start, TimeSpan span)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
                _cancellationTokenSource.CancelAfter(span);
            string uriStr = $"ws://{TestHarnessProxy.Endpoint.Authority}/launch-host-and-connect/?test_id={Id}";
            if (!DebuggerTestBase.RunningOnChrome)
            {
                uriStr += $"&host=firefox&firefox-proxy-port={DebuggerTestBase.FirefoxProxyPort}";
                // Ensure the listener is running early, so trying to
                // connect to that does not race with the starting of it
                FirefoxDebuggerProxy.StartListener(DebuggerTestBase.FirefoxProxyPort, _logger);
            }

            await Client.Connect(new Uri(uriStr), OnMessage, _cancellationTokenSource);
            Client.RunLoopStopped += (_, args) =>
            {
                switch (args.reason)
                {
                    case RunLoopStopReason.Exception:
                        if (TestHarnessProxy.TryGetProxyExitState(Id.ToString(), out var state))
                        {
                            Console.WriteLine ($"client exiting with exception, and proxy has: {state}");
                        }
                        FailAllWaiters(args.exception);
                        break;

                    case RunLoopStopReason.Cancelled when Token.IsCancellationRequested:
                        if (_isFailingWithException is null)
                            FailAllWaiters(new TaskCanceledException($"Test timed out (elapsed time: {(DateTime.Now - start).TotalSeconds})"));
                        break;

                    default:
                        if (_isFailingWithException is null)
                            FailAllWaiters();
                        break;
                };
            };

            TestHarnessProxy.RegisterExitHandler(Id.ToString(), state =>
            {
                if (_isFailingWithException is null && state.reason == RunLoopStopReason.Exception)
                {
                    Client.Fail(state.exception);
                    FailAllWaiters(state.exception);
                }
            });
        }

        public async Task OpenSessionAsync(Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> getInitCmds, string urlToInspect, TimeSpan span)
        {
            var start = DateTime.Now;
            try
            {
                await LaunchBrowser(start, span);
                var init_cmds = getInitCmds(Client, _cancellationTokenSource.Token);

                Task<Result> readyTask = Task.Run(async () => Result.FromJson(await WaitFor(APP_READY)));
                init_cmds.Add((APP_READY, readyTask));

                _logger.LogInformation("waiting for the runtime to be ready");
                while (!_cancellationTokenSource.IsCancellationRequested && init_cmds.Count > 0)
                {
                    var cmd_tasks = init_cmds.Select(ct => ct.Item2);
                    Task<Result> completedTask = await Task.WhenAny(cmd_tasks);

                    int cmdIdx = init_cmds.FindIndex(ct => ct.Item2 == completedTask);
                    string cmd_name = init_cmds[cmdIdx].Item1;

                    if (_isFailingWithException is not null)
                    {
                        _logger.LogError($"_isFailingWithException. {_isFailingWithException}.");
                        throw _isFailingWithException;
                    }

                    if (completedTask.IsCanceled)
                    {
                        throw new TaskCanceledException(
                                    $"Command {cmd_name} timed out during init for the test." +
                                    $"Remaining commands: {RemainingCommandsToString(cmd_name, init_cmds)}." +
                                    $"Total time: {(DateTime.Now - start).TotalSeconds}");
                    }

                    if (completedTask.IsFaulted)
                    {
                        _logger.LogError($"Command {cmd_name} failed with {completedTask.Exception}. Remaining commands: {RemainingCommandsToString(cmd_name, init_cmds)}.");
                        throw completedTask.Exception!;
                    }

                    await Client.ProcessCommand(completedTask.Result, _cancellationTokenSource.Token);
                    Result res = completedTask.Result;

                    if (!res.IsOk)
                        throw new ArgumentException($"Command {cmd_name} failed with: {res.Error}. Remaining commands: {RemainingCommandsToString(cmd_name, init_cmds)}");

                    if (DebuggerTestBase.RunningOnChrome && cmd_name == "Debugger.enable")
                        await Client.SendCommand("Page.navigate", JObject.FromObject(new { url = urlToInspect }), _cancellationTokenSource.Token);
                    init_cmds.RemoveAt(cmdIdx);
                }

                _logger.LogInformation("runtime ready, TEST TIME");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex.ToString());
                if (TestHarnessProxy.TryGetProxyExitState(Id.ToString(), out var state))
                {
                    Console.WriteLine ($"OpenSession crashing. proxy state: {state}");
                    if (state.reason == RunLoopStopReason.Exception && state.exception is not null)
                        ExceptionDispatchInfo.Capture(state.exception).Throw();
                }

                throw;
            }

            static string RemainingCommandsToString(string cmd_name, IList<(string, Task<Result>)> cmds)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < cmds.Count; i++)
                {
                    var (name, task) = cmds[i];

                    if (cmd_name == name)
                        continue;

                    sb.Append($"[{name}: {task.Status}], ");
                }

                if (sb.Length > 2)
                    sb.Length -= 2;

                return sb.ToString();
            }
        }

        public async Task ShutdownAsync()
        {
            if (Client == null)
                throw new InvalidOperationException($"InspectorClient is null. Duplicate Shutdown?");

            try
            {
                TestHarnessProxy.ShutdownProxy(Id.ToString());
                await Client.ShutdownAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
                throw;
            }
            finally
            {
                _cancellationTokenSource.Cancel();
                Client.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }
    }

    public enum ProtocolEventHandlerReturn
    {
        KeepHandler,
        RemoveHandler
    }
}
