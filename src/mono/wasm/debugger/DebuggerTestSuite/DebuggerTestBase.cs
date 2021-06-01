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

namespace DebuggerTests
{
    public class DebuggerTestBase : IAsyncLifetime
    {
        internal InspectorClient cli;
        internal Inspector insp;
        protected CancellationToken token;
        protected Dictionary<string, string> scripts;
        protected Task startTask;

        public bool UseCallFunctionOnBeforeGetProperties;

        static string s_debuggerTestAppPath;
        protected static string DebuggerTestAppPath
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
            var asm_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var test_app_path = Path.Combine(asm_dir, "..", "..", "..", "debugger-test", "Debug", "publish");
            if (File.Exists(Path.Combine(test_app_path, "debugger-driver.html")))
                return test_app_path;

            throw new Exception($"Could not figure out debugger-test app path ({test_app_path}) based on the test suite location ({asm_dir})");
        }

        static string[] PROBE_LIST = {
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            "/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
            "/usr/bin/chromium",
            "C:/Program Files/Google/Chrome/Application/chrome.exe",
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
            // the debugger is working in locale of the debugged application. For example Datetime.ToString()
            // we want the test to mach it. We are also starting chrome with --lang=en-US
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            insp = new Inspector();
            cli = insp.Client;
            scripts = SubscribeToScripts(insp);

            startTask = TestHarnessProxy.Start(FindChromePath(), DebuggerTestAppPath, driver);
        }

        public virtual async Task InitializeAsync()
        {
            Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> fn = (client, token) =>
             {
                 Func<string, (string, Task<Result>)> getInitCmdFn = (cmd) => (cmd, client.SendCommand(cmd, null, token));
                 var init_cmds = new List<(string, Task<Result>)>
                 {
                    getInitCmdFn("Profiler.enable"),
                    getInitCmdFn("Runtime.enable"),
                    getInitCmdFn("Debugger.enable"),
                    getInitCmdFn("Runtime.runIfWaitingForDebugger")
                 };

                 return init_cmds;
             };

            await Ready();
            await insp.OpenSessionAsync(fn);
        }

        public virtual async Task DisposeAsync() => await insp.ShutdownAsync().ConfigureAwait(false);

        public Task Ready() => startTask;

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
                    var dbgUrl = args["url"]?.Value<string>();
                    var arrStr = dbgUrl.Split("/");
                    dicScriptsIdToUrl[script_id] = arrStr[arrStr.Length - 1];
                    dicFileToUrl[new Uri(url).AbsolutePath] = url;
                }
                await Task.FromResult(0);
            });
            return dicScriptsIdToUrl;
        }

        internal async Task CheckInspectLocalsAtBreakpointSite(string url_key, int line, int column, string function_name, string eval_expression,
            Action<JToken> test_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false)
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
                locals_fn: (locals) =>
                {
                    if (test_fn != null)
                        test_fn(locals);
                }
            );
        }

        // sets breakpoint by method name and line offset
        internal async Task CheckInspectLocalsAtBreakpointSite(string type, string method, int line_offset, string bp_function_name, string eval_expression,
            Action<JToken> locals_fn = null, Func<JObject, Task> wait_for_event_fn = null, bool use_cfo = false, string assembly = "debugger-test.dll", int col = 0)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            var bp = await SetBreakpointInMethod(assembly, type, method, line_offset, col);

            var args = JObject.FromObject(new { expression = eval_expression });
            var res = await cli.SendCommand("Runtime.evaluate", args, token);
            if (!res.IsOk)
            {
                Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }

            var pause_location = await insp.WaitFor(Inspector.PAUSE);

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
                locals_fn(locals);
            }
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

        internal JToken Check(JToken locals, string name, JObject expected)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], expected, name).Wait();
            return l;
        }

        internal JToken CheckObject(JToken locals, string name, string class_name, string subtype = null, bool is_null = false, string description = null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            var val = l["value"];
            CheckValue(val, TObject(class_name, is_null: is_null, description: description), name).Wait();
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

        internal async Task CheckDateTimeValue(JToken value, DateTime expected, string label = "")
        {
            await CheckDateTimeMembers(value, expected, label);

            var res = await InvokeGetter(JObject.FromObject(new { value = value }), "Date");
            await CheckDateTimeMembers(res.Value["result"], expected.Date, label);

            // FIXME: check some float properties too

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

        internal JToken CheckValueType(JToken locals, string name, string class_name, string description=null)
        {
            var l = GetAndAssertObjectWithName(locals, name);
            CheckValue(l["value"], TValueType(class_name, description: description), name).Wait();
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
            var wait_res = await insp.WaitFor(Inspector.PAUSE);
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

        internal async Task<JObject> StepAndCheck(StepKind kind, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null, int times = 1)
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

        internal async Task<JObject> EvaluateAndCheck(string expression, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null) => await SendCommandAndCheck(
            JObject.FromObject(new { expression = expression }),
            "Runtime.evaluate", script_loc, line, column, function_name,
            wait_for_event_fn: wait_for_event_fn,
            locals_fn: locals_fn);

        internal async Task<JObject> SendCommandAndCheck(JObject args, string method, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Action<JToken> locals_fn = null, string waitForEvent = Inspector.PAUSE)
        {
            var res = await cli.SendCommand(method, args, token);
            if (!res.IsOk)
            {
                Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
                Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
            }

            var wait_res = await insp.WaitFor(waitForEvent);
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
                    locals_fn(locals);
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

            try
            {
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
                Console.WriteLine($"{ex.Message} \nExpected: {exp_val} \nActual: {actual_val}");
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
        internal async Task<JToken> GetProperties(string id, JToken fn_args = null, bool? own_properties = null, bool? accessors_only = null, bool expect_ok = true)
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

        internal async Task<(JToken, Result)> EvaluateOnCallFrame(string id, string expression, bool expect_ok = true)
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
            Assert.True(expect_ok ? res.IsOk : res.IsErr);

            return res;
        }

        internal async Task<Result> SetBreakpoint(string url_key, int line, int column, bool expect_ok = true, bool use_regex = false, string condition = "")
        {
            var bp1_req = !use_regex ?
                JObject.FromObject(new { lineNumber = line, columnNumber = column, url = dicFileToUrl[url_key], condition }) :
                JObject.FromObject(new { lineNumber = line, columnNumber = column, urlRegex = url_key, condition });

            var bp1_res = await cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, token);
            Assert.True(expect_ok ? bp1_res.IsOk : bp1_res.IsErr);

            return bp1_res;
        }

        internal async Task<Result> SetPauseOnException(string state)
        {
            var exc_res = await cli.SendCommand("Debugger.setPauseOnExceptions", JObject.FromObject(new { state = state }), token);
            return exc_res;
        }

        internal async Task<Result> SetBreakpointInMethod(string assembly, string type, string method, int lineOffset = 0, int col = 0, string condition = "")
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

        internal static JObject TNumber(string value) =>
            JObject.FromObject(new { type = "number", value = @value.ToString(), description = value });

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
            Assert.True(load_assemblies_res.IsOk);
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
