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

    public class EvaluateOnCallFrameTests : DebuggerTestBase
    {

        [Fact]
        public async Task EvaluateThisProperties() => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", 25, 16,
            "run",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "a");
               CheckContentValue(evaluate, "1");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "b");
               CheckContentValue(evaluate, "2");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "c");
               CheckContentValue(evaluate, "3");

               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "dt");
               await CheckDateTimeValue(evaluate, new DateTime(2000, 5, 4, 3, 2, 1));
           });

        [Theory]
        [InlineData(63, 12, "EvaluateTestsStructInstanceMethod")]
        [InlineData(79, 12, "GenericInstanceMethodOnStruct<int>")]
        [InlineData(102, 12, "EvaluateTestsGenericStructInstanceMethod")]
        public async Task EvaluateThisPropertiesOnStruct(int line, int col, string method_name) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", line, col,
            method_name,
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "a");
               CheckContentValue(evaluate, "1");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "b");
               CheckContentValue(evaluate, "2");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "c");
               CheckContentValue(evaluate, "3");

               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "dateTime");
               await CheckDateTimeValue(evaluate, new DateTime(2020, 1, 2, 3, 4, 5));
           });

        [Fact]
        public async Task EvaluateParameters() => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", 25, 16,
            "run",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "g");
               CheckContentValue(evaluate, "100");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "h");
               CheckContentValue(evaluate, "200");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "valString");
               CheckContentValue(evaluate, "test");
           });

        [Fact]
        public async Task EvaluateLocals() => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", 25, 16,
            "run",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "d");
               CheckContentValue(evaluate, "101");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "e");
               CheckContentValue(evaluate, "102");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "f");
               CheckContentValue(evaluate, "103");

               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "local_dt");
               await CheckDateTimeValue(evaluate, new DateTime(2010, 9, 8, 7, 6, 5));
           });

        [Fact]
        public async Task EvaluateLocalsAsync()
        {
            var bp_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";
            int line = 249;
            int col = 12;
            var function_name = "MoveNext";
            await CheckInspectLocalsAtBreakpointSite(
                bp_loc, line, col,
                function_name,
                "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod', true); })",
                wait_for_event_fn: async (pause_location) =>
               {
                   var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

                    // sc_arg
                    {
                       var sc_arg = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "sc_arg");
                       await CheckValue(sc_arg, TObject("DebuggerTests.SimpleClass"), "sc_arg#1");

                       var sc_arg_props = await GetProperties(sc_arg["objectId"]?.Value<string>());
                       await CheckProps(sc_arg_props, new
                       {
                           X = TNumber(10),
                           Y = TNumber(45),
                           Id = TString("sc#Id"),
                           Color = TEnum("DebuggerTests.RGB", "Blue"),
                           PointWithCustomGetter = TGetter("PointWithCustomGetter")
                       }, "sc_arg_props#1");
                   }

                    // local_gs
                    {
                       var local_gs = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "local_gs");
                       await CheckValue(local_gs, TValueType("DebuggerTests.SimpleGenericStruct<int>"), "local_gs#1");

                       var local_gs_props = await GetProperties(local_gs["objectId"]?.Value<string>());
                       await CheckProps(local_gs_props, new
                       {
                           Id = TObject("string", is_null: true),
                           Color = TEnum("DebuggerTests.RGB", "Red"),
                           Value = TNumber(0)
                       }, "local_gs_props#1");
                   }

                    // step, check local_gs
                    pause_location = await StepAndCheck(StepKind.Over, bp_loc, line + 1, col, function_name);
                   {
                       var local_gs = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "local_gs");
                       await CheckValue(local_gs, TValueType("DebuggerTests.SimpleGenericStruct<int>"), "local_gs#2");

                       var local_gs_props = await GetProperties(local_gs["objectId"]?.Value<string>());
                       await CheckProps(local_gs_props, new
                       {
                           Id = TString("local_gs#Id"),
                           Color = TEnum("DebuggerTests.RGB", "Green"),
                           Value = TNumber(4)
                       }, "local_gs_props#2");
                   }

                    // step check sc_arg.Id
                    pause_location = await StepAndCheck(StepKind.Over, bp_loc, line + 2, col, function_name);
                   {
                       var sc_arg = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "sc_arg");
                       await CheckValue(sc_arg, TObject("DebuggerTests.SimpleClass"), "sc_arg#2");

                       var sc_arg_props = await GetProperties(sc_arg["objectId"]?.Value<string>());
                       await CheckProps(sc_arg_props, new
                       {
                           X = TNumber(10),
                           Y = TNumber(45),
                           Id = TString("sc_arg#Id"), // <------- This changed
                            Color = TEnum("DebuggerTests.RGB", "Blue"),
                           PointWithCustomGetter = TGetter("PointWithCustomGetter")
                       }, "sc_arg_props#2");
                   }
               });
        }

        [Fact]
        public async Task EvaluateExpressions() => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", 25, 16,
            "run",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "d + e");
               CheckContentValue(evaluate, "203");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "e + 10");
               CheckContentValue(evaluate, "112");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "a + a");
               CheckContentValue(evaluate, "2");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "this.a + this.b");
               CheckContentValue(evaluate, "3");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "\"test\" + \"test\"");
               CheckContentValue(evaluate, "testtest");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "5 + 5");
               CheckContentValue(evaluate, "10");
           });

        [Fact]
        public async Task EvaluateThisExpressions() => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-evaluate-test.cs", 25, 16,
            "run",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "this.a");
               CheckContentValue(evaluate, "1");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "this.b");
               CheckContentValue(evaluate, "2");
               evaluate = await EvaluateOnCallFrame(pause_location["callFrames"][0]["callFrameId"].Value<string>(), "this.c");
               CheckContentValue(evaluate, "3");

                // FIXME: not supported yet
                // evaluate = await EvaluateOnCallFrame (pause_location ["callFrames"][0] ["callFrameId"].Value<string> (), "this.dt");
                // await CheckDateTimeValue (evaluate, new DateTime (2000, 5, 4, 3, 2, 1));
            });
    }

}
