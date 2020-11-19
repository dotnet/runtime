// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

#nullable enable

namespace DebuggerTests
{
    public class HarnessTests : DebuggerTestBase
    {
        [Fact]
        public async Task InspectorWaitForAfterMessageAlreadyReceived()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                Result res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
                Assert.True(res.IsOk, $"setBreakpoint failed with {res}");

                res = await ctx.cli.SendCommand(
                    "Runtime.evaluate",
                    JObject.FromObject(new { expression = "window.setTimeout(function() { invoke_add(); }, 0);" }),
                    ctx.token);
                Assert.True(res.IsOk, $"evaluating the function failed with {res}");

                // delay, so that we can get the Debugger.pause event
                await Task.Delay(1000);

                await insp.WaitFor(Inspector.PAUSE);
            });
        }
    }
}
