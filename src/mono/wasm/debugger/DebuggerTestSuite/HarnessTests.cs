// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
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
        public async Task TimedOutWaitingForInvalidBreakpoint()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 100, 0);
            var tce = await Assert.ThrowsAsync<TaskCanceledException>(
                         async () => await EvaluateAndCheck("window.setTimeout(function() { invoke_add(); }, 1);", null, -1, -1, null));
            Assert.Contains("timed out", tce.Message);
        }

        [Fact]
        public async Task ExceptionThrown()
        {
            var ae = await Assert.ThrowsAsync<ArgumentException>(
                        async () => await EvaluateAndCheck("window.setTimeout(function() { non_existant_fn(); }, 1);", null, -1, -1, null));
            Assert.Contains("non_existant_fn is not defined", ae.Message);
        }

        [Fact]
        public async Task BrowserCrash() => await Assert.ThrowsAsync<WebSocketException>(async () =>
            await SendCommandAndCheck(null, "Browser.crash", null, -1, -1, null));

        [Fact]
        public async Task BrowserClose()
        {
            ArgumentException ae = await Assert.ThrowsAsync<ArgumentException>(async () =>
                                        await SendCommandAndCheck(null, "Browser.close", null, -1, -1, null));
            Assert.Contains("Inspector.detached", ae.Message);
            Assert.Contains("target_close", ae.Message);
        }

        [Fact]
        public async Task InspectorWaitForAfterMessageAlreadyReceived()
        {
            Result res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            Assert.True(res.IsOk, $"setBreakpoint failed with {res}");

            res = await cli.SendCommand(
                "Runtime.evaluate",
                JObject.FromObject(new { expression = "window.setTimeout(function() { invoke_add(); }, 0);" }),
                token);
            Assert.True(res.IsOk, $"evaluating the function failed with {res}");

            // delay, so that we can get the Debugger.pause event
            await Task.Delay(1000);

            await insp.WaitFor(Inspector.PAUSE);
        }

        [Fact]
        public async Task InspectorWaitForMessageThatNeverArrives()
        {
            var tce = await Assert.ThrowsAsync<TaskCanceledException>(async () => await insp.WaitFor("Message.that.never.arrives"));
            Assert.Contains("timed out", tce.Message);
        }
    }
}
