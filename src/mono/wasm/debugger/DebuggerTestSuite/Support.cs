// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    class Inspector
    {
        // InspectorClient client;
        Dictionary<string, TaskCompletionSource<JObject>> notifications = new Dictionary<string, TaskCompletionSource<JObject>>();
        Dictionary<string, Func<JObject, CancellationToken, Task>> eventListeners = new Dictionary<string, Func<JObject, CancellationToken, Task>>();

        public const string PAUSE = "pause";
        public const string READY = "ready";

        public Task<JObject> WaitFor(string what)
        {
            if (notifications.ContainsKey(what))
                throw new Exception($"Invalid internal state, waiting for {what} while another wait is already setup");
            var n = new TaskCompletionSource<JObject>();
            notifications[what] = n;
            return n.Task;
        }

        void NotifyOf(string what, JObject args)
        {
            if (!notifications.ContainsKey(what))
                throw new Exception($"Invalid internal state, notifying of {what}, but nobody waiting");
            notifications[what].SetResult(args);
            notifications.Remove(what);
        }

        public void On(string evtName, Func<JObject, CancellationToken, Task> cb)
        {
            eventListeners[evtName] = cb;
        }

        void FailAllWaitersWithException(JObject exception)
        {
            foreach (var tcs in notifications.Values)
                tcs.SetException(new ArgumentException(exception.ToString()));
        }

        async Task OnMessage(string method, JObject args, CancellationToken token)
        {
            //System.Console.WriteLine("OnMessage " + method + args);
            switch (method)
            {
                case "Debugger.paused":
                    NotifyOf(PAUSE, args);
                    break;
                case "Mono.runtimeReady":
                    NotifyOf(READY, args);
                    break;
                case "Runtime.consoleAPICalled":
                    Console.WriteLine("CWL: {0}", args?["args"]?[0]?["value"]);
                    break;
            }
            if (eventListeners.ContainsKey(method))
                await eventListeners[method](args, token);
            else if (String.Compare(method, "Runtime.exceptionThrown") == 0)
                FailAllWaitersWithException(args);
        }

        public async Task Ready(Func<InspectorClient, CancellationToken, Task> cb = null, TimeSpan? span = null)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(span?.Milliseconds ?? 60 * 1000); //tests have 1 minute to complete by default
                var uri = new Uri($"ws://{TestHarnessProxy.Endpoint.Authority}/launch-chrome-and-connect");
                using var loggerFactory = LoggerFactory.Create(
                    builder => builder.AddConsole().AddFilter(null, LogLevel.Information));
                using (var client = new InspectorClient(loggerFactory.CreateLogger<Inspector>()))
                {
                    await client.Connect(uri, OnMessage, async token =>
                    {
                        Task[] init_cmds = {
                    client.SendCommand("Profiler.enable", null, token),
                    client.SendCommand("Runtime.enable", null, token),
                    client.SendCommand("Debugger.enable", null, token),
                    client.SendCommand("Runtime.runIfWaitingForDebugger", null, token),
                    WaitFor(READY),
                        };
                        // await Task.WhenAll (init_cmds);
                        Console.WriteLine("waiting for the runtime to be ready");
                        await init_cmds[4];
                        Console.WriteLine("runtime ready, TEST TIME");
                        if (cb != null)
                        {
                            Console.WriteLine("await cb(client, token)");
                            await cb(client, token);
                        }

                    }, cts.Token);
                    await client.Close(cts.Token);
                }
            }
        }
    }

    public class DebuggerTestBase
    {
        protected Task startTask;

        static string FindTestPath()
        {
            //FIXME how would I locate it otherwise?
            var test_path = Environment.GetEnvironmentVariable("TEST_SUITE_PATH");
            //Lets try to guest
            if (test_path != null && Directory.Exists(test_path))
                return test_path;

            var cwd = Environment.CurrentDirectory;
            Console.WriteLine("guessing from {0}", cwd);
            //tests run from DebuggerTestSuite/bin/Debug/netcoreapp2.1
            var new_path = Path.Combine(cwd, "../../../../bin/debugger-test-suite");
            if (File.Exists(Path.Combine(new_path, "debugger-driver.html")))
                return new_path;

            throw new Exception("Missing TEST_SUITE_PATH env var and could not guess path from CWD");
        }

        static string[] PROBE_LIST = {
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            "/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
        };
        static string chrome_path;

        static string FindChromePath()
        {
            if (chrome_path != null)
                return chrome_path;

            foreach (var s in PROBE_LIST)
            {
                if (File.Exists(s))
                {
                    chrome_path = s;
                    Console.WriteLine($"Using chrome path: ${s}");
                    return s;
                }
            }
            throw new Exception("Could not find an installed Chrome to use");
        }

        public DebuggerTestBase(string driver = "debugger-driver.html")
        {
            startTask = TestHarnessProxy.Start(FindChromePath(), FindTestPath(), driver);
        }

        public Task Ready() => startTask;

        internal DebugTestContext ctx;
        internal Dictionary<string, string> dicScriptsIdToUrl;
        internal Dictionary<string, string> dicFileToUrl;
        internal Dictionary<string, string> SubscribeToScripts(Inspector insp)
        {
            dicScriptsIdToUrl = new Dictionary<string, string>();
            dicFileToUrl = new Dictionary<string, string>();
            insp.On("Debugger.scriptParsed", async (args, c) =>
            {
                var script_id = args?["scriptId"]?.Value<string>();
                var url = args["url"]?.Value<string>();
                if (script_id.StartsWith("dotnet://"))
                {
                    var dbgUrl = args["dotNetUrl"]?.Value<string>();
                    var arrStr = dbgUrl.Split("/");
                    dbgUrl = arrStr[0] + "/" + arrStr[1] + "/" + arrStr[2] + "/" + arrStr[arrStr.Length - 1];
                    dicScriptsIdToUrl[script_id] = dbgUrl;
                    dicFileToUrl[dbgUrl] = args["url"]?.Value<string>();
                }
                else if (!String.IsNullOrEmpty(url))
                {
                    dicFileToUrl[new Uri(url).AbsolutePath] = url;
                }
                await Task.FromResult(0);
            });
            return dicScriptsIdToUrl;
        }

        internal async Task CheckInspectLocalsAtBreakpointSite(string url_key, int line, int column, string function_name, string eval_expression,
            Action<JToken> test_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

                var bp = await SetBreakpoint(url_key, line, column);

                await EvaluateAndCheck(
                    eval_expression, url_key, line, column,
                    function_name,
                    wait_for_event_fn: async (pause_location) =>
                   {
                        //make sure we're on the right bp

                        Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                       var top_frame = pause_location["callFrames"][0];

                       var scope = top_frame["scopeChain"][0];
                       Assert.Equal("dotnet:scope:0", scope["object"]["objectId"]);
                       if (wait_for_event_fn != null)
                           await wait_for_event_fn(pause_location);
                       else
                           await Task.CompletedTask;
                   },
                    locals_fn: (locals) =>
                    {
                        if (test_fn != null)
                            test_fn(locals);
                    }
                );
            });
        }

        // sets breakpoint by method name and line offset
        internal async Task CheckInspectLocalsAtBreakpointSite(string type, string method, int line_offset, string bp_function_name, string eval_expression,
            Action<JToken> locals_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false, string assembly = "debugger-test.dll", int col = 0)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

                var bp = await SetBreakpointInMethod(assembly, type, method, line_offset, col);

                var args = JObject.FromObject(new { expression = eval_expression });
                var res = await ctx.cli.SendCommand("Runtime.evaluate", args, ctx.token);
                if (!res.IsOk)
                {
                    Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                    Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
                }

                var pause_location = await ctx.insp.WaitFor(Inspector.PAUSE);

                if (bp_function_name != null)
                    Assert.Equal(bp_function_name, pause_location["callFrames"]?[0]?["functionName"]?.Value<string>());

                Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                var top_frame = pause_location["callFrames"][0];

                var scope = top_frame["scopeChain"][0];
                Assert.Equal("dotnet:scope:0", scope["object"]["objectId"]);

                if (wait_for_event_fn != null)
                    await wait_for_event_fn(pause_location);

                if (locals_fn != null)
                {
                    var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                    locals_fn(locals);
                }
            });
        }

        internal void CheckLocation(string script_loc, int line, int column, Dictionary<string, string> scripts, JToken location)
        {
            var loc_str = $"{ scripts[location["scriptId"].Value<string>()] }" +
                $"#{ location["lineNumber"].Value<int>() }" +
                $"#{ location["columnNumber"].Value<int>() }";

            var expected_loc_str = $"{script_loc}#{line}#{column}";
            Assert.Equal(expected_loc_str, loc_str);
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

        internal void CheckString(JToken locals, string name, string value)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TString(value), name).Wait();
        }

        internal JToken CheckSymbol(JToken locals, string name, string value)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TSymbol(value), name).Wait();
            return l;
        }

        internal JToken CheckObject(JToken locals, string name, string class_name, string subtype = null, bool is_null = false)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            var val = l["value"];
            CheckValue(val, TObject(class_name, is_null: is_null), name).Wait();
            Assert.True(val["isValueType"] == null || !val["isValueType"].Value<bool>());

            return l;
        }

        internal async Task<JToken> CheckPointerValue(JToken locals, string name, JToken expected, string label = null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            await CheckValue(l["value"], expected, $"{label ?? String.Empty}-{name}");
            return l;
        }

        internal async Task CheckDateTime(JToken locals, string name, DateTime expected)
        {
            var obj = GetAndAssertObjectWithName(locals, name);
            await CheckDateTimeValue(obj["value"], expected);
        }

        internal async Task CheckDateTimeValue(JToken value, DateTime expected)
        {
            await CheckDateTimeMembers(value, expected);

            var res = await InvokeGetter(JObject.FromObject(new { value = value }), "Date");
            await CheckDateTimeMembers(res.Value["result"], expected.Date);

            // FIXME: check some float properties too

            async Task CheckDateTimeMembers(JToken v, DateTime exp_dt)
            {
                AssertEqual("System.DateTime", v["className"]?.Value<string>(), "className");
                AssertEqual(exp_dt.ToString(), v["description"]?.Value<string>(), "description");

                var members = await GetProperties(v["objectId"]?.Value<string>());

                // not checking everything
                CheckNumber(members, "Year", exp_dt.Year);
                CheckNumber(members, "Month", exp_dt.Month);
                CheckNumber(members, "Day", exp_dt.Day);
                CheckNumber(members, "Hour", exp_dt.Hour);
                CheckNumber(members, "Minute", exp_dt.Minute);
                CheckNumber(members, "Second", exp_dt.Second);
            }
        }

        internal JToken CheckBool(JToken locals, string name, bool expected)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TBool(expected), name).Wait();
            return l;
        }

        internal void CheckContentValue(JToken token, string value)
        {
            var val = token["value"].Value<string>();
            Assert.Equal(value, val);
        }

        internal JToken CheckValueType(JToken locals, string name, string class_name)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TValueType(class_name), name).Wait();
            return l;
        }

        internal JToken CheckEnum(JToken locals, string name, string class_name, string descr)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TEnum(class_name, descr), name).Wait();
            return l;
        }

        internal void CheckArray(JToken locals, string name, string class_name, int length)
           => CheckValue(
                GetAndAssertObjectWithName(locals, name)["value"],
                TArray(class_name, length), name).Wait();

        internal JToken GetAndAssertObjectWithName(JToken obj, string name)
        {
            var l = obj.FirstOrDefault(jt => jt["name"]?.Value<string>() == name);
            if (l == null)
                Assert.True(false, $"Could not find variable '{name}'");
            return l;
        }

        internal async Task<Result> SendCommand(string method, JObject args)
        {
            var res = await ctx.cli.SendCommand(method, args, ctx.token);
            if (!res.IsOk)
            {
                Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }
            return res;
        }

        internal async Task<Result> Evaluate(string expression)
        {
            return await SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = expression }));
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
            var wait_res = await ctx.insp.WaitFor(Inspector.PAUSE);
            AssertLocation(wait_res, "locals_inner");
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

            var res = await ctx.cli.SendCommand("Runtime.callFunctionOn", req, ctx.token);
            Assert.True(expect_ok == res.IsOk, $"InvokeGetter failed for {req} with {res}");

            return res;
        }

        internal async Task<JObject> StepAndCheck(StepKind kind, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null, int times = 1)
        {
            for (int i = 0; i < times - 1; i++)
            {
                await SendCommandAndCheck(null, $"Debugger.step{kind.ToString()}", null, -1, -1, null);
            }

            // Check for method/line etc only at the last step
            return await SendCommandAndCheck(
                null, $"Debugger.step{kind.ToString()}", script_loc, line, column, function_name,
                wait_for_event_fn: wait_for_event_fn,
                locals_fn: locals_fn);
        }

        internal async Task<JObject> EvaluateAndCheck(string expression, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null) => await SendCommandAndCheck(
            JObject.FromObject(new { expression = expression }),
            "Runtime.evaluate", script_loc, line, column, function_name,
            wait_for_event_fn: wait_for_event_fn,
            locals_fn: locals_fn);

        internal async Task<JObject> SendCommandAndCheck(JObject args, string method, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null, string waitForEvent = Inspector.PAUSE)
        {
            var res = await ctx.cli.SendCommand(method, args, ctx.token);
            if (!res.IsOk)
            {
                Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }

            var wait_res = await ctx.insp.WaitFor(waitForEvent);

            if (function_name != null)
                Assert.Equal(function_name, wait_res["callFrames"]?[0]?["functionName"]?.Value<string>());

            if (script_loc != null)
                CheckLocation(script_loc, line, column, ctx.scripts, wait_res["callFrames"][0]["location"]);

            if (wait_for_event_fn != null)
                await wait_for_event_fn(wait_res);

            if (locals_fn != null)
            {
                var locals = await GetProperties(wait_res["callFrames"][0]["callFrameId"].Value<string>());
                locals_fn(locals);
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
                        var get = actual_val["get"];
                        Assert.True(get != null, $"[{label}] No `get` found. {(actual_val != null ? "Make sure to pass the container object for testing getters, and not the ['value']" : String.Empty)}");

                        AssertEqual("Function", get["className"]?.Value<string>(), $"{label}-className");
                        AssertStartsWith($"get {exp_val["type_name"]?.Value<string>()} ()", get["description"]?.Value<string>(), $"{label}-description");
                        AssertEqual("function", get["type"]?.Value<string>(), $"{label}-type");

                        break;
                    }

                case "ignore_me":
                    // nothing to check ;)
                    break;

                default:
                    throw new ArgumentException($"{ctype} not supported");
            }
        }

        internal async Task CheckProps(JToken actual, object exp_o, string label, int num_fields = -1)
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

                    AssertEqual(i.ToString(), act_i["name"]?.Value<string>(), $"{label}-[{i}].name");
                    if (exp_i != null)
                        await CheckValue(act_i["value"], exp_i, $"{label}-{i}th value");
                }

                return;
            }

            // Not an array
            var exp = exp_o as JObject;
            if (exp == null)
                exp = JObject.FromObject(exp_o);

            num_fields = num_fields < 0 ? exp.Values<JToken>().Count() : num_fields;
            Assert.True(num_fields == actual.Count(), $"[{label}] Number of fields don't match, Expected: {num_fields}, Actual: {actual.Count()}");

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

                var actual_val = actual_obj["value"];
                if (exp_val.Type == JTokenType.Array)
                {
                    var actual_props = await GetProperties(actual_val["objectId"]?.Value<string>());
                    await CheckProps(actual_props, exp_val, $"{label}-{exp_name}");
                }
                else if (exp_val["__custom_type"] != null && exp_val["__custom_type"]?.Value<string>() == "getter")
                {
                    // hack: for getters, actual won't have a .value
                    await CheckCustomType(actual_obj, exp_val, $"{label}#{exp_name}");
                }
                else
                {
                    await CheckValue(actual_val, exp_val, $"{label}#{exp_name}");
                }
            }
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

            foreach (var jp in exp_val.Values<JProperty>())
            {
                if (jp.Value.Type == JTokenType.Object)
                {
                    var new_val = await GetProperties(actual_val["objectId"].Value<string>());
                    await CheckProps(new_val, jp.Value, $"{label}-{actual_val["objectId"]?.Value<string>()}");

                    continue;
                }

                var exp_val_str = jp.Value.Value<string>();
                bool null_or_empty_exp_val = String.IsNullOrEmpty(exp_val_str);

                var actual_field_val = actual_val.Values<JProperty>().FirstOrDefault(a_jp => a_jp.Name == jp.Name);
                var actual_field_val_str = actual_field_val?.Value?.Value<string>();
                if (null_or_empty_exp_val && String.IsNullOrEmpty(actual_field_val_str))
                    continue;

                Assert.True(actual_field_val != null, $"[{label}] Could not find value field named {jp.Name}");

                Assert.True(exp_val_str == actual_field_val_str,
                    $"[{label}] Value for json property named {jp.Name} didn't match.\n" +
                    $"Expected: {jp.Value.Value<string>()}\n" +
                    $"Actual:   {actual_field_val.Value.Value<string>()}");
            }
        }

        internal async Task<JToken> GetLocalsForFrame(JToken frame, string script_loc, int line, int column, string function_name)
        {
            CheckLocation(script_loc, line, column, ctx.scripts, frame["location"]);
            Assert.Equal(function_name, frame["functionName"].Value<string>());

            return await GetProperties(frame["callFrameId"].Value<string>());
        }

        internal async Task<JToken> GetObjectOnFrame(JToken frame, string name)
        {
            var locals = await GetProperties(frame["callFrameId"].Value<string>());
            return await GetObjectOnLocals(locals, name);
        }

        // Find an object with @name, *fetch* the object, and check against @o
        internal async Task<JToken> CompareObjectPropertiesFor(JToken locals, string name, object o, string label = null, int num_fields = -1)
        {
            if (label == null)
                label = name;
            var props = await GetObjectOnLocals(locals, name);
            try
            {
                if (o != null)
                    await CheckProps(props, o, label, num_fields);
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

        /* @fn_args is for use with `Runtime.callFunctionOn` only */
        internal async Task<JToken> GetProperties(string id, JToken fn_args = null, bool expect_ok = true)
        {
            if (ctx.UseCallFunctionOnBeforeGetProperties && !id.StartsWith("dotnet:scope:"))
            {
                var fn_decl = "function () { return this; }";
                var cfo_args = JObject.FromObject(new
                {
                    functionDeclaration = fn_decl,
                    objectId = id
                });
                if (fn_args != null)
                    cfo_args["arguments"] = fn_args;

                var result = await ctx.cli.SendCommand("Runtime.callFunctionOn", cfo_args, ctx.token);
                AssertEqual(expect_ok, result.IsOk, $"Runtime.getProperties returned {result.IsOk} instead of {expect_ok}, for {cfo_args.ToString()}, with Result: {result}");
                if (!result.IsOk)
                    return null;
                id = result.Value["result"]?["objectId"]?.Value<string>();
            }

            var get_prop_req = JObject.FromObject(new
            {
                objectId = id
            });

            var frame_props = await ctx.cli.SendCommand("Runtime.getProperties", get_prop_req, ctx.token);
            AssertEqual(expect_ok, frame_props.IsOk, $"Runtime.getProperties returned {frame_props.IsOk} instead of {expect_ok}, for {get_prop_req}, with Result: {frame_props}");
            if (!frame_props.IsOk)
                return null;

            var locals = frame_props.Value["result"];
            // FIXME: Should be done when generating the list in library_mono.js, but not sure yet
            //        whether to remove it, and how to do it correctly.
            if (locals is JArray)
            {
                foreach (var p in locals)
                {
                    if (p["name"]?.Value<string>() == "length" && p["enumerable"]?.Value<bool>() != true)
                    {
                        p.Remove();
                        break;
                    }
                }
            }

            return locals;
        }

        internal async Task<JToken> EvaluateOnCallFrame(string id, string expression)
        {
            var evaluate_req = JObject.FromObject(new
            {
                callFrameId = id,
                expression = expression
            });

            var frame_evaluate = await ctx.cli.SendCommand("Debugger.evaluateOnCallFrame", evaluate_req, ctx.token);
            if (!frame_evaluate.IsOk)
                Assert.True(false, $"Debugger.evaluateOnCallFrame failed for {evaluate_req.ToString()}, with Result: {frame_evaluate}");

            var evaluate_result = frame_evaluate.Value["result"];
            return evaluate_result;
        }

        internal async Task<Result> SetBreakpoint(string url_key, int line, int column, bool expect_ok = true, bool use_regex = false)
        {
            var bp1_req = !use_regex ?
                JObject.FromObject(new { lineNumber = line, columnNumber = column, url = dicFileToUrl[url_key], }) :
                JObject.FromObject(new { lineNumber = line, columnNumber = column, urlRegex = url_key, });

            var bp1_res = await ctx.cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, ctx.token);
            Assert.True(expect_ok ? bp1_res.IsOk : bp1_res.IsErr);

            return bp1_res;
        }

        internal async Task<Result> SetPauseOnException(string state)
        {
            var exc_res = await ctx.cli.SendCommand("Debugger.setPauseOnExceptions", JObject.FromObject(new { state = state }), ctx.token);
            return exc_res;
        }

        internal async Task<Result> SetBreakpointInMethod(string assembly, string type, string method, int lineOffset = 0, int col = 0)
        {
            var req = JObject.FromObject(new { assemblyName = assembly, typeName = type, methodName = method, lineOffset = lineOffset });

            // Protocol extension
            var res = await ctx.cli.SendCommand("DotnetDebugger.getMethodLocation", req, ctx.token);
            Assert.True(res.IsOk);

            var m_url = res.Value["result"]["url"].Value<string>();
            var m_line = res.Value["result"]["line"].Value<int>();

            var bp1_req = JObject.FromObject(new
            {
                lineNumber = m_line + lineOffset,
                columnNumber = col,
                url = m_url
            });

            res = await ctx.cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, ctx.token);
            Assert.True(res.IsOk);

            return res;
        }

        internal void AssertEqual(object expected, object actual, string label) => Assert.True(expected?.Equals(actual),
            $"[{label}]\n" +
            $"Expected: {expected?.ToString()}\n" +
            $"Actual:   {actual?.ToString()}\n");

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
            TObject("string", is_null: true) :
            JObject.FromObject(new { type = "string", value = @value });

        internal static JObject TNumber(int value) =>
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = value.ToString() });

        internal static JObject TNumber(uint value) =>
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = value.ToString() });

        internal static JObject TValueType(string className, string description = null, object members = null) =>
            JObject.FromObject(new { type = "object", isValueType = true, className = className, description = description ?? className });

        internal static JObject TEnum(string className, string descr, object members = null) =>
            JObject.FromObject(new { type = "object", isEnum = true, className = className, description = descr });

        internal static JObject TObject(string className, string description = null, bool is_null = false) =>
            is_null ?
            JObject.FromObject(new { type = "object", className = className, description = description ?? className, subtype = is_null ? "null" : null }) :
            JObject.FromObject(new { type = "object", className = className, description = description ?? className });

        internal static JObject TArray(string className, int length = 0) => JObject.FromObject(new { type = "object", className = className, description = $"{className}({length})", subtype = "array" });

        internal static JObject TBool(bool value) => JObject.FromObject(new { type = "boolean", value = @value, description = @value ? "true" : "false" });

        internal static JObject TSymbol(string value) => JObject.FromObject(new { type = "symbol", value = @value, description = @value });

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

        internal static JObject TGetter(string type) => JObject.FromObject(new { __custom_type = "getter", type_name = type });
    }

    class DebugTestContext
    {
        public InspectorClient cli;
        public Inspector insp;
        public CancellationToken token;
        public Dictionary<string, string> scripts;

        public bool UseCallFunctionOnBeforeGetProperties;

        public DebugTestContext(InspectorClient cli, Inspector insp, CancellationToken token, Dictionary<string, string> scripts)
        {
            this.cli = cli;
            this.insp = insp;
            this.token = token;
            this.scripts = scripts;
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
        Out
    }
}
