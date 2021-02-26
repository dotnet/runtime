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

        [Fact]
        public async Task ConditionalBreakpointTrue()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"c == 30");

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointFalse()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"c == 40");
            var bp2_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 13, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 13, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointInvalidCondition() //testing with js debugger an invalid expression the debugger does not stop
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"g == 40");
            var bp2_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 13, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 13, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointConst1()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"true");

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointConst2()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"5");

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointLower()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"c < 40");

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
        }

        [Fact]
        public async Task ConditionalBreakpointLowerFalse()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8, condition:"c < 0");
            var bp2_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 13, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 13, 8, "IntAdd");
        }

        [Fact]
        public async Task JSConditionalBreakpointTrue()
        {
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 78, 3, condition:"c == 15");

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10); }, 1);",
                "debugger-driver.html", 78, 11, "conditional_breakpoint_test");
        }

        [Fact]
        public async Task JSConditionalBreakpointFalse()
        {
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 78, 3, condition:"c == 17");
            var bp2_res = await SetBreakpoint("/debugger-driver.html", 79, 3);

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10); }, 1);",
                "debugger-driver.html", 79, 3, "conditional_breakpoint_test");
        }

        [Fact]
        public async Task JSConditionalBreakpointInvalidCondition()
        {
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 78, 3, condition:"g == 17");
            var bp2_res = await SetBreakpoint("/debugger-driver.html", 79, 3);

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10); }, 1);",
                "debugger-driver.html", 79, 3, "conditional_breakpoint_test");
        }

        [Fact]
        public async Task JSConditionalBreakpointConst1()
        {
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 78, 3, condition:"true");

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10); }, 1);",
                "debugger-driver.html", 78, 11, "conditional_breakpoint_test");
        }

        [Fact]
        public async Task JSConditionalBreakpointConst2()
        {
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 78, 3, condition:"5");

            await EvaluateAndCheck(
                "window.setTimeout(function() { conditional_breakpoint_test(5, 10); }, 1);",
                "debugger-driver.html", 78, 11, "conditional_breakpoint_test");
        }

        [Fact]
        public async Task ConditionalBreakpointAttribute()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 20, 8, condition:"complex.A == 10");

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_use_complex(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 20, 8, "UseComplex");
        }
    }
}
