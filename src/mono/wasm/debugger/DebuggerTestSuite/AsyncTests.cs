// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class AsyncTests : DebuggerTests
    {
        public AsyncTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        // FIXME: method with multiple async blocks - so that we have two separate classes for that method!
        // FIXME: nested blocks
        // FIXME: Confirm the actual bp location
        // FIXME: check object properties..

        //FIXME: function name
        [ConditionalTheory(nameof(WasmSingleThreaded), nameof(RunningOnChrome))]
        [InlineData("ContinueWithStaticAsync", "DebuggerTests.AsyncTests.ContinueWithTests.ContinueWithStaticAsync.AnonymousMethod__3_0")]
        [InlineData("ContinueWithInstanceAsync", "DebuggerTests.AsyncTests.ContinueWithTests.ContinueWithInstanceAsync.AnonymousMethod__5_0")]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/86496", typeof(DebuggerTests), nameof(DebuggerTests.WasmMultiThreaded))]
        public async Task AsyncLocalsInContinueWithInstanceUsingThisBlock() => await CheckInspectLocalsAtBreakpointSite(
             "DebuggerTests.AsyncTests.ContinueWithTests", "ContinueWithInstanceUsingThisAsync", 5, "DebuggerTests.AsyncTests.ContinueWithTests.ContinueWithInstanceUsingThisAsync.AnonymousMethod__6_0",
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
         [ActiveIssue("https://github.com/dotnet/runtime/issues/86496", typeof(DebuggerTests), nameof(DebuggerTests.WasmMultiThreaded))]
         public async Task AsyncLocalsInNestedContinueWithStaticBlock() => await CheckInspectLocalsAtBreakpointSite(
              "DebuggerTests.AsyncTests.ContinueWithTests", "NestedContinueWithStaticAsync", 5, "DebuggerTests.AsyncTests.ContinueWithTests.NestedContinueWithStaticAsync",
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

        [ConditionalTheory(nameof(WasmSingleThreaded), nameof(RunningOnChrome))]
        [InlineData("Run", 246, 16, 252, 16, "RunCSharpScope")]
        [InlineData("RunContinueWith", 277, 20, 283, 20, "RunContinueWithSameVariableName")]
        [InlineData("RunNestedContinueWith", 309, 24, 315, 24, "RunNestedContinueWithSameVariableName.AnonymousMethod__1")]
        [InlineData("RunNonAsyncMethod", 334, 16, 340, 16, "RunNonAsyncMethodSameVariableName")]
        public async Task InspectLocalsWithSameNameInDifferentScopesInAsyncMethod_CSharp(string method_to_run, int line1, int col1, int line2, int col2, string func_to_pause)
            => await InspectLocalsWithSameNameInDifferentScopesInAsyncMethod(
                        $"[debugger-test] DebuggerTests.AsyncTests.VariablesWithSameNameDifferentScopes:{method_to_run}",
                        "dotnet://debugger-test.dll/debugger-async-test.cs",
                        line1,
                        col1,
                        line2,
                        col2,
                        $"DebuggerTests.AsyncTests.VariablesWithSameNameDifferentScopes.{func_to_pause}",
                        "testCSharpScope");

        [Theory]
        [InlineData("[debugger-test-vb] DebuggerTestVB.TestVbScope:Run", 14, 12, 22, 12, "DebuggerTestVB.TestVbScope.RunVBScope", "testVbScope")]
        public async Task InspectLocalsWithSameNameInDifferentScopesInAsyncMethod_VB(string method_to_run, int line1, int col1, int line2, int col2, string func_to_pause, string variable_to_inspect)
            => await InspectLocalsWithSameNameInDifferentScopesInAsyncMethod(
                        method_to_run,
                        "dotnet://debugger-test-vb.dll/debugger-test-vb.vb",
                        line1,
                        col1,
                        line2,
                        col2,
                        func_to_pause,
                        variable_to_inspect);

        private async Task InspectLocalsWithSameNameInDifferentScopesInAsyncMethod(string method_to_run, string source_to_pause, int line1, int col1, int line2, int col2, string func_to_pause, string variable_to_inspect)
        {
            var expression = $"{{ invoke_static_method('{method_to_run}'); }}";

            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                source_to_pause, line1, col1,
                func_to_pause,
                locals_fn: async (locals) =>
                {
                    await CheckString(locals, variable_to_inspect, "hello");
                    await CheckString(locals, "onlyInFirstScope", "only-in-first-scope");
                    Assert.False(locals.Any(jt => jt["name"]?.Value<string>() == "onlyInSecondScope"));
                }
            );
            await StepAndCheck(StepKind.Resume, source_to_pause, line2, col2, func_to_pause,
                locals_fn: async (locals) =>
                {
                    await CheckString(locals, variable_to_inspect, "hi");
                    await CheckString(locals, "onlyInSecondScope", "only-in-second-scope");
                    Assert.False(locals.Any(jt => jt["name"]?.Value<string>() == "onlyInFirstScope"));
                }
            );
        }

        [Fact]
        public async Task InspectLocalsInAsyncVBMethod()
        {
            var expression = $"{{ invoke_static_method('[debugger-test-vb] DebuggerTestVB.TestVbScope:Run'); }}";
            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                "dotnet://debugger-test-vb.dll/debugger-test-vb.vb", 14, 12,
                "DebuggerTestVB.TestVbScope.RunVBScope",
                locals_fn: async (locals) =>
                {
                    await CheckString(locals, "testVbScope", "hello");
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "data", 10);
                }
            );
        }

        [ConditionalFact(nameof(WasmSingleThreaded), nameof(RunningOnChrome))]
        public async Task StepOutOfAsyncMethod()
        {
            await SetJustMyCode(true);
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                "DebuggerTests.AsyncStepClass.TestAsyncStepOut2");

            await StepAndCheck(StepKind.Out, source_file, 16, 8, "DebuggerTests.AsyncStepClass.TestAsyncStepOut");
        }
    }
}
