// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class AsyncTests : DebuggerTests
    {

        // FIXME: method with multiple async blocks - so that we have two separate classes for that method!
        // FIXME: nested blocks
        // FIXME: Confirm the actual bp location
        // FIXME: check object properties..

        //FIXME: function name
        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("ContinueWithStaticAsync", "<ContinueWithStaticAsync>b__3_0")]
        [InlineData("ContinueWithInstanceAsync", "<ContinueWithInstanceAsync>b__5_0")]
        public async Task AsyncLocalsInContinueWith(string method_name, string expected_method_name) => await CheckInspectLocalsAtBreakpointSite(
             "DebuggerTests.AsyncTests.ContinueWithTests", method_name, 5, expected_method_name,
             "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
             wait_for_event_fn: async (pause_location) =>
             {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    t = TObject("System.Threading.Tasks.Task.DelayPromise"),
                    code = TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"),
                    @this = TObject("DebuggerTests.AsyncTests.ContinueWithTests.<>c"),
                    dt = TDateTime(new DateTime(4513, 4, 5, 6, 7, 8))
                }, "locals");

                var res = await InvokeGetter(GetAndAssertObjectWithName(frame_locals, "t"), "Status");
                await CheckValue(res.Value["result"], TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"), "t.Status");
             });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task AsyncLocalsInContinueWithInstanceUsingThisBlock() => await CheckInspectLocalsAtBreakpointSite(
             "DebuggerTests.AsyncTests.ContinueWithTests", "ContinueWithInstanceUsingThisAsync", 5, "<ContinueWithInstanceUsingThisAsync>b__6_0",
             "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
             wait_for_event_fn: async (pause_location) =>
             {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    t = TObject("System.Threading.Tasks.Task.DelayPromise"),
                    code = TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"),
                    dt = TDateTime(new DateTime(4513, 4, 5, 6, 7, 8)),
                    @this = TObject("DebuggerTests.AsyncTests.ContinueWithTests")
                }, "locals");

                var res = await InvokeGetter(GetAndAssertObjectWithName(frame_locals, "t"), "Status");
                await CheckValue(res.Value["result"], TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"), "t.Status");

                res = await InvokeGetter(GetAndAssertObjectWithName(frame_locals, "this"), "Date");
                await CheckValue(res.Value["result"], TDateTime(new DateTime(2510, 1, 2, 3, 4, 5)), "this.Date");
             });

         [Fact] // NestedContinueWith
         public async Task AsyncLocalsInNestedContinueWithStaticBlock() => await CheckInspectLocalsAtBreakpointSite(
              "DebuggerTests.AsyncTests.ContinueWithTests", "NestedContinueWithStaticAsync", 5, "MoveNext",
              "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
              wait_for_event_fn: async (pause_location) =>
              {
                 var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                 await CheckProps(frame_locals, new
                 {
                     t = TObject("System.Threading.Tasks.Task.DelayPromise"),
                     code = TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"),
                     str = TString("foobar"),
                     @this = TObject("DebuggerTests.AsyncTests.ContinueWithTests.<>c__DisplayClass4_0"),
                     ncs_dt0 = TDateTime(new DateTime(3412, 4, 6, 8, 0, 2))
                 }, "locals");
              });
    }
}
