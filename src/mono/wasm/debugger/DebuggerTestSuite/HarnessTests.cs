// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

#nullable enable

namespace DebuggerTests
{
    public class HarnessTests : DebuggerTests
    {
        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task TimedOutWaitingForInvalidBreakpoint()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 100, 0);
            var tce = await Assert.ThrowsAsync<TaskCanceledException>(
                         async () => await EvaluateAndCheck("window.setTimeout(function() { invoke_add(); }, 1);", null, -1, -1, null));
            Assert.Contains("timed out", tce.Message);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionThrown()
        {
            var ae = await Assert.ThrowsAsync<ArgumentException>(
                        async () => await EvaluateAndCheck("window.setTimeout(function() { non_existant_fn(); }, 3000);", null, -1, -1, null));
            Assert.Contains("non_existant_fn is not defined", ae.Message);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BrowserCrash()
        {
            TaskCompletionSource<RunLoopExitState> clientRunLoopStopped = new();
            insp.Client.RunLoopStopped += (_, args) => clientRunLoopStopped.TrySetResult(args);
            try
            {
                await SendCommandAndCheck(null, "Browser.crash", null, -1, -1, null);
            }
            catch (Exception ex)
            {
                Task t = await Task.WhenAny(clientRunLoopStopped.Task, Task.Delay(10000));
                if (t != clientRunLoopStopped.Task)
                    Assert.Fail($"Proxy did not stop, as expected");
                RunLoopExitState? state = await clientRunLoopStopped.Task;
                if (state.reason != RunLoopStopReason.ConnectionClosed)
                    Assert.Fail($"Client runloop did not stop with ConnectionClosed. state: {state}.{Environment.NewLine}SendCommand had failed with {ex}");
            }
        }

        [ConditionalFact(nameof(RunningOnChrome))]
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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task InspectorWaitForMessageThatNeverArrives()
        {
            var tce = await Assert.ThrowsAsync<TaskCanceledException>(async () => await insp.WaitFor("Message.that.never.arrives"));
            Assert.Contains("timed out", tce.Message);
        }
    }
}
