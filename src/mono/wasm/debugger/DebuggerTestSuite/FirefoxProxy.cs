// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace DebuggerTests;

public class DebuggerTestFirefox : DebuggerTestBase
{
    internal FirefoxInspectorClient client;
    public DebuggerTestFirefox(string driver = "debugger-driver.html"):base(driver)
    {
        client = insp.Client as FirefoxInspectorClient;
    }

    internal override string[] ProbeList()
    {
        string [] ret = {
            //"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            //"/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            //"/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
            //"/usr/bin/chromium",
            "C:/Program Files/Mozilla Firefox/firefox.exe",
            //"/usr/bin/chromium-browser",
        };
        return ret;
    }

    internal override string InitParms()
    {
        return "-headless -private -start-debugger-server ";
    }

    internal override string UrlToRemoteDebugging()
    {
        return "http://localhost:6000";
    }

    internal override async Task<string> ExtractConnUrl (string str, ILogger<TestHarnessProxy> logger)
    {
        await Task.Delay(1);
        return UrlToRemoteDebugging();
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
        await insp.OpenSessionAsync(fn);
    }

    internal override Dictionary<string, string> SubscribeToScripts(Inspector insp)
    {
        dicScriptsIdToUrl = new Dictionary<string, string>();
        dicFileToUrl = new Dictionary<string, string>();
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
            await Task.FromResult(0);
        });
        return dicScriptsIdToUrl;
    }

    internal override async Task<Result> SetBreakpoint(string url_key, int line, int column, bool expect_ok = true, bool use_regex = false, string condition = "")
    {
        var bp1_req = JObject.FromObject(new { 
                type = "setBreakpoint",
                location = JObject.FromObject(new { 
                   line,
                   column, 
                   sourceUrl = dicFileToUrl[url_key]
                }),
                to = client.BreakpointActorId
            });

        var bp1_res = await cli.SendCommand("setBreakpoint", bp1_req, token);
        Assert.True(expect_ok ? bp1_res.IsOk : bp1_res.IsErr);

        return bp1_res;
    }
    internal override async Task<JObject> EvaluateAndCheck(
                                    string expression, string script_loc, int line, int column, string function_name,
                                    Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null)
    {
        var o = JObject.FromObject(new
            {
                to = client.ConsoleActorId,
                type = "evaluateJSAsync",
                text = expression,
                options = new { eager = true, mapped = new { @await = true } }
            });

       return await SendCommandAndCheck(
                    o,
                    "evaluateJSAsync", script_loc, line, column, function_name,
                    wait_for_event_fn: wait_for_event_fn,
                    locals_fn: locals_fn);
    }


    internal override void CheckLocation(string script_loc, int line, int column, Dictionary<string, string> scripts, JToken location)
    {
        var loc_str = $"{ scripts[location["actor"].Value<string>()] }" +
            $"#{ location["line"].Value<int>() }" +
            $"#{ location["column"].Value<int>() }";

        var expected_loc_str = $"{script_loc}#{line}#{column}";
        Assert.Equal(expected_loc_str, loc_str);
    }
    
    internal override async Task<JObject> SendCommandAndCheck(JObject args, string method, string script_loc, int line, int column, string function_name,
            Func<JObject, Task> wait_for_event_fn = null, Func<JToken, Task> locals_fn = null, string waitForEvent = Inspector.PAUSE)
    {

        var res = await cli.SendCommand(method, args, token);
        if (!res.IsOk)
        {
            Console.WriteLine($"Failed to run command {method} with args: {args?.ToString()}\nresult: {res.Error.ToString()}");
            Assert.True(false, $"SendCommand for {method} failed with {res.Error.ToString()}");
        }
        var wait_res = await insp.WaitFor(waitForEvent);

        var frames = await cli.SendCommand("frames", JObject.FromObject(new
            {
                to = wait_res["from"].Value<string>(),
                type = "frames",
                start = 0,
                count =  1000
            }), token);

        JToken top_frame = frames.Value["result"]?["value"]?["frames"]?[0];
        if (function_name != null)
        {
            AssertEqual(function_name, top_frame?["displayName"]?.Value<string>(), top_frame?.ToString());
        }

        if (script_loc != null && line >= 0)
            CheckLocation(script_loc, line, column, scripts, top_frame["where"]);

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
}