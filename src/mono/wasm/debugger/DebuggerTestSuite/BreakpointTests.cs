// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;

namespace DebuggerTests
{

    public class BreakpointTests : DebuggerTestBase
    {
        [Fact]
        public async Task CreateGoodBreakpoint()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

            Assert.EndsWith("debugger-test.cs", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal("dotnet://debugger-test.dll/debugger-test.cs", scripts[loc["scriptId"]?.Value<string>()]);
            Assert.Equal(10, loc["lineNumber"]);
            Assert.Equal(8, loc["columnNumber"]);
        }

        [Fact]
        public async Task CreateJSBreakpoint()
        {
            // Test that js breakpoints get set correctly
            // 13 24
            // 13 31
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 13, 24);

            Assert.EndsWith("debugger-driver.html", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal(13, loc["lineNumber"]);
            Assert.Equal(24, loc["columnNumber"]);

            var bp2_res = await SetBreakpoint("/debugger-driver.html", 13, 31);

            Assert.EndsWith("debugger-driver.html", bp2_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp2_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc2 = bp2_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc2["scriptId"]);
            Assert.Equal(13, loc2["lineNumber"]);
            Assert.Equal(31, loc2["columnNumber"]);
        }

        [Fact]
        public async Task CreateJS0Breakpoint()
        {
            // 13 24
            // 13 31
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 13, 0);

            Assert.EndsWith("debugger-driver.html", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal(13, loc["lineNumber"]);
            Assert.Equal(24, loc["columnNumber"]);

            var bp2_res = await SetBreakpoint("/debugger-driver.html", 13, 31);

            Assert.EndsWith("debugger-driver.html", bp2_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp2_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc2 = bp2_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc2["scriptId"]);
            Assert.Equal(13, loc2["lineNumber"]);
            Assert.Equal(31, loc2["columnNumber"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        public async Task CheckMultipleBreakpointsOnSameLine(int col)
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-array-test.cs", 219, col);
            Assert.EndsWith("debugger-array-test.cs", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            CheckLocation("dotnet://debugger-test.dll/debugger-array-test.cs", 219, 50, scripts, loc);

            var bp2_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-array-test.cs", 219, 55);
            Assert.EndsWith("debugger-array-test.cs", bp2_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp2_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc2 = bp2_res.Value["locations"]?.Value<JArray>()[0];

            CheckLocation("dotnet://debugger-test.dll/debugger-array-test.cs", 219, 55, scripts, loc2);
        }

        [Fact]
        public async Task CreateBadBreakpoint()
        {
            var bp1_req = JObject.FromObject(new
            {
                lineNumber = 8,
                columnNumber = 2,
                url = "dotnet://debugger-test.dll/this-file-doesnt-exist.cs",
            });

            var bp1_res = await cli.SendCommand("Debugger.setBreakpointByUrl", bp1_req, token);

            Assert.True(bp1_res.IsOk);
            Assert.Empty(bp1_res.Value["locations"].Values<object>());
            //Assert.Equal ((int)MonoErrorCodes.BpNotFound, bp1_res.Error ["code"]?.Value<int> ());
        }

        [Fact]
        public async Task CreateGoodBreakpointAndHit()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

            var eval_req = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_add(); }, 1);",
            });

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "IntAdd",
                wait_for_event_fn: (pause_location) =>
                {
                    Assert.Equal("other", pause_location["reason"]?.Value<string>());
                    Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                    var top_frame = pause_location["callFrames"][0];
                    Assert.Equal("IntAdd", top_frame["functionName"].Value<string>());
                    Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

                    //now check the scope
                    var scope = top_frame["scopeChain"][0];
                    Assert.Equal("local", scope["type"]);
                    Assert.Equal("IntAdd", scope["name"]);

                    Assert.Equal("object", scope["object"]["type"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);
                    return Task.CompletedTask;
                }
            );
        }

        public static TheoryData<string, string, string, bool> FalseConditions = new TheoryData<string, string, string, bool>
        {
            { "invoke_add()", "IntAdd", "0.0", false },
            { "invoke_add()", "IntAdd", "c == 40", false },
            { "invoke_add()", "IntAdd", "c < 0", false },
        };

        public static TheoryData<string, string, string, bool> TrueConditions = new TheoryData<string, string, string, bool>
        {
            { "invoke_add()", "IntAdd", "c == 30", true },
            { "invoke_add()", "IntAdd", "true", true },
            { "invoke_add()", "IntAdd", "5", true },
            { "invoke_add()", "IntAdd", "c < 40", true },
            { "invoke_use_complex()", "UseComplex", "complex.A == 10", true },
            { "invoke_add()", "IntAdd", "1.0", true },
            { "invoke_add()", "IntAdd", "\"foo\"", true },
            { "invoke_add()", "IntAdd", "\"true\"", true },
            { "invoke_add()", "IntAdd", "\"false\"", true },
        };

        public static TheoryData<string, string, string, bool> InvalidConditions = new TheoryData<string, string, string, bool>
        {
            { "invoke_add()", "IntAdd", "foo.bar", false },
            { "invoke_add()", "IntAdd", "Math.IntAdd()", false },
            { "invoke_add()", "IntAdd", "c == \"xyz\"", false },
            { "invoke_add()", "IntAdd", "Math.NonExistantProperty", false },
            { "invoke_add()", "IntAdd", "g == 40", false },
            { "invoke_add()", "IntAdd", "null", false },
        };

        [Theory]
        [MemberData(nameof(FalseConditions))]
        [MemberData(nameof(TrueConditions))]
        [MemberData(nameof(InvalidConditions))]
        public async Task ConditionalBreakpoint(string function_to_call, string method_to_stop, string condition, bool bp_stop_expected)
        {
            Result [] bps = new Result[2];
            bps[0] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 3, condition:condition);
            bps[1] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 4);
            await EvaluateAndCheck(
                "window.setTimeout(function() { " + function_to_call + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["lineNumber"].Value<int>(),
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["columnNumber"].Value<int>(),
                method_to_stop);
        }

        [Theory]
        [InlineData("c == 15", 78, 3, 78, 11)]
        [InlineData("c == 17", 78, 3, 79, 3)]
        [InlineData("g == 17", 78, 3, 79, 3)]
        [InlineData("true", 78, 3, 78, 11)]
        [InlineData("\"false\"", 78, 3, 78, 11)]
        [InlineData("\"true\"", 78, 3, 78, 11)]
        [InlineData("5", 78, 3, 78, 11)]
        [InlineData("p", 78, 3, 79, 3)]
        [InlineData("0.0", 78, 3, 79, 3)]
        public async Task JSConditionalBreakpoint(string condition, int line_bp, int column_bp, int line_expected, int column_expected)
        {
            await SetBreakpoint("/debugger-driver.html", line_bp, column_bp, condition: condition);
            await SetBreakpoint("/debugger-driver.html", 79, 3);

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10, null); }, 1);",
                "debugger-driver.html", line_expected, column_expected, "conditional_breakpoint_test");
        }

        [Theory]
        [InlineData("invoke_add_with_parms(10, 20)", "invoke_add_with_parms(10, 20)",  "IntAdd", "c == 30", true, true)]
        [InlineData("invoke_add_with_parms(5, 10)", "invoke_add_with_parms(10, 20)",  "IntAdd", "c == 30", false, true)]
        [InlineData("invoke_add_with_parms(10, 20)", "invoke_add_with_parms(5, 10)",  "IntAdd", "c == 30", true, false)]
        public async Task ConditionalBreakpointHitTwice(string function_to_call, string function_to_call2, string method_to_stop, string condition, bool bp_stop_expected, bool bp_stop_expected2)
        {
            Result [] bps = new Result[2];
            bps[0] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 3, condition:condition);
            bps[1] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 4);
            await EvaluateAndCheck(
                "window.setTimeout(function() { " + function_to_call + "; " +  function_to_call2 + ";}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["lineNumber"].Value<int>(),
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["columnNumber"].Value<int>(),
                method_to_stop);

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bps[bp_stop_expected2 ? 0 : 1].Value["locations"][0]["lineNumber"].Value<int>(),
                bps[bp_stop_expected2 ? 0 : 1].Value["locations"][0]["columnNumber"].Value<int>(),
                method_to_stop);
        }

        [Fact]
        public async Task BreakOnDebuggerBreak()
        {
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] UserBreak:BreakOnDebuggerBreakCommand'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test2.cs", 56, 4,
                "BreakOnDebuggerBreakCommand");
        }

        [Fact]
        public async Task BreakpointInAssemblyUsingTypeFromAnotherAssembly_BothDynamicallyLoaded()
        {
            int line = 7;
            await SetBreakpoint(".*/library-dependency-debugger-test1.cs$", line, 0, use_regex: true);
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test2.dll"),
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test2.pdb"));
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test1.dll"),
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test1.pdb"));

            var source_location = "dotnet://library-dependency-debugger-test1.dll/library-dependency-debugger-test1.cs";
            Assert.Contains(source_location, scripts.Values);

            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function () { invoke_static_method('[library-dependency-debugger-test1] TestDependency:IntAdd', 5, 10); }, 1);",
               source_location, line, 8,
               "IntAdd");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 5);
            CheckNumber(locals, "b", 10);
        }

        [Fact]
        public async Task DebugHotReloadMethodChangedUserBreak()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody1", "StaticMethod1");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 14, 8, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 14, 8, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckBool(locals, "c", true);
        }

        [Fact]
        public async Task DebugHotReloadMethodUnchanged()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody2", "StaticMethod1");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 23, 8, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 23, 8, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
        }

        [Fact]
        public async Task DebugHotReloadMethodAddBreakpoint()
        {
            int line = 30;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody3", "StaticMethod3");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, "StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);

            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, "StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckBool(locals, "c", true);
        }

    }
}
