// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class DebuggerTests :
#if RUN_IN_CHROME
    DebuggerTestBase
#else
    DebuggerTestFirefox
#endif
    {
        public const string WebcilInWasmExtension = ".wasm";

        public DebuggerTests(ITestOutputHelper testOutput, string locale = "en-US", string driver = "debugger-driver.html")
                : base(testOutput, locale, driver)
        {}
    }

    public class DebuggerTestBase : IAsyncLifetime
    {
        public static WasmHost RunningOn
#if RUN_IN_CHROME
            => WasmHost.Chrome;
#else
            => WasmHost.Firefox;
#endif
        public static bool ReleaseRuntime
#if RELEASE_RUNTIME
            => true;
#else
            => false;
#endif
        public static bool WasmMultiThreaded => EnvironmentVariables.WasmTestsUsingVariant == "multithreaded";

        public static bool WasmSingleThreaded => !WasmMultiThreaded;

        public static bool RunningOnChrome => RunningOn == WasmHost.Chrome;

        public static bool RunningOnChromeAndLinux => RunningOn == WasmHost.Chrome && PlatformDetection.IsLinux;

        public const int FirefoxProxyPort = 6002;

        internal InspectorClient cli;
        internal Inspector insp;
        protected CancellationToken token;
        protected Dictionary<string, string> scripts;
        protected Task startTask;

        public bool UseCallFunctionOnBeforeGetProperties;

        private const int DefaultTestTimeoutMs = 1 * 60 * 1000;
        protected TimeSpan TestTimeout = TimeSpan.FromMilliseconds(DefaultTestTimeoutMs);
        protected ITestOutputHelper _testOutput;
        protected readonly TestEnvironment _env;

        static string s_debuggerTestAppPath;
        static int s_idCounter = -1;

        public int Id { get; set; }
        public string driver;
        
        public static string DebuggerTestAppPath
        {
            get
            {
                if (s_debuggerTestAppPath == null)
                    s_debuggerTestAppPath = FindTestPath();

                return s_debuggerTestAppPath;
            }
        }

        static protected string FindTestPath()
        {
            string test_app_path = EnvironmentVariables.DebuggerTestPath;

            if (string.IsNullOrEmpty(test_app_path))
            {
                var asm_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#if DEBUG
                var config="Debug";
#else
                var config="Release";
#endif
                test_app_path = Path.Combine(asm_dir, "..", "..", "..", "debugger-test", config);

                if (string.IsNullOrEmpty(test_app_path))
                    throw new Exception("Could not figure out debugger-test app path from the 'DEBUGGER_TEST_PATH' " +
                                       $"environment variable, or based on the test suite location ({asm_dir})");
            }

            if (!string.IsNullOrEmpty(test_app_path))
                test_app_path = Path.Combine(test_app_path, "AppBundle");

            if (File.Exists(Path.Combine(test_app_path, "debugger-driver.html")))
                return test_app_path;

            throw new Exception($"Cannot find 'debugger-driver.html' in {test_app_path}");
        }

        internal virtual string UrlToRemoteDebugging() => "http://localhost:0";

        static string s_testLogPath = null;
        public static string TestLogPath
        {
            get
            {
                if (s_testLogPath == null)
                {
                    string logPathVar = EnvironmentVariables.TestLogPath;
                    logPathVar = string.IsNullOrEmpty(logPathVar) ? Environment.CurrentDirectory : logPathVar;
                    Interlocked.CompareExchange(ref s_testLogPath, logPathVar, null);
                }

                return s_testLogPath;
            }
        }

        public static string TempPath => Path.Combine(Path.GetTempPath(), "dbg-tests-tmp");
        static DebuggerTestBase()
        {
            if (Directory.Exists(TempPath))
                Directory.Delete(TempPath, recursive: true);
        }

        public DebuggerTestBase(ITestOutputHelper testOutput, string locale, string _driver = "debugger-driver.html")
        {
            _env = new TestEnvironment(testOutput);
            _testOutput = testOutput;
            Id = Interlocked.Increment(ref s_idCounter);
            // the debugger is working in locale of the debugged application. For example Datetime.ToString()
            // we want the test to mach it. We are also starting chrome with --lang=en-US
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(locale);

            insp = new Inspector(Id, _testOutput);
            cli = insp.Client;
            driver = _driver;
            scripts = SubscribeToScripts(insp);
            startTask = TestHarnessProxy.Start(DebuggerTestAppPath, driver, UrlToRemoteDebugging(), testOutput, locale);
        }

        public virtual async Task InitializeAsync()
        {
            bool retry = true;
            Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> fn = (client, token) =>
             {
                 Func<string, JObject, (string, Task<Result>)> getInitCmdFn = (cmd, args) => (cmd, client.SendCommand(cmd, args, token));
                 var init_cmds = new List<(string, Task<Result>)>
                 {
                    getInitCmdFn("Profiler.enable", null),
                    getInitCmdFn("Runtime.enable", null),
                    getInitCmdFn("Debugger.enable", null),
                    getInitCmdFn("Runtime.runIfWaitingForDebugger", null),
                    getInitCmdFn("Debugger.setAsyncCallStackDepth", JObject.FromObject(new { maxDepth = 32 })),
                    getInitCmdFn("Target.setAutoAttach", JObject.FromObject(new { autoAttach = true, waitForDebuggerOnStart = true, flatten = true }))
                    //getInitCmdFn("ServiceWorker.enable", null)
                 };
                 return init_cmds;
             };

            await Ready();
            try {
                await insp.OpenSessionAsync(fn,  $"http://{TestHarnessProxy.Endpoint.Authority}/{driver}", TestTimeout);
            }
            catch (TaskCanceledException exc) //if timed out for some reason let's try again
            {
                if (!retry)
                    throw exc;
                retry = false;
                _testOutput.WriteLine($"Let's retry: {exc.ToString()}");
                Id = Interlocked.Increment(ref s_idCounter);
                insp = new Inspector(Id, _testOutput);
                cli = insp.Client;
                scripts = SubscribeToScripts(insp);
                await insp.OpenSessionAsync(fn,  $"http://{TestHarnessProxy.Endpoint.Authority}/{driver}", TestTimeout);
            }
        }

        public virtual async Task DisposeAsync()
        {
            await insp.ShutdownAsync().ConfigureAwait(false);
            _env.Dispose();
        }

        public Task Ready() => startTask;

        internal Dictionary<string, string> dicScriptsIdToUrl;
        internal Dictionary<string, string> dicFileToUrl;
        internal virtual Dictionary<string, string> SubscribeToScripts(Inspector insp)
        {
            dicScriptsIdToUrl = new Dictionary<string, string>();
            dicFileToUrl = new Dictionary<string, string>();
            insp.On("Debugger.scriptParsed", DefaultScriptParsedHandler);
            return dicScriptsIdToUrl;
        }

        protected Task<ProtocolEventHandlerReturn> DefaultScriptParsedHandler(JObject args, CancellationToken token)
        {
            var script_id = args?["scriptId"]?.Value<string>();
            var url = args["url"]?.Value<string>();
            script_id += args["sessionId"]?.Value<string>();
            if (script_id.StartsWith("dotnet://"))
            {
                var dbgUrl = args["dotNetUrl"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dbgUrl = arrStr[0] + "/" + arrStr[1] + "/" + arrStr[2] + "/" + arrStr[arrStr.Length - 1];
                dicScriptsIdToUrl[script_id] = dbgUrl;
                dicFileToUrl[dbgUrl] = args["url"]?.Value<string>();
            }
            else if (url.StartsWith("cdp://"))
            {
                //ignore them as it's done by the browser and vscode-js-debug
            }
            else if (!String.IsNullOrEmpty(url))
            {
                var dbgUrl = args["url"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dicScriptsIdToUrl[script_id] = arrStr[arrStr.Length - 1];
                dicFileToUrl[new Uri(url).AbsolutePath] = url;
            }
            return Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
        }

        internal async Task CheckInspectLocalsAtBreakpointSite(string url_key, int line, int column, string function_name, string eval_expression,
            Func<JToken, Task> test_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            var bp = await SetBreakpoint(url_key, line, column);

            await EvaluateAndCheck(
                eval_expression, url_key, line, column,
                function_name,
                wait_for_event_fn: async (pause_location) =>
               {
                   //make sure we're on the right bp

                   Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                   var top_frame = pause_location!["callFrames"]?[0];

                   var scope = top_frame!["scopeChain"]?[0];
                   if (wait_for_event_fn != null)
                       await wait_for_event_fn(pause_location);
                   else
                       await Task.CompletedTask;
               },
                locals_fn: async (locals) =>
                {
                    if (test_fn != null)
                        await test_fn(locals);
                }
            );
        }

        internal virtual string EvaluateCommand()
        {
            return "Runtime.evaluate";
        }

        internal virtual JObject CreateEvaluateArgs(string expression)
            => JObject.FromObject(new { expression });

        internal virtual async Task<JObject> WaitFor(string what)
        {
            return await insp.WaitFor(what);
        }
        public async Task WaitForConsoleMessage(string message)
        {
            object llock = new();
            var tcs = new TaskCompletionSource();
            insp.On("Runtime.consoleAPICalled", async (args, c) =>
            {
                (string line, string type) = insp.FormatConsoleAPICalled(args);
                if (string.IsNullOrEmpty(line))
                    return await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);

                lock (llock)
                {
                    try
                    {
                        if (line == message)
                        {
                            tcs.SetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }

                return tcs.Task.IsCompleted
                            ? await Task.FromResult(ProtocolEventHandlerReturn.RemoveHandler)
                            : await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
            });

            await tcs.Task;
        }
        public async Task WaitForScriptParsedEventsAsync(params string[] paths)
        {
            object llock = new();
            List<string> pathsList = new(paths);
            var tcs = new TaskCompletionSource();
            insp.On("Debugger.scriptParsed", async (args, c) =>
            {
                await DefaultScriptParsedHandler(args, c);

                string url = args["url"]?.Value<string>();
                if (string.IsNullOrEmpty(url))
                    return await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);

                lock (llock)
                {
                    try
                    {
                        int idx = pathsList.FindIndex(p => url?.EndsWith(p) == true);
                        if (idx >= 0)
                        {
                            pathsList.RemoveAt(idx);
                            if (pathsList.Count == 0)
                            {
                                tcs.SetResult();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }

                return tcs.Task.IsCompleted
                            ? await Task.FromResult(ProtocolEventHandlerReturn.RemoveHandler)
                            : await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
            });

            await tcs.Task;
        }

        // sets breakpoint by method name and line offset
        internal async Task CheckInspectLocalsAtBreakpointSite(string type, string method, int line_offset, string bp_function_name, string eval_expression,
            Func<JToken, Task> locals_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false, string assembly = "debugger-test", int col = 0)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            var bp = await SetBreakpointInMethod(assembly, type, method, line_offset, col);
            var res = await cli.SendCommand(EvaluateCommand(), CreateEvaluateArgs(eval_expression), token);
            if (!res.IsOk)
            {
                _testOutput.WriteLine($"Failed to run command {method} with args: {CreateEvaluateArgs(eval_expression)?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }
            var pause_location = await WaitFor(Inspector.PAUSE);

            if (bp_function_name != null)
                Assert.Equal(bp_function_name, pause_location["callFrames"]?[0]?["functionName"]?.Value<string>());

            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            var top_frame = pause_location!["callFrames"]?[0];

            var scope = top_frame?["scopeChain"]?[0];

            if (wait_for_event_fn != null)
                await wait_for_event_fn(pause_location);

            if (locals_fn != null)
            {
                var locals = await GetProperties(pause_location?["callFrames"]?[0]?["callFrameId"]?.Value<string>());
                await locals_fn(locals);
            }
        }

        internal virtual void CheckLocation(string script_loc, int line, int column, Dictionary<string, string> scripts, JToken location)
        {
            var loc_str = $"{ scripts[location["scriptId"].Value<string>()+cli.CurrentSessionId.sessionId] }" +
                $"#{ location["lineNumber"].Value<int>() }" +
                $"#{ location["columnNumber"].Value<int>() }";

            var expected_loc_str = $"{script_loc}#{line}#{column}";
            Assert.Equal(expected_loc_str, loc_str);
        }

        internal virtual void CheckLocationLine(JToken location, int line)
        {
            Assert.Equal(location["lineNumber"].Value<int>(), line);
        }

        internal void CheckNumber<T>(JToken locals, string name, T value)
        {
            foreach (var l in locals)
            {
                if (name != l["name"]?.Value<string>())
                    continue;
                var val = l["value"];
                Assert.Equal("number", val["type"]?.Value<string>());
                Assert.Equal(value, val["value"].Value<T>());
                Assert.Equal(value.ToString(), val["description"].Value<T>().ToString());
                return;
            }
            Assert.True(false, $"Could not find variable '{name}'");
        }

        internal void CheckNumberAsString(JToken locals, string name, string value)
        {
            foreach (var l in locals)
            {
                if (name != l["name"]?.Value<string>())
                    continue;
                var val = l["value"];
                Assert.Equal("number", val["type"]?.Value<string>());
                Assert.Equal(value, val["value"].ToString());
                return;
            }
            Assert.True(false, $"Could not find variable '{name}'");
        }

        internal async Task CheckString(JToken locals, string name, string value)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], TString(value), name);
        }

        internal async Task<JToken> CheckSymbol(JToken locals, string name, char value)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], TChar(value), name);
            return l;
        }

        internal async Task<JToken> Check(JToken locals, string name, JObject expected)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], expected, name);
            return l;
        }

        internal async Task<JToken> CheckObject(JToken locals, string name, string class_name, string subtype = null, bool is_null = false, string description = null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            var val = l["value"];
            await CheckValue(val, TObject(class_name, is_null: is_null, description: description), name);
            Assert.True(val["isValueType"] == null || !val["isValueType"].Value<bool>());

            return l;
        }

        internal async Task<JToken> CheckPointerValue(JToken locals, string name, JToken expected, string label = null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], expected, $"{label ?? String.Empty}-{name}");
            return l;
        }

        internal async Task CheckDateTime(JToken value, DateTime expected, string label = "")
        {
            await CheckValue(value, TValueType("System.DateTime", expected.ToString()), label);
            await CheckDateTimeValue(value, expected, label);
        }

        internal async Task CheckDateTime(JToken locals, string name, DateTime expected, string label = "")
        {
            var obj = GetAndAssertObjectWithName(locals, name, label);
            await CheckDateTimeValue(obj["value"], expected, label);
        }

        async Task CheckDateTimeMembers(JToken v, DateTime exp_dt, string label = "")
        {
            AssertEqual("System.DateTime", v["className"]?.Value<string>(), $"{label}#className");
            AssertEqual(exp_dt.ToString(), v["description"]?.Value<string>(), $"{label}#description");

            var members = await GetProperties(v["objectId"]?.Value<string>());

            // not checking everything
            CheckNumber(members, "Year", exp_dt.Year);
            CheckNumber(members, "Month", exp_dt.Month);
            CheckNumber(members, "Day", exp_dt.Day);
            CheckNumber(members, "Hour", exp_dt.Hour);
            CheckNumber(members, "Minute", exp_dt.Minute);
            CheckNumber(members, "Second", exp_dt.Second);
        }

        internal virtual async Task CheckDateTimeGetter(JToken value, DateTime expected, string label = "")
        {
            var res = await InvokeGetter(JObject.FromObject(new { value = value }), "Date");
            await CheckDateTimeMembers(res.Value["result"], expected.Date, label);
        }

        internal async Task CheckDateTimeValue(JToken value, DateTime expected, string label = "")
        {
            await CheckDateTimeMembers(value, expected, label);

            await CheckDateTimeGetter(value, expected, label);
        }

        internal async Task<JToken> CheckBool(JToken locals, string name, bool expected)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], TBool(expected), name);
            return l;
        }

        internal void CheckContentValue(JToken token, string value)
        {
            var val = token["value"].Value<string>();
            Assert.Equal(value, val);
        }

        internal void CheckContainsJObject(JToken locals, JToken comparedTo, string name)
        {
            var val = GetAndAssertObjectWithName(locals, name);
            JObject refValue = (JObject)val["value"];
            refValue?.Property("objectId")?.Remove();
            JObject comparedToValue = (JObject)comparedTo["value"];
            comparedToValue?.Property("objectId")?.Remove();
            Assert.Equal(val, comparedTo);
        }

        internal async Task<JToken> CheckValueType(JToken locals, string name, string class_name, string description=null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], TValueType(class_name, description: description), name);
            return l;
        }

        internal async Task<JToken> CheckEnum(JToken locals, string name, string class_name, string descr)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], TEnum(class_name, descr), name);
            return l;
        }

        internal async Task CheckArray(JToken locals, string name, string class_name, string description)
           => await CheckValue(
                        GetAndAssertObjectWithName(locals, name)["value"],
                        TArray(class_name, description), name);

        internal JToken GetAndAssertObjectWithName(JToken obj, string name, string label = "")
        {
            var l = obj.FirstOrDefault(jt => jt["name"]?.Value<string>() == name);
            if (l == null)
                Assert.True(false, $"[{label}] Could not find variable '{name}'");
            return l;
        }

        internal async Task<Result> SendCommand(string method, JObject args)
        {
            var res = await cli.SendCommand(method, args, token);
            if (!res.IsOk)
            {
                _testOutput.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }
            return res;
        }

        internal async Task<Result> Evaluate(string expression)
        {
            return await SendCommand(EvaluateCommand(), CreateEvaluateArgs(expression));
        }

        internal void AssertLocation(JObject args, string methodName)
        {
            Assert.Equal(methodName, args["callFrames"]?[0]?["functionName"]?.Value<string>());
        }

        // Place a breakpoint in the given method and run until its hit
        // Return the Debugger.paused data
        internal async Task<JObject> RunUntil(string methodName)
        {
            await SetBreakpointInMethod("debugger-test", "DebuggerTest", methodName);
            // This will run all the tests until it hits the bp
            await Evaluate("window.setTimeout(function() { invoke_run_all (); }, 1);");
            var wait_res = await WaitFor(Inspector.PAUSE);
            AssertLocation(wait_res, "DebuggerTest.locals_inner");
            return wait_res;
        }

        internal async Task<Result> InvokeGetter(JToken obj, object arguments, string fn = "function(e){return this[e]}", bool expect_ok = true, bool? returnByValue = null)
        {
            var req = JObject.FromObject(new
            {
                functionDeclaration = fn,
                objectId = obj["value"]?["objectId"]?.Value<string>(),
                arguments = new[] { new { value = arguments } }
            });
            if (returnByValue != null)
                req["returnByValue"] = returnByValue.Value;

            var res = await cli.SendCommand("Runtime.callFunctionOn", req, token);
            Assert.True(expect_ok == res.IsOk, $"InvokeGetter failed for {req} with {res}");

            return res;
        }

        internal async Task<Result> SetValueOnObject(JToken obj, string property, string newvalue, string fn = "function(a, b) { this[a] = b; }", bool expect_ok = true)
        {
            var req = JObject.FromObject(new
            {
                functionDeclaration = fn,
                objectId = obj["value"]?["objectId"]?.Value<string>(),
                arguments = new[] { new { value = property }, new { value = newvalue } },
                silent = true
            });
            var res = await cli.SendCommand("Runtime.callFunctionOn", req, token);
            Assert.True(expect_ok == res.IsOk, $"SetValueOnObject failed for {req} with {res}");

            return res;
        }

        internal virtual async Task<JObject> StepAndCheck(StepKind kind, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, int times = 1)
        {
            string method = (kind == StepKind.Resume ? "Debugger.resume" : $"Debugger.step{kind}");
            for (int i = 0; i < times - 1; i++)
            {
                await SendCommandAndCheck(null, method, null, -1, -1, null);
            }

            // Check for method/line etc only at the last step
            return await SendCommandAndCheck(
                null, method, script_loc, line, column, function_name,
                wait_for_event_fn: wait_for_event_fn,
                locals_fn: locals_fn);
        }

        internal async Task<JObject> SetNextIPAndCheck(string script_id, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, bool expected_error = false)
        {
            var setNextIPArgs = JObject.FromObject(new
                {
                    scriptId = script_id,
                    lineNumber = line,
                    columnNumber = column
                });

            if (!expected_error)
            {
                return await SendCommandAndCheck(
                    JObject.FromObject(new { location = setNextIPArgs }), "DotnetDebugger.setNextIP", script_loc, line, column, function_name,
                    wait_for_event_fn: wait_for_event_fn,
                    locals_fn: locals_fn);
            }

            var res = await cli.SendCommand("DotnetDebugger.setNextIP", JObject.FromObject(new { location = setNextIPArgs }), token);
            Assert.False(res.IsOk);
            return JObject.FromObject(res);
        }

        internal virtual async Task<JObject> EvaluateAndCheck(
                                        string expression, string script_loc, int line, int column, string function_name,
                                        Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null)
            => await SendCommandAndCheck(
                        CreateEvaluateArgs(expression),
                        "Runtime.evaluate", script_loc, line, column, function_name,
                        wait_for_event_fn: wait_for_event_fn,
                        locals_fn: locals_fn);

        internal virtual async Task<JObject> SendCommandAndCheck(JObject args, string method, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, string waitForEvent = Inspector.PAUSE)
        {
            var res = await cli.SendCommand(method, args, token);
            if (!res.IsOk)
            {
                _testOutput.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }

            var wait_res = await WaitFor(waitForEvent);
            JToken top_frame = wait_res["callFrames"]?[0];
            if (function_name != null)
            {
                AssertEqual(function_name, wait_res["callFrames"]?[0]?["functionName"]?.Value<string>(), top_frame?.ToString());
            }

            if (script_loc != null && line >= 0)
                CheckLocation(script_loc, line, column, scripts, top_frame["location"]);

            if (wait_for_event_fn != null)
                await wait_for_event_fn(wait_res);

            if (locals_fn != null)
            {
                var locals = await GetProperties(wait_res["callFrames"][0]["callFrameId"].Value<string>());
                try
                {
                    await locals_fn(locals);
                }
                catch (System.AggregateException ex)
                {
                    throw new AggregateException(ex.Message + " \n" + locals.ToString(), ex);
                }
            }

            return wait_res;
        }

        internal async Task CheckDelegate(JToken locals, string name, string className, string target)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            var val = l["value"];

            await CheckDelegate(l, TDelegate(className, target), name);
        }

        internal async Task CheckDelegate(JToken actual_val, JToken exp_val, string label)
        {
            AssertEqual("object", actual_val["type"]?.Value<string>(), $"{label}-type");
            AssertEqual(exp_val["className"]?.Value<string>(), actual_val["className"]?.Value<string>(), $"{label}-className");

            var actual_target = actual_val["description"]?.Value<string>();
            Assert.True(actual_target != null, $"${label}-description");
            var exp_target = exp_val["target"].Value<string>();

            CheckDelegateTarget(actual_target, exp_target);

            var del_props = await GetProperties(actual_val["objectId"]?.Value<string>());
            AssertEqual(1, del_props.Count(), $"${label}-delegate-properties-count");

            var obj = del_props.Where(jt => jt["name"]?.Value<string>() == "Target").FirstOrDefault();
            Assert.True(obj != null, $"[{label}] Property named 'Target' found found in delegate properties");

            AssertEqual("symbol", obj["value"]?["type"]?.Value<string>(), $"{label}#Target#type");
            CheckDelegateTarget(obj["value"]?["value"]?.Value<string>(), exp_target);

            return;

            void CheckDelegateTarget(string actual_target, string exp_target)
            {
                var parts = exp_target.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    // not a generated method
                    AssertEqual(exp_target, actual_target, $"{label}-description");
                }
                else
                {
                    bool prefix = actual_target.StartsWith(parts[0], StringComparison.Ordinal);
                    Assert.True(prefix, $"{label}-description, Expected target to start with '{parts[0]}'. Actual: '{actual_target}'");

                    var remaining = actual_target.Substring(parts[0].Length);
                    bool suffix = remaining.EndsWith(parts[1], StringComparison.Ordinal);
                    Assert.True(prefix, $"{label}-description, Expected target to end with '{parts[1]}'. Actual: '{remaining}'");
                }
            }
        }

        internal async Task CheckCustomType(JToken actual_val, JToken exp_val, string label)
        {
            var ctype = exp_val["__custom_type"].Value<string>();
            switch (ctype)
            {
                case "delegate":
                    await CheckDelegate(actual_val, exp_val, label);
                    break;

                case "pointer":
                    {

                        if (exp_val["is_null"]?.Value<bool>() == true)
                        {
                            AssertEqual("symbol", actual_val["type"]?.Value<string>(), $"{label}-type");

                            var exp_val_str = $"({exp_val["type_name"]?.Value<string>()}) 0";
                            AssertEqual(exp_val_str, actual_val["value"]?.Value<string>(), $"{label}-value");
                            AssertEqual(exp_val_str, actual_val["description"]?.Value<string>(), $"{label}-description");
                        }
                        else if (exp_val["is_void"]?.Value<bool>() == true)
                        {
                            AssertEqual("symbol", actual_val["type"]?.Value<string>(), $"{label}-type");

                            var exp_val_str = $"({exp_val["type_name"]?.Value<string>()})";
                            AssertStartsWith(exp_val_str, actual_val["value"]?.Value<string>(), $"{label}-value");
                            AssertStartsWith(exp_val_str, actual_val["description"]?.Value<string>(), $"{label}-description");
                        }
                        else
                        {
                            AssertEqual("object", actual_val["type"]?.Value<string>(), $"{label}-type");

                            var exp_prefix = $"({exp_val["type_name"]?.Value<string>()})";
                            AssertStartsWith(exp_prefix, actual_val["className"]?.Value<string>(), $"{label}-className");
                            AssertStartsWith(exp_prefix, actual_val["description"]?.Value<string>(), $"{label}-description");
                            Assert.False(actual_val["className"]?.Value<string>() == $"{exp_prefix} 0", $"[{label}] Expected a non-null value, but got {actual_val}");
                        }
                        break;
                    }

                case "getter":
                    {
                        // For getter, `actual_val` is not `.value`, instead it's the container object
                        // which has a `.get` instead of a `.value`
                        var get = actual_val?["get"];
                        Assert.True(get != null, $"[{label}] No `get` found. {(actual_val != null ? "Make sure to pass the container object for testing getters, and not the ['value']" : String.Empty)}");

                        AssertEqual("Function", get["className"]?.Value<string>(), $"{label}-className");
                        AssertStartsWith($"get {exp_val["type_name"]?.Value<string>()} ()", get["description"]?.Value<string>(), $"{label}-description");
                        AssertEqual("function", get["type"]?.Value<string>(), $"{label}-type");
                        var expectedValue = exp_val["value"];
                        if (expectedValue.Type != JTokenType.Null)
                        {
                            var valueAfterRunGet = await GetProperties(get["objectId"]?.Value<string>());
                            await CheckValue(valueAfterRunGet[0]["value"], expectedValue, exp_val["type_name"]?.Value<string>());
                        }
                        break;
                    }

                case "datetime":
                    {
                        var dateTime = DateTime.FromBinary(exp_val["binary"].Value<long>());
                        await CheckDateTime(actual_val, dateTime, label);
                        break;
                    }

                case "ignore_me":
                    // nothing to check ;)
                    break;

                default:
                    throw new ArgumentException($"{ctype} not supported");
            }
        }

        internal async Task CheckProps(JToken actual, object exp_o, string label, int num_fields = -1, bool skip_num_fields_check = false)
        {
            if (exp_o.GetType().IsArray || exp_o is JArray)
            {
                if (!(actual is JArray actual_arr))
                {
                    Assert.True(false, $"[{label}] Expected to get an array here but got {actual}");
                    return;
                }

                var exp_v_arr = JArray.FromObject(exp_o);
                AssertEqual(exp_v_arr.Count, actual_arr.Count(), $"{label}-count");

                for (int i = 0; i < exp_v_arr.Count; i++)
                {
                    var exp_i = exp_v_arr[i];
                    var act_i = actual_arr[i];

                    string exp_name = exp_i["name"]?.Value<string>();
                    if (string.IsNullOrEmpty(exp_name))
                        exp_name = i.ToString();

                    AssertEqual(exp_name, act_i["name"]?.Value<string>(), $"{label}-[{i}].name");
                    if (exp_i != null)
                    {
                        await CheckValue(act_i["value"],
                            ((JObject)exp_i).GetValue("value")?.HasValues == true ? exp_i["value"] : exp_i,
                            $"{label}-{i}th value");
                    }
                }
                return;
            }
            // Not an array
            var exp = exp_o as JObject;
            if (exp == null)
                exp = JObject.FromObject(exp_o);

            if (!skip_num_fields_check)
            {
                num_fields = num_fields < 0 ? exp.Values<JToken>().Count() : num_fields;
                var expected_str = string.Join(", ",
                    exp.Children()
                    .Select(e => e is JProperty jprop ? jprop.Name : e["name"]?.Value<string>())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .OrderBy(e => e));

                var actual_str = string.Join(", ",
                    actual.Children()
                    .Select(e => e["name"]?.Value<string>())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .OrderBy(e => e));
                Assert.True(num_fields == actual.Count(), $"[{label}] Number of fields don't match, Expected: {num_fields}, Actual: {actual.Count()}.{Environment.NewLine}"
                    + $"  Expected: {expected_str}{Environment.NewLine}"
                    + $"  Actual:   {actual_str}");
            }

            foreach (var kvp in exp)
            {
                var exp_name = kvp.Key;
                var exp_val = kvp.Value;

                var actual_obj = actual.FirstOrDefault(jt => jt["name"]?.Value<string>() == exp_name);
                if (actual_obj == null)
                {
                    Assert.True(actual_obj != null, $"[{label}] Could not find property named '{exp_name}'");
                }

                Assert.True(actual_obj != null, $"[{label}] not value found for property named '{exp_name}'");

                if (exp_val.Type == JTokenType.Array)
                {
                    var actual_props = await GetProperties(actual_obj["value"]["objectId"]?.Value<string>());
                    await CheckProps(actual_props, exp_val, $"{label}-{exp_name}");
                }
                else if (exp_val["__custom_type"] != null && exp_val["__custom_type"]?.Value<string>() == "getter")
                {
                    // hack: for getters, actual won't have a .value
                    await CheckCustomType(actual_obj, exp_val, $"{label}#{exp_name}");
                }
                else
                {
                    await CheckValue(actual_obj["value"], exp_val, $"{label}#{exp_name}");
                }
            }
        }

        internal virtual bool SkipProperty(string propertyName)
        {
            return false;
        }

        internal async Task CheckValue(JToken actual_val, JToken exp_val, string label)
        {
            if (exp_val["__custom_type"] != null)
            {
                await CheckCustomType(actual_val, exp_val, label);
                return;
            }

            if (exp_val["type"] == null && actual_val["objectId"] != null)
            {
                var new_val = await GetProperties(actual_val["objectId"].Value<string>());
                await CheckProps(new_val, exp_val, $"{label}-{actual_val["objectId"]?.Value<string>()}");
                return;
            }

            try
            {
                foreach (var jp in exp_val.Values<JProperty>())
                {
                    if (SkipProperty(jp.Name))
                        continue;
                    if (jp.Value.Type == JTokenType.Object)
                    {
                        var new_val = await GetProperties(actual_val["objectId"].Value<string>());
                        await CheckProps(new_val, jp.Value, $"{label}-{actual_val["objectId"]?.Value<string>()}");

                        continue;
                    }

                    var exp_val_str = jp.Value.Value<string>();
                    bool null_or_empty_exp_val = String.IsNullOrEmpty(exp_val_str);
                    var actual_field_val = actual_val?.Values<JProperty>()?.FirstOrDefault(a_jp => a_jp.Name == jp.Name);
                    var actual_field_val_str = actual_field_val?.Value?.Value<string>();
                    if (null_or_empty_exp_val && String.IsNullOrEmpty(actual_field_val_str))
                        continue;

                    Assert.True(actual_field_val != null, $"[{label}] Could not find value field named {jp.Name}");
                    AssertEqual(exp_val_str, actual_field_val_str, $"[{label}] Value for json property named {jp.Name} didn't match.");
                }
            }
            catch (Exception ex)
            {
                _testOutput.WriteLine($"{ex.Message} \nExpected: {exp_val} \nActual: {actual_val}");
                throw;
            }
        }

        internal async Task<JToken> GetLocalsForFrame(JToken frame, string script_loc, int line, int column, string function_name)
        {
            CheckLocation(script_loc, line, column, scripts, frame["location"]);
            Assert.Equal(function_name, frame["functionName"].Value<string>());

            return await GetProperties(frame["callFrameId"].Value<string>());
        }

        internal async Task<JToken> GetObjectOnFrame(JToken frame, string name)
        {
            var locals = await GetProperties(frame["callFrameId"].Value<string>());
            return await GetObjectOnLocals(locals, name);
        }

        // Find an object with @name, *fetch* the object, and check against @o
        internal async Task<JToken> CompareObjectPropertiesFor(JToken locals, string name, object o, string label = null, int num_fields = -1, bool skip_num_fields_check = false)
        {
            if (label == null)
                label = name;
            var props = await GetObjectOnLocals(locals, name);
            try
            {
                if (o != null)
                    await CheckProps(props, o, label, num_fields, skip_num_fields_check);
                return props;
            }
            catch
            {
                throw;
            }
        }

        internal async Task<JToken> GetObjectOnLocals(JToken locals, string name)
        {
            var obj = GetAndAssertObjectWithName(locals, name);
            var objectId = obj["value"]["objectId"]?.Value<string>();
            Assert.True(!String.IsNullOrEmpty(objectId), $"No objectId found for {name}");

            return await GetProperties(objectId);
        }

        internal void AssertInternalUseFieldsAreRemoved(JToken item)
        {
            if (item is JObject jobj && jobj.Count != 0)
            {
                foreach (JProperty jp in jobj.Properties())
                {
                    Assert.False(InternalUseFieldName.IsKnown(jp.Name),
                     $"Property {jp.Name} of object: {jobj} is for internal proxy use and should not be exposed externally.");
                }
            }
        }

        /* @fn_args is for use with `Runtime.callFunctionOn` only */
        internal virtual async Task<JToken> GetProperties(string id, JToken fn_args = null, bool? own_properties = null, bool? accessors_only = null, bool expect_ok = true)
        {
            if (UseCallFunctionOnBeforeGetProperties && !id.StartsWith("dotnet:scope:"))
            {
                var fn_decl = "function () { return this; }";
                var cfo_args = JObject.FromObject(new
                {
                    functionDeclaration = fn_decl,
                    objectId = id
                });
                if (fn_args != null)
                    cfo_args["arguments"] = fn_args;

                var result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
                AssertEqual(expect_ok, result.IsOk, $"Runtime.getProperties returned {result.IsOk} instead of {expect_ok}, for {cfo_args.ToString()}, with Result: {result}");
                if (!result.IsOk)
                    return null;
                id = result.Value["result"]?["objectId"]?.Value<string>();
            }

            var get_prop_req = JObject.FromObject(new
            {
                objectId = id
            });
            if (own_properties.HasValue)
            {
                get_prop_req["ownProperties"] = own_properties.Value;
            }
            if (accessors_only.HasValue)
            {
                get_prop_req["accessorPropertiesOnly"] = accessors_only.Value;
            }

            var frame_props = await cli.SendCommand("Runtime.getProperties", get_prop_req, token);
            AssertEqual(expect_ok, frame_props.IsOk, $"Runtime.getProperties returned {frame_props.IsOk} instead of {expect_ok}, for {get_prop_req}, with Result: {frame_props}");
            if (!frame_props.IsOk)
                return null;

            var locals = frame_props.Value["result"];
            var locals_internal = frame_props.Value["internalProperties"];
            var locals_private = frame_props.Value["privateProperties"];

            if (locals_internal != null)
                locals = new JArray(locals.Union(locals_internal));
            if (locals_private != null)
                locals = new JArray(locals.Union(locals_private));
            // FIXME: Should be done when generating the list in dotnet.es6.lib.js, but not sure yet
            //        whether to remove it, and how to do it correctly.
            if (locals is JArray)
            {
                foreach (var p in locals)
                {
                    AssertInternalUseFieldsAreRemoved(p);
                    if (p["name"]?.Value<string>() == "length" && p["enumerable"]?.Value<bool>() != true)
                    {
                        p.Remove();
                        break;
                    }
                }
            }
            return locals;
        }

        internal async Task<(JToken, JToken)> GetPropertiesSortedByProtectionLevels(string id, JToken fn_args = null, bool? own_properties = null, bool? accessors_only = null, bool expect_ok = true)
        {
            if (UseCallFunctionOnBeforeGetProperties && !id.StartsWith("dotnet:scope:"))
            {
                var fn_decl = "function () { return this; }";
                var cfo_args = JObject.FromObject(new
                {
                    functionDeclaration = fn_decl,
                    objectId = id
                });
                if (fn_args != null)
                    cfo_args["arguments"] = fn_args;

                var result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
                AssertEqual(expect_ok, result.IsOk, $"Runtime.getProperties returned {result.IsOk} instead of {expect_ok}, for {cfo_args.ToString()}, with Result: {result}");
                if (!result.IsOk)
                    return (null, null);
                id = result.Value["result"]?["objectId"]?.Value<string>();
            }

            var get_prop_req = JObject.FromObject(new
            {
                objectId = id
            });
            if (own_properties.HasValue)
            {
                get_prop_req["ownProperties"] = own_properties.Value;
            }
            if (accessors_only.HasValue)
            {
                get_prop_req["accessorPropertiesOnly"] = accessors_only.Value;
            }

            var frame_props = await cli.SendCommand("Runtime.getProperties", get_prop_req, token);
            AssertEqual(expect_ok, frame_props.IsOk, $"Runtime.getProperties returned {frame_props.IsOk} instead of {expect_ok}, for {get_prop_req}, with Result: {frame_props}");
            if (!frame_props.IsOk)
                return (null, null);;

            var locals = frame_props.Value["result"];
            var locals_private = frame_props.Value["privateProperties"];

            // FIXME: Should be done when generating the list in dotnet.es6.lib.js, but not sure yet
            //        whether to remove it, and how to do it correctly.
            if (locals is JArray)
            {
                foreach (var p in locals)
                {
                    AssertInternalUseFieldsAreRemoved(p);
                    if (p["name"]?.Value<string>() == "length" && p["enumerable"]?.Value<bool>() != true)
                    {
                        p.Remove();
                        break;
                    }
                }
            }

            return (locals, locals_private);
        }

        internal virtual async Task<(JToken, Result)> EvaluateOnCallFrame(string id, string expression, bool expect_ok = true)
        {
            var evaluate_req = JObject.FromObject(new
            {
                callFrameId = id,
                expression = expression
            });

            var res = await cli.SendCommand("Debugger.evaluateOnCallFrame", evaluate_req, token);
            AssertEqual(expect_ok, res.IsOk, $"Debugger.evaluateOnCallFrame ('{expression}', scope: {id}) returned {res.IsOk} instead of {expect_ok}, with Result: {res}");
            if (res.IsOk)
                return (res.Value["result"], res);

            return (null, res);
        }

        internal async Task RuntimeEvaluateAndCheck(params (string expression, JObject expected)[] args)
        {
            foreach (var arg in args)
            {
                var (eval_val, _) = await RuntimeEvaluate(arg.expression);
                try
                {
                    await CheckValue(eval_val, arg.expected, arg.expression);
                }
                catch
                {
                    _testOutput.WriteLine($"CheckValue failed for {arg.expression}. Expected: {arg.expected}, vs {eval_val}");
                    throw;
                }
            }
        }
        internal async Task<(JToken, Result)> RuntimeEvaluate(string expression, bool expect_ok = true)
        {
            var evaluate_req = JObject.FromObject(new
            {
                expression = expression
            });

            var res = await cli.SendCommand("Runtime.evaluate", evaluate_req, token);
            AssertEqual(expect_ok, res.IsOk, $"Runtime.evaluate ('{expression}') returned {res.IsOk} instead of {expect_ok}, with Result: {res}");
            if (res.IsOk)
                return (res.Value["result"], res);

            return (null, res);
        }

        internal async Task<(JToken, Result)> SetVariableValueOnCallFrame(JObject parms, bool expect_ok = true)
        {
            var res = await cli.SendCommand("Debugger.setVariableValue", parms, token);
            AssertEqual(expect_ok, res.IsOk, $"Debugger.setVariableValue ('{parms}') returned {res.IsOk} instead of {expect_ok}, with Result: {res}");
            if (res.IsOk)
                return (res.Value["result"], res);

            return (null, res);
        }

        internal async Task<Result> RemoveBreakpoint(string id, bool expect_ok = true)
        {
            var remove_bp = JObject.FromObject(new
            {
                breakpointId = id
            });

            var res = await cli.SendCommand("Debugger.removeBreakpoint", remove_bp, token);
            Assert.True(expect_ok ? res.IsOk : !res.IsOk);

            return res;
        }

        internal virtual async Task<Result> SetBreakpoint(string url_key, int line, int column, bool expect_ok = true, bool use_regex = false, string condition = "")
        {
            JObject bp1_req;
            if (column != -1)
            {
                bp1_req = !use_regex ?
                JObject.FromObject(new { lineNumber = line, columnNumber = column, url = dicFileToUrl[url_key], condition }) :
                JObject.FromObject(new { lineNumber = line, columnNumber = column, urlRegex = url_key, condition });
            }
            else
            {
                bp1_req = !use_regex ?
                JObject.FromObject(new { lineNumber = line, url = dicFileToUrl[url_key], condition }) :
                JObject.FromObject(new { lineNumber = line, urlRegex = url_key, condition });
            }

            var bp1_res = await cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, token);
            Assert.True(expect_ok ? bp1_res.IsOk : !bp1_res.IsOk);

            return bp1_res;
        }

        internal async Task<Result> SetPauseOnException(string state)
        {
            var exc_res = await cli.SendCommand("Debugger.setPauseOnExceptions", JObject.FromObject(new { state = state }), token);
            return exc_res;
        }

        internal virtual async Task<Result> SetBreakpointInMethod(string assembly, string type, string method, int lineOffset = 0, int col = 0, string condition = "")
        {
            var req = JObject.FromObject(new { assemblyName = assembly, typeName = type, methodName = method, lineOffset = lineOffset });

            // Protocol extension
            var res = await cli.SendCommand("DotnetDebugger.getMethodLocation", req, token);
            Assert.True(res.IsOk);

            var m_url = res.Value["result"]["url"].Value<string>();
            var m_line = res.Value["result"]["line"].Value<int>();

            var bp1_req = JObject.FromObject(new
            {
                lineNumber = m_line + lineOffset,
                columnNumber = col,
                url = m_url,
                condition
            });

            res = await cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, token);
            Assert.True(res.IsOk);
            return res;
        }

        internal async Task EvaluateOnCallFrameAndCheck(string call_frame_id, params (string expression, JObject expected)[] args)
        {
            foreach (var arg in args)
            {
                var (eval_val, _) = await EvaluateOnCallFrame(call_frame_id, arg.expression).ConfigureAwait(false);
                try
                {
                    await CheckValue(eval_val, arg.expected, arg.expression);
                }
                catch
                {
                    _testOutput.WriteLine($"CheckValue failed for {arg.expression}. Expected: {arg.expected}, vs {eval_val}");
                    throw;
                }
            }
        }

        protected async Task EvaluateOnCallFrameFail(string call_frame_id, params (string expression, string class_name)[] args)
        {
            foreach (var arg in args)
            {
                var (_, res) = await EvaluateOnCallFrame(call_frame_id, arg.expression, expect_ok: false);
                if (arg.class_name != null)
                    AssertEqual(arg.class_name, res.Error["result"]?["className"]?.Value<string>(), $"Error className did not match for expression '{arg.expression}'");
            }
        }


        internal void AssertEqual(object expected, object actual, string label)
        {
            if (expected?.Equals(actual) == true)
                return;

            throw new AssertActualExpectedException(
                expected, actual,
                $"[{label}]\n");
        }

        internal void AssertStartsWith(string expected, string actual, string label) => Assert.True(actual?.StartsWith(expected), $"[{label}] Does not start with the expected string\nExpected: {expected}\nActual:   {actual}");

        internal static Func<int, int, string, string, object> TSimpleClass = (X, Y, Id, Color) => new
        {
            X = TNumber(X),
            Y = TNumber(Y),
            Id = TString(Id),
            Color = TEnum("DebuggerTests.RGB", Color),
            PointWithCustomGetter = TGetter("PointWithCustomGetter")
        };

        internal static Func<int, int, string, string, object> TPoint = (X, Y, Id, Color) => new
        {
            X = TNumber(X),
            Y = TNumber(Y),
            Id = TString(Id),
            Color = TEnum("DebuggerTests.RGB", Color),
        };

        //FIXME: um maybe we don't need to convert jobject right here!
        internal static JObject TString(string value) =>
            value == null ?
            JObject.FromObject(new { type = "object", className = "string", subtype = "null" }) :
            JObject.FromObject(new { type = "string", value = @value });

        internal static JObject TNumber(int value) =>
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = value.ToString() });

        internal static JObject TNumber(uint value) =>
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = value.ToString() });

        // If is decimal, skip description due to culture-specific separators.
        // They depend on user's settings and we are not able to detect them here
        internal static JObject TNumber(string value, bool isDecimal = false) =>
            isDecimal ?
            JObject.FromObject(new { type = "number", value = @value.ToString() }) :
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture).ToString() });

        internal static JObject TValueType(string className, string description = null, object members = null) =>
            JObject.FromObject(new { type = "object", isValueType = true, className = className, description = description ?? className });

        internal static JObject TEnum(string className, string descr, object members = null) =>
            JObject.FromObject(new { type = "object", isEnum = true, className = className, description = descr });

        internal static JObject TObject(string className, string description = null, bool is_null = false) =>
            is_null ?
            JObject.FromObject(new { type = "object", className = className, description = description ?? className, subtype = is_null ? "null" : null }) :
            JObject.FromObject(new { type = "object", className = className, description = description ?? className });

        internal static JObject TArray(string className, string description) => JObject.FromObject(new { type = "object", className, description, subtype = "array" });

        internal static JObject TBool(bool value) => JObject.FromObject(new { type = "boolean", value = @value, description = @value ? "true" : "false" });

        internal static JObject TSymbol(string value) => JObject.FromObject(new { type = "symbol", value = @value, description = @value });

        internal static JObject TChar(char value) => JObject.FromObject(new { type = "symbol", value = @value, description = $"{(int)value} '{@value}'" });

        /*
        	For target names with generated method names like
        		`void <ActionTSignatureTest>b__11_0 (Math.GenericStruct<int[]>)`

        	.. pass target "as `target: "void <ActionTSignatureTest>|(Math.GenericStruct<int[]>)"`
        */
        internal static JObject TDelegate(string className, string target) => JObject.FromObject(new
        {
            __custom_type = "delegate",
            className = className,
            target = target
        });

        internal static JObject TPointer(string type_name, bool is_null = false) => JObject.FromObject(new { __custom_type = "pointer", type_name = type_name, is_null = is_null, is_void = type_name.StartsWith("void*") });

        internal static JObject TIgnore() => JObject.FromObject(new { __custom_type = "ignore_me" });

        internal static JObject TGetter(string type, JObject value = null) => JObject.FromObject(new { __custom_type = "getter", type_name = type, value = value});

        internal static JObject TDateTime(DateTime dt) => JObject.FromObject(new
        {
            __custom_type = "datetime",
            binary = dt.ToBinary()
        });

        internal async Task LoadAssemblyDynamically(string asm_file, string pdb_file)
        {
            // Simulate loading an assembly into the framework
            byte[] bytes = File.ReadAllBytes(asm_file);
            string asm_base64 = Convert.ToBase64String(bytes);

            string pdb_base64 = null;
            if (pdb_file != null)
            {
                bytes = File.ReadAllBytes(pdb_file);
                pdb_base64 = Convert.ToBase64String(bytes);
            }

            var load_assemblies = JObject.FromObject(new
            {
                expression = $"{{ let asm_b64 = '{asm_base64}'; let pdb_b64 = '{pdb_base64}'; invoke_static_method('[debugger-test] LoadDebuggerTest:LoadLazyAssembly', asm_b64, pdb_b64); }}"
            });

            Result load_assemblies_res = await cli.SendCommand("Runtime.evaluate", load_assemblies, token);
            load_assemblies_res.AssertOk();
        }

        internal async Task<JObject> LoadAssemblyDynamicallyALCAndRunMethod(string asm_file, string pdb_file, string type_name, string method_name)
        {
            // Simulate loading an assembly into the framework
            byte[] bytes = File.ReadAllBytes(asm_file);
            string asm_base64 = Convert.ToBase64String(bytes);

            string pdb_base64 = null;
            if (pdb_file != null)
            {
                bytes = File.ReadAllBytes(pdb_file);
                pdb_base64 = Convert.ToBase64String(bytes);
            }

            Task<JObject> bpResolved = WaitForBreakpointResolvedEvent();
            var load_assemblies = JObject.FromObject(new
            {
                expression = $"{{ let asm_b64 = '{asm_base64}'; let pdb_b64 = '{pdb_base64}'; invoke_static_method('[debugger-test] LoadDebuggerTestALC:LoadLazyAssemblyInALC', asm_b64, pdb_b64); }}"
            });

            Result load_assemblies_res = await cli.SendCommand("Runtime.evaluate", load_assemblies, token);
            load_assemblies_res.AssertOk();
            await bpResolved;

            var run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_static_method('[debugger-test] LoadDebuggerTestALC:RunMethodInALC', '" + type_name + "',  '" + method_name + "'); }, 1);"
            });

            await cli.SendCommand("Runtime.evaluate", run_method, token);
            return await WaitFor(Inspector.PAUSE);
        }

        internal async Task<JObject> LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(string asm_file, string pdb_file, string class_name, string method_name, bool expectBpResolvedEvent, params string[] sourcesToWait)
        {
            byte[] bytes = File.ReadAllBytes(asm_file);
            string asm_base64 = Convert.ToBase64String(bytes);
            bytes = File.ReadAllBytes(pdb_file);
            string pdb_base64 = Convert.ToBase64String(bytes);

            string expression = $"let asm_b64 = '{asm_base64}'; let pdb_b64 = '{pdb_base64}';";
            expression = $"{{ {expression} invoke_static_method('[debugger-test] TestHotReloadUsingSDB:LoadLazyHotReload', asm_b64, pdb_b64); }}";
            var load_assemblies = JObject.FromObject(new
            {
                expression
            });

            Task eventTask = expectBpResolvedEvent
                                ? WaitForBreakpointResolvedEvent()
                                : WaitForScriptParsedEventsAsync(sourcesToWait);
            (await cli.SendCommand("Runtime.evaluate", load_assemblies, token)).AssertOk();
            await eventTask;

            var run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_static_method('[debugger-test] TestHotReloadUsingSDB:RunMethod', '" + class_name + "', '" + method_name + "'); }, 1);"
            });

            (await cli.SendCommand("Runtime.evaluate", run_method, token)).AssertOk();
            return await WaitFor(Inspector.PAUSE);
        }

        internal async Task<JObject> LoadAssemblyAndTestHotReloadUsingSDB(string asm_file_hot_reload, string class_name, string method_name, int id, Func<Task> rebindBreakpoint = null, bool rebindBeforeUpdates = false)
        {
            await cli.SendCommand("Debugger.resume", null, token);
            var bytes = File.ReadAllBytes($"{asm_file_hot_reload}.{id}.dmeta");
            string dmeta1 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.{id}.dil");
            string dil1 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.{id}.dpdb");
            string dpdb1 = Convert.ToBase64String(bytes);

            var run_method = JObject.FromObject(new
            {
                expression = "invoke_static_method('[debugger-test] TestHotReloadUsingSDB:GetModuleGUID');"
            });

            var moduleGUID_res = await cli.SendCommand("Runtime.evaluate", run_method, token);

            Assert.True(moduleGUID_res.IsOk);
            var moduleGUID = moduleGUID_res.Value["result"]["value"];

            var applyUpdates = JObject.FromObject(new
            {
                moduleGUID,
                dmeta = dmeta1,
                dil = dil1,
                dpdb = dpdb1
            });

            if (rebindBreakpoint != null && rebindBeforeUpdates)
                await rebindBreakpoint();

            await cli.SendCommand("DotnetDebugger.applyUpdates", applyUpdates, token);

            if (rebindBreakpoint != null && !rebindBeforeUpdates)
                await rebindBreakpoint();

            run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_static_method('[debugger-test] TestHotReloadUsingSDB:RunMethod', '" + class_name + "', '" + method_name + "'); }, 1);"
            });
            await cli.SendCommand("Runtime.evaluate", run_method, token);
            return await WaitFor(Inspector.PAUSE);
        }

        internal async Task<JObject> LoadAssemblyAndTestHotReload(string asm_file, string pdb_file, string asm_file_hot_reload, string class_name, string method_name, bool expectBpResolvedEvent, string[] sourcesToWait, string methodName2 = "", string methodName3 = "")
        {
            byte[] bytes = File.ReadAllBytes(asm_file);
            string asm_base64 = Convert.ToBase64String(bytes);
            bytes = File.ReadAllBytes(pdb_file);
            string pdb_base64 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.1.dmeta");
            string dmeta1 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.1.dil");
            string dil1 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.1.dpdb");
            string dpdb1 = Convert.ToBase64String(bytes);


            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.2.dmeta");
            string dmeta2 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.2.dil");
            string dil2 = Convert.ToBase64String(bytes);

            bytes = File.ReadAllBytes($"{asm_file_hot_reload}.2.dpdb");
            string dpdb2 = Convert.ToBase64String(bytes);

            string expression = $"let asm_b64 = '{asm_base64}'; let pdb_b64 = '{pdb_base64}';";
            expression = $"{expression} let dmeta1 = '{dmeta1}'; let dil1 = '{dil1}'; let dpdb1 = '{dpdb1}';";
            expression = $"{expression} let dmeta2 = '{dmeta2}'; let dil2 = '{dil2}'; let dpdb2 = '{dpdb2}';";
            expression = $"{{ {expression} invoke_static_method('[debugger-test] TestHotReload:LoadLazyHotReload', asm_b64, pdb_b64, dmeta1, dil1, dpdb1, dmeta2, dil2, dpdb2); }}";
            var load_assemblies = JObject.FromObject(new
            {
                expression
            });

            Task eventTask = expectBpResolvedEvent
                                ? WaitForBreakpointResolvedEvent()
                                : WaitForScriptParsedEventsAsync(sourcesToWait);
            (await cli.SendCommand("Runtime.evaluate", load_assemblies, token)).AssertOk();
            await eventTask;

            if (methodName2 == "")
                methodName2 = method_name;
            if (methodName3 == "")
                methodName3 = method_name;

            var run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_static_method('[debugger-test] TestHotReload:RunMethod', '" + class_name + "', '" + method_name + "', '" + methodName2 + "', '" + methodName3 + "'); }, 1);"
            });

            await cli.SendCommand("Runtime.evaluate", run_method, token);
            return await WaitFor(Inspector.PAUSE);
        }

        public Task<JObject> WaitForBreakpointResolvedEvent() => WaitForEventAsync("Debugger.breakpointResolved");

        public async Task<JObject> WaitForEventAsync(string eventName)
        {
            try
            {
                return await insp.WaitForEvent(eventName);
            }
            catch (TaskCanceledException)
            {
                throw new XunitException($"Timed out waiting for {eventName} event");
            }
        }

        internal virtual async Task SetJustMyCode(bool enabled)
        {
            var req = JObject.FromObject(new { JustMyCodeStepping = enabled });
            var res = await cli.SendCommand("DotnetDebugger.setDebuggerProperty", req, token);
            Assert.True(res.IsOk);
            Assert.Equal(res.Value["justMyCodeEnabled"], enabled);
        }

        internal async Task SetSymbolOptions(JObject param)
        {
            var res = await cli.SendCommand("DotnetDebugger.setSymbolOptions", param, token);
            Assert.True(res.IsOk);
        }

        internal async Task CheckEvaluateFail(string id, params (string expression, string message)[] args)
        {
            foreach (var arg in args)
            {
                (_, Result _res) = await EvaluateOnCallFrame(id, arg.expression, expect_ok: false).ConfigureAwait(false);
                // different response structure for Chrome and Firefox:
                string errorMessage = _res.Error["preview"] == null ? _res.Error["result"]?["description"]?.Value<string>() : _res.Error["preview"]?["message"]?.Value<string>();
                AssertEqual(arg.message, errorMessage, $"Expression '{arg.expression}' - wrong error message");
            }
        }
    }

    class DotnetObjectId
    {
        public string Scheme { get; }
        public string Value { get; }

        JObject value_json;
        public JObject ValueAsJson
        {
            get
            {
                if (value_json == null)
                {
                    try
                    {
                        value_json = JObject.Parse(Value);
                    }
                    catch (JsonReaderException) { }
                }

                return value_json;
            }
        }

        public static bool TryParse(JToken jToken, out DotnetObjectId objectId) => TryParse(jToken?.Value<string>(), out objectId);

        public static bool TryParse(string id, out DotnetObjectId objectId)
        {
            objectId = null;
            if (id == null)
            {
                return false;
            }

            if (!id.StartsWith("dotnet:"))
            {
                return false;
            }

            var parts = id.Split(":", 3);

            if (parts.Length < 3)
            {
                return false;
            }

            objectId = new DotnetObjectId(parts[1], parts[2]);

            return true;
        }

        public DotnetObjectId(string scheme, string value)
        {
            Scheme = scheme;
            Value = value;
        }

        public override string ToString() => $"dotnet:{Scheme}:{Value}";
    }

    enum StepKind
    {
        Into,
        Over,
        Out,
        Resume
    }
}
