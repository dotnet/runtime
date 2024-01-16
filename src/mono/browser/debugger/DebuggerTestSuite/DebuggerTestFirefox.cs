// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests;

public class DebuggerTestFirefox : DebuggerTestBase
{
    private new TimeSpan TestTimeout => base.TestTimeout * 5;
    internal FirefoxInspectorClient _client;
    public DebuggerTestFirefox(ITestOutputHelper testOutput, string driver = "debugger-driver.html", string locale = "en-US")
        : base(testOutput, driver, locale)
    {
        if (insp.Client is not FirefoxInspectorClient)
            throw new Exception($"Bug: client should be {nameof(FirefoxInspectorClient)} for use with {nameof(DebuggerTestFirefox)}");

        _client = (FirefoxInspectorClient)insp.Client;
    }

    public override async Task InitializeAsync()
    {
        Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> fn = (client, token) =>
            {
                Func<string, JObject, (string, Task<Result>)> getInitCmdFn = (cmd, args) => (cmd, client.SendCommand(cmd, args, token));
                var init_cmds = new List<(string, Task<Result>)>
                {
                    getInitCmdFn("listTabs", JObject.FromObject(new { type = "listTabs", to = "root"}))
                };

                return init_cmds;
            };

        await Ready();
        await insp.OpenSessionAsync(fn, "", TestTimeout);
    }

    internal override Dictionary<string, string> SubscribeToScripts(Inspector insp)
    {
        dicScriptsIdToUrl = new Dictionary<string, string>();
        dicFileToUrl = new Dictionary<string, string>();
        insp.On("newSource", async (args, c) =>
        {
            var script_id = args?["source"]?["actor"].Value<string>();
            var url = args?["source"]?["sourceMapBaseURL"]?.Value<string>();
            /*_testOutput.WriteLine(script_id);
            _testOutput.WriteLine(args);*/
            if (script_id.StartsWith("dotnet://"))
            {
                var dbgUrl = args?["source"]?["dotNetUrl"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dbgUrl = arrStr[0] + "/" + arrStr[1] + "/" + arrStr[2] + "/" + arrStr[arrStr.Length - 1];
                dicScriptsIdToUrl[script_id] = dbgUrl;
                dicFileToUrl[dbgUrl] = args?["source"]?["url"]?.Value<string>();
            }
            else if (!String.IsNullOrEmpty(url))
            {
                var dbgUrl = args?["source"]?["sourceMapBaseURL"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dicScriptsIdToUrl[script_id] = arrStr[arrStr.Length - 1];
                dicFileToUrl[new Uri(url).AbsolutePath] = url;
            }
            return await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
        });
        insp.On("resource-available-form", async (args, c) =>
        {
            var script_id = args?["resources"]?[0]?["actor"].Value<string>();
            var url = args?["resources"]?[0]?["url"]?.Value<string>();
            if (script_id.StartsWith("dotnet://"))
            {
                var dbgUrl = args?["resources"]?[0]?["dotNetUrl"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dbgUrl = arrStr[0] + "/" + arrStr[1] + "/" + arrStr[2] + "/" + arrStr[arrStr.Length - 1];
                dicScriptsIdToUrl[script_id] = dbgUrl;
                dicFileToUrl[dbgUrl] = args?["resources"]?[0]?["url"]?.Value<string>();
            }
            else if (!String.IsNullOrEmpty(url))
            {
                var dbgUrl = args?["resources"]?[0]?["url"]?.Value<string>();
                var arrStr = dbgUrl.Split("/");
                dicScriptsIdToUrl[script_id] = arrStr[arrStr.Length - 1];
                dicFileToUrl[new Uri(url).AbsolutePath] = url;
            }
            return await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
        });
        return dicScriptsIdToUrl;
    }

    internal override async Task<Result> SetBreakpoint(string url_key, int line, int column, bool expect_ok = true, bool use_regex = false, string condition = "")
    {
        var bp1_req = JObject.FromObject(new {
                type = "setBreakpoint",
                location = JObject.FromObject(new {
                   line = line + 1,
                   column,
                   sourceUrl = dicFileToUrl[url_key]
                }),
                to = _client.BreakpointActorId
            });

        var bp1_res = await cli.SendCommand("setBreakpoint", bp1_req, token);
        Assert.True(expect_ok == bp1_res.IsOk);
        return bp1_res;
    }
    internal override async Task<JObject> EvaluateAndCheck(
                                    string expression, string script_loc, int line, int column, string function_name,
                                    Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null)
    {
        return await SendCommandAndCheck(
                    CreateEvaluateArgs(expression),
                    "evaluateJSAsync", script_loc, line, column, function_name,
                    wait_for_event_fn: wait_for_event_fn,
                    locals_fn: locals_fn);
    }

    internal override void CheckLocation(string script_loc, int line, int column, Dictionary<string, string> scripts, JToken location)
    {
        if (location == null) //probably trying to check startLocation endLocation or functionLocation which are not available on Firefox
            return;
        int column_from_stack = -1;
        if (column != -1)
            column_from_stack = location["columnNumber"].Value<int>();

        var loc_str = $"{ scripts[location["scriptId"].Value<string>()] }" +
            $"#{ location["lineNumber"].Value<int>()}" +
            $"#{ column_from_stack }";

        var expected_loc_str = $"{script_loc}#{line+1}#{column}";
        Assert.Equal(expected_loc_str, loc_str);
    }

    internal override void CheckLocationLine(JToken location, int line)
    {
        if (location == null) //probably trying to check startLocation endLocation or functionLocation which are not available on Firefox
            return;
        Assert.Equal(location["lineNumber"].Value<int>(), line+1);
    }

    private JObject ConvertFirefoxToDefaultFormat(JArray frames, JObject wait_res)
    {
        var callFrames = new JArray();
        foreach (var frame in frames)
        {
            var callFrame = JObject.FromObject(new
            {
                functionName = frame["displayName"].Value<string>(),
                callFrameId = frame["actor"].Value<string>(),
                //functionLocation = 0,
                location =  JObject.FromObject(new
                {
                    scriptId = frame["where"]["actor"].Value<string>(),
                    lineNumber = frame["where"]["line"].Value<int>(),
                    columnNumber = frame["where"]["column"].Value<int>()
                }),
                url = scripts[frame["where"]["actor"].Value<string>()],
                scopeChain = new JArray(JObject.FromObject(new
                        {
                            type = "local",
                            name = frame["displayName"].Value<string>(),
                            @object = JObject.FromObject(new
                            {
                                type = "object",
                                className = "Object",
                                description = "Object",
                                objectId = frame["actor"].Value<string>()
                            })
                        }))
            });
            callFrames.Add(callFrame);
        }
        return JObject.FromObject(new
                {
                    callFrames,
                    reason = "other"
                });
    }

    internal override Task SetJustMyCode(bool enabled) => Task.CompletedTask;

    internal override async Task<JObject> SendCommandAndCheck(JObject args, string method, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, string waitForEvent = Inspector.PAUSE)
    {
        switch (method)
        {
            case "Debugger.resume":
                return await StepAndCheck(StepKind.Resume, script_loc, line, column, function_name, wait_for_event_fn, locals_fn);
            case "Debugger.stepInto":
                return await StepAndCheck(StepKind.Into, script_loc, line, column, function_name, wait_for_event_fn, locals_fn);
        }
        var res = await cli.SendCommand(method, args, token);
        if (!res.IsOk)
        {
            _testOutput.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
            Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
        }
        var wait_res = await WaitFor(waitForEvent);
        if (function_name != null)
        {
            AssertEqual(function_name, wait_res["callFrames"]?[0]?["functionName"]?.Value<string>(),  wait_res["callFrames"]?[0]?["functionName"]?.ToString());
        }

        if (script_loc != null && line >= 0)
            CheckLocation(script_loc, line, column, scripts, wait_res["callFrames"]?[0]?["location"]);

        if (wait_for_event_fn != null)
        {
            await wait_for_event_fn(wait_res);
        }

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

    internal JObject ConvertFromFirefoxToDefaultFormat(KeyValuePair<string, JToken> variable)
    {
        string name = variable.Key;
        JToken value = variable.Value;
        JObject variableValue = null;
        string valueType = "value";
        if (value?["type"] == null || value["type"].Value<string>() == "object" || value["type"].Value<string>() == "string")
        {
            var actor = value["value"]?["actor"]?.Value<string>();
            string type = value["value"]?["type"]?.Value<string>();
            switch (type)
            {
                case "null":
                    variableValue = JObject.FromObject(new
                        {
                            type = "object",
                            subtype = "null",
                            className = value["value"]["class"].Value<string>(),
                            description = value["value"]["class"].Value<string>()
                        });
                    if (actor != null && actor.StartsWith("dotnet:pointer:"))
                        variableValue["type"] = "symbol";
                    break;
                case "function":
                    variableValue = JObject.FromObject(new
                        {
                            type = type,
                            objectId = value["value"]["actor"].Value<string>(),
                            className = "Function",
                            description = $"get {name} ()"
                        });
                    valueType = "get";
                    break;
                case "string":
                    variableValue = JObject.FromObject(new
                        {
                            type = type,
                            objectId = value["value"]["actor"]?.Value<string>(),
                            value = value["value"]["value"]?.Value<string>(),
                            description = value["value"]["value"].Value<string>()
                        });
                    break;
                default:
                    variableValue = JObject.FromObject(new
                        {
                            type = type,
                            value = (string)null,
                            description = value["value"]?["value"]?.Value<string>() == null ? value["value"]["class"].Value<string>() : value["value"]?["value"]?.Value<string>(),
                            className = value["value"]["class"].Value<string>(),
                            objectId = actor,
                        });
                    if (actor.StartsWith("dotnet:valuetype:"))
                        variableValue["isValueType"] = true;
                    if (actor.StartsWith("dotnet:array:"))
                        variableValue["subtype"] = "array";
                    if (actor.StartsWith("dotnet:pointer:"))
                        variableValue["type"] = "object";
                    if (actor.StartsWith("dotnet:pointer:-1"))
                    {
                        variableValue["type"] = "symbol";
                        variableValue["value"] = value["value"]?["value"]?.Value<string>();
                    }
                    break;
            }
        }
        else
        {
            var description = value["value"].ToString();
            if (value["type"].Value<string>() == "boolean")
                description = description.ToLower();
                variableValue = JObject.FromObject(new
                        {
                            type = value["type"],
                            value = value["value"],
                            description
                        });
        }
        var ret = JObject.FromObject(new
        {
            name,
            writable = value["writable"] != null ? value["writable"] : false
        });
        ret[valueType] = variableValue;
        return ret;
    }

    /* @fn_args is for use with `Runtime.callFunctionOn` only */
    internal override async Task<JToken> GetProperties(string id, JToken fn_args = null, bool? own_properties = null, bool? accessors_only = null, bool expect_ok = true)
    {
        if (id.StartsWith("dotnet:scope:"))
        {
            JArray ret = new ();
            var o = JObject.FromObject(new
            {
                to = id,
                type = "getEnvironment"
            });
            var frame_props = await cli.SendCommand("getEnvironment", o, token);
            foreach (var variable in frame_props.Value["result"]["value"]["bindings"]["variables"].Value<JObject>())
            {
                var varToAdd = ConvertFromFirefoxToDefaultFormat(variable);
                ret.Add(varToAdd);
            }
            return ret;
        }
        if (id.StartsWith("dotnet:evaluationResult:") || id.StartsWith("dotnet:valuetype:") || id.StartsWith("dotnet:object:") || id.StartsWith("dotnet:array:") || id.StartsWith("dotnet:pointer:"))
        {
            JArray ret = new ();
            var o = JObject.FromObject(new
            {
                to = id,
                type = "enumProperties"
            });
            var propertyIterator = await cli.SendCommand("enumProperties", o, token);
            o = JObject.FromObject(new
            {
                to = propertyIterator.Value["result"]["value"]?["iterator"]?["actor"].Value<string>().Replace("propertyIterator", ""),
                type = "prototypeAndProperties"
            });
            var objProps = await cli.SendCommand("prototypeAndProperties", o, token);
            foreach (var prop in objProps.Value["result"]["value"]["ownProperties"].Value<JObject>())
            {
                var varToAdd = ConvertFromFirefoxToDefaultFormat(prop);
                ret.Add(varToAdd);
            }
            return ret;
        }
        return null;
    }

    internal override async Task<JObject> StepAndCheck(StepKind kind, string script_loc, int line, int column, string function_name,
    Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, int times = 1)
    {
        JObject resumeLimit = null;

        if (kind != StepKind.Resume)
        {
            resumeLimit = JObject.FromObject(new
            {
                type = kind == StepKind.Over ? "next" : kind == StepKind.Out ? "finish" : "step"
            });
        }
        var o = JObject.FromObject(new
        {
            to = _client.ThreadActorId,
            type = "resume",
            resumeLimit
        });

        for (int i = 0; i < times - 1; i++)
        {
            await SendCommandAndCheck(o, "resume", null, -1, -1, null);
        }

        // Check for method/line etc only at the last step
        return await SendCommandAndCheck(
            o, "resume", script_loc, line, column, function_name,
            wait_for_event_fn: wait_for_event_fn,
            locals_fn: locals_fn);
    }

    internal override async Task<Result> SetBreakpointInMethod(string assembly, string type, string method, int lineOffset = 0, int col = 0, string condition = "")
    {
        var req = JObject.FromObject(new { assemblyName = assembly, type = "DotnetDebugger.getMethodLocation", typeName = type, methodName = method, lineOffset = lineOffset, to = "internal" });

        // Protocol extension
        var res = await cli.SendCommand("DotnetDebugger.getMethodLocation", req, token);
        Assert.True(res.IsOk);
        var m_url = res.Value["result"]["value"]["url"].Value<string>();
        var m_line = res.Value["result"]["value"]["line"].Value<int>();
        var m_column = res.Value["result"]["value"]["column"].Value<int>();


        var bp1_req = JObject.FromObject(new {
                type = "setBreakpoint",
                location = JObject.FromObject(new {
                   line = m_line + lineOffset + 1,
                   column = col,
                   sourceUrl = m_url
                }),
                to = _client.BreakpointActorId
            });

        if (condition != "")
            bp1_req["options"] = JObject.FromObject(new { condition });

        var bp1_res = await cli.SendCommand("setBreakpoint", bp1_req, token);
        Assert.True(bp1_res.IsOk);

        var arr = new JArray(JObject.FromObject(new {
                lineNumber = m_line + lineOffset,
                columnNumber = -1
            }));

        bp1_res.Value["locations"] = arr;
        return bp1_res;
    }

    internal override async Task<(JToken, Result)> EvaluateOnCallFrame(string id, string expression, bool expect_ok = true)
    {
        var o = CreateEvaluateArgs(expression);
        var res = await cli.SendCommand("evaluateJSAsync", o, token);
        if (res.IsOk)
        {
            if (res.Value["result"]["value"] is JObject)
            {
                var actor = res.Value["result"]["value"]["actor"].Value<string>();
                var resObj = JObject.FromObject(new
                {
                    type = res.Value["result"]["value"]["type"],
                    className = res.Value["result"]["value"]["class"],
                    description = res.Value["result"]["value"]["description"],
                    objectId = actor
                });
                if (actor?.StartsWith("dotnet:valuetype:") == true)
                    resObj["isValueType"] = true;
                return (resObj, res);
            }
            return (res.Value["result"], res);
        }

        return (null, res);
    }

    internal override bool SkipProperty(string propertyName) => propertyName == "isEnum";

    internal override async Task CheckDateTimeGetter(JToken value, DateTime expected, string label = "") => await Task.CompletedTask;

    internal override string EvaluateCommand() => "evaluateJSAsync";

    internal override JObject CreateEvaluateArgs(string expression)
    {
        if (string.IsNullOrEmpty(_client.ConsoleActorId))
            throw new Exception($"Cannot create evaluate request because consoleActorId is '{_client.ConsoleActorId}");
        return JObject.FromObject(new
        {
            to = _client.ConsoleActorId,
            type = "evaluateJSAsync",
            text = expression,
            options = new { eager = true, mapped = new { @await = true } }
        });
    }

    internal override async Task<JObject> WaitFor(string what)
    {
        var wait_res = await insp.WaitFor(what);
        var frames = await cli.SendCommand("frames", JObject.FromObject(new
        {
            to = wait_res["from"].Value<string>(),
            type = "frames",
            start = 0,
            count =  1000
        }), token);

        if (frames.Value["result"]?["value"]?["frames"] is not JArray frames_arr)
            throw new Exception($"Tried to get frames after waiting for '{what}', but got unexpected result: {frames}");

        return ConvertFirefoxToDefaultFormat(frames_arr, wait_res);
    }
}
