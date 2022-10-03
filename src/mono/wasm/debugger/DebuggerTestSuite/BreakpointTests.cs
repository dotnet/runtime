// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace DebuggerTests
{

    public class BreakpointTests : DebuggerTests
    {
        public BreakpointTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpoint()
        {
            var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

            Assert.EndsWith("debugger-test.cs", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal("dotnet://debugger-test.dll/debugger-test.cs", scripts[loc["scriptId"]?.Value<string>()]);
            Assert.Equal(10, (int)loc["lineNumber"]);
            Assert.Equal(8, (int)loc["columnNumber"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateJSBreakpoint()
        {
            // Test that js breakpoints get set correctly
            // 13 24
            // 14 24
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 13, 24);

            Assert.EndsWith("debugger-driver.html", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal(13, (int)loc["lineNumber"]);
            Assert.Equal(24, (int)loc["columnNumber"]);

            var bp2_res = await SetBreakpoint("/debugger-driver.html", 14, 24);

            Assert.EndsWith("debugger-driver.html", bp2_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp2_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc2 = bp2_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc2["scriptId"]);
            Assert.Equal(14, (int)loc2["lineNumber"]);
            Assert.Equal(24, (int)loc2["columnNumber"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateJS0Breakpoint()
        {
            // 13 24
            // 14 24
            var bp1_res = await SetBreakpoint("/debugger-driver.html", 13, 0);

            Assert.EndsWith("debugger-driver.html", bp1_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc["scriptId"]);
            Assert.Equal(13, (int)loc["lineNumber"]);
            Assert.Equal(24, (int)loc["columnNumber"]);

            var bp2_res = await SetBreakpoint("/debugger-driver.html", 14, 0);

            Assert.EndsWith("debugger-driver.html", bp2_res.Value["breakpointId"].ToString());
            Assert.Equal(1, bp2_res.Value["locations"]?.Value<JArray>()?.Count);

            var loc2 = bp2_res.Value["locations"]?.Value<JArray>()[0];

            Assert.NotNull(loc2["scriptId"]);
            Assert.Equal(14, (int)loc2["lineNumber"]);
            Assert.Equal(24, (int)loc2["columnNumber"]);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
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

        [ConditionalFact(nameof(RunningOnChrome))]
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

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd",
                wait_for_event_fn: (pause_location) =>
                {
                    Assert.Equal("other", pause_location["reason"]?.Value<string>());
                    Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                    var top_frame = pause_location["callFrames"][0];
                    Assert.Equal("Math.IntAdd", top_frame["functionName"].Value<string>());
                    Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

                    //now check the scope
                    var scope = top_frame["scopeChain"][0];
                    Assert.Equal("local", scope["type"]);
                    Assert.Equal("Math.IntAdd", scope["name"]);

                    Assert.Equal("object", scope["object"]["type"]);

                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);

                    foreach (var frame in pause_location["callFrames"])
                    {
                        Assert.Equal(false, frame["url"].Value<string>().Contains(".wasm"));
                        Assert.Equal(false, frame["url"].Value<string>().Contains("wasm://"));
                    }
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
            /*{ "invoke_add()", "IntAdd", "true", true },
            { "invoke_add()", "IntAdd", "5", true },
            { "invoke_add()", "IntAdd", "c < 40", true },
            { "invoke_use_complex()", "UseComplex", "complex.A == 10", true },
            { "invoke_add()", "IntAdd", "1.0", true },
            { "invoke_add()", "IntAdd", "\"foo\"", true },
            { "invoke_add()", "IntAdd", "\"true\"", true },
            { "invoke_add()", "IntAdd", "\"false\"", true },*/
        };

        public static TheoryData<string, string, string, bool> InvalidConditions = new TheoryData<string, string, string, bool>
        {
            { "invoke_add()", "IntAdd", "foo.bar", false },
            { "invoke_add()", "IntAdd", "Math.IntAdd()", false },
            { "invoke_add()", "IntAdd", "c == \"xyz\"", false },
            { "invoke_add()", "IntAdd", "Math.NonExistentProperty", false },
            { "invoke_add()", "IntAdd", "g == 40", false },
            { "invoke_add()", "IntAdd", "null", false },
        };

        [Theory]
        [MemberData(nameof(FalseConditions))]
        [MemberData(nameof(TrueConditions))]
        [MemberData(nameof(InvalidConditions))]
        public async Task ConditionalBreakpoint2(string function_to_call, string method_to_stop, string condition, bool bp_stop_expected)
        {
            Result [] bps = new Result[2];
            bps[0] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 3, condition:condition);
            bps[1] = await SetBreakpointInMethod("debugger-test.dll", "Math", method_to_stop, 4);
            await EvaluateAndCheck(
                "window.setTimeout(function() { " + function_to_call + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["lineNumber"].Value<int>(),
                bps[bp_stop_expected ? 0 : 1].Value["locations"][0]["columnNumber"].Value<int>(),
                "Math." + method_to_stop);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("c == 15", 79, 3, 79, 11)]
        [InlineData("c == 17", 79, 3, 80, 11)]
        [InlineData("g == 17", 79, 3, 80, 11)]
        [InlineData("true", 79, 3, 79, 11)]
        [InlineData("\"false\"", 79, 3, 79, 11)]
        [InlineData("\"true\"", 79, 3, 79, 11)]
        [InlineData("5", 79, 3, 79, 11)]
        [InlineData("p", 79, 3, 80, 11)]
        [InlineData("0.0", 79, 3, 80, 11)]
        public async Task JSConditionalBreakpoint(string condition, int line_bp, int column_bp, int line_expected, int column_expected)
        {
            await SetBreakpoint("/debugger-driver.html", line_bp, column_bp, condition: condition);
            await SetBreakpoint("/debugger-driver.html", 80, 11);

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
                "Math." + method_to_stop);

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bps[bp_stop_expected2 ? 0 : 1].Value["locations"][0]["lineNumber"].Value<int>(),
                bps[bp_stop_expected2 ? 0 : 1].Value["locations"][0]["columnNumber"].Value<int>(),
                "Math." + method_to_stop);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BreakOnDebuggerBreak()
        {
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] UserBreak:BreakOnDebuggerBreakCommand'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test2.cs", 58, 8,
                "UserBreak.BreakOnDebuggerBreakCommand",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test2.cs", 59, 8, "UserBreak.BreakOnDebuggerBreakCommand",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test2.cs", 60, 8, "UserBreak.BreakOnDebuggerBreakCommand",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test2.cs", 61, 8, "UserBreak.BreakOnDebuggerBreakCommand",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 50);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test2.cs", 62, 4, "UserBreak.BreakOnDebuggerBreakCommand",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 100);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BreakpointInAssemblyUsingTypeFromAnotherAssembly_BothDynamicallyLoaded()
        {
            int line = 7;

            // Start the task earlier than loading the assemblies, so we don't miss the event
            Task<JObject> bpResolved = WaitForBreakpointResolvedEvent();
            await SetBreakpoint(".*/library-dependency-debugger-test1.cs$", line, 0, use_regex: true);
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test2.dll"),
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test2.pdb"));
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test1.dll"),
                    Path.Combine(DebuggerTestAppPath, "library-dependency-debugger-test1.pdb"));

            var source_location = "dotnet://library-dependency-debugger-test1.dll/library-dependency-debugger-test1.cs";
            Assert.Contains(source_location, scripts.Values);

            await bpResolved;

            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function () { invoke_static_method('[library-dependency-debugger-test1] TestDependency:IntAdd', 5, 10); }, 1);",
               source_location, line, 8,
               "TestDependency.IntAdd");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 5);
            CheckNumber(locals, "b", 10);
        }

        [Fact]
        public async Task ConditionalBreakpointInALoop()
        {
            var bp_conditional = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 4, condition:"i == 3");
            var bp_check = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 5);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] LoopClass:LoopToBreak'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 3);
                    await Task.CompletedTask;
                }
            );

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_check.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_check.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak");
        }

        [Fact]
        public async Task ConditionalBreakpointInALoopStopMoreThanOnce()
        {
            var bp_conditional = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 4, condition:"i % 3 == 0");
            var bp_check = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 5);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] LoopClass:LoopToBreak'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 0);
                    await Task.CompletedTask;
                }
            );

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 3);
                    await Task.CompletedTask;
                });

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 6);
                    await Task.CompletedTask;
                });

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 9);
                    await Task.CompletedTask;
                });

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_check.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_check.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak");
        }

        [Fact]
        public async Task ConditionalBreakpointNoStopInALoop()
        {
            var bp_conditional = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 4, condition:"i == \"10\"");
            var bp_check = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 5);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] LoopClass:LoopToBreak'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_check.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_check.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak"
            );
        }

        [Fact]
        public async Task ConditionalBreakpointNotBooleanInALoop()
        {
            var bp_conditional = await SetBreakpointInMethod("debugger-test.dll", "LoopClass", "LoopToBreak", 4, condition:"i + 4");
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] LoopClass:LoopToBreak'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 0);
                    await Task.CompletedTask;
                }
            );

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 1);
                    await Task.CompletedTask;
                });

            await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "LoopClass.LoopToBreak",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 2);
                    await Task.CompletedTask;
                });
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpointAndHitGoToNonWasmPageComeBackAndHitAgain()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd");
            Assert.Equal("other", pause_location["reason"]?.Value<string>());
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            var top_frame = pause_location["callFrames"][0];
            Assert.Equal("Math.IntAdd", top_frame["functionName"].Value<string>());
            Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

            //now check the scope
            var scope = top_frame["scopeChain"][0];
            Assert.Equal("local", scope["type"]);
            Assert.Equal("Math.IntAdd", scope["name"]);

            Assert.Equal("object", scope["object"]["type"]);
            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);

            await cli.SendCommand("Debugger.resume", null, token);

            var run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { load_non_wasm_page(); }, 1);"
            });
            await cli.SendCommand("Runtime.evaluate", run_method, token);
            await Task.Delay(1000, token);

            run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { reload_wasm_page(); }, 1);"
            });
            await cli.SendCommand("Runtime.evaluate", run_method, token);
            await insp.WaitFor(Inspector.READY);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd",
                wait_for_event_fn: (pause_location) =>
                {
                    Assert.Equal("other", pause_location["reason"]?.Value<string>());
                    Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                    var top_frame = pause_location["callFrames"][0];
                    Assert.Equal("Math.IntAdd", top_frame["functionName"].Value<string>());
                    Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

                    //now check the scope
                    var scope = top_frame["scopeChain"][0];
                    Assert.Equal("local", scope["type"]);
                    Assert.Equal("Math.IntAdd", scope["name"]);

                    Assert.Equal("object", scope["object"]["type"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);
                    return Task.CompletedTask;
                }
            );
        }


        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("RunDebuggerHidden", "HiddenMethod")]
        [InlineData("RunStepThroughWithHidden", "StepThroughWithHiddenBp")] // debuggerHidden shadows the effect of stepThrough
        [InlineData("RunNonUserCodeWithHidden", "NonUserCodeWithHiddenBp")] // and nonUserCode
        public async Task DebuggerHiddenNoStopOnBp(string evalFunName, string decoratedFunName)
        {
            var bp_hidden = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", decoratedFunName, 1);
            var bp_final = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 2);
            Assert.Empty(bp_hidden.Value["locations"]);
            await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DebuggerAttribute:{evalFunName}'); }}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_final.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_final.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("RunDebuggerHidden")]
        [InlineData("RunStepThroughWithHidden")] // debuggerHidden shadows the effect of stepThrough
        [InlineData("RunNonUserCodeWithHidden")] // and nonUserCode
        public async Task DebuggerHiddenStopOnUserBp(string evalFunName)
        {
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 2);
            var bp_final = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 3);
            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DebuggerAttribute:{evalFunName}'); }}, 2);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName
            );
            await SendCommandAndCheck(null, "Debugger.resume",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName);
            await SendCommandAndCheck(null, "Debugger.resume",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_final.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_final.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false, "RunStepThrough", "DebuggerAttribute", 1, 847, 8, "DebuggerAttribute.RunStepThrough")]
        [InlineData(true, "RunStepThrough", "DebuggerAttribute", 1, 847, 8, "DebuggerAttribute.RunStepThrough")]
        [InlineData(false, "RunNonUserCode", "DebuggerAttribute", 1, 852, 4, "DebuggerAttribute.NonUserCodeBp")]
        [InlineData(true, "RunNonUserCode", "DebuggerAttribute", 1, 867, 8, "DebuggerAttribute.RunNonUserCode")]
        [InlineData(false, "RunStepThroughWithNonUserCode", "DebuggerAttribute", 1, 933, 8, "DebuggerAttribute.RunStepThroughWithNonUserCode")]
        [InlineData(true, "RunStepThroughWithNonUserCode", "DebuggerAttribute", 1, 933, 8, "DebuggerAttribute.RunStepThroughWithNonUserCode")]
        [InlineData(false, "EvaluateStepThroughAttr", "DefaultInterfaceMethod", 2, 1108, 4, "DefaultInterfaceMethod.EvaluateStepThroughAttr")]
        [InlineData(true, "EvaluateStepThroughAttr", "DefaultInterfaceMethod", 2, 1108, 4, "DefaultInterfaceMethod.EvaluateStepThroughAttr")]
        [InlineData(false, "EvaluateNonUserCodeAttr", "DefaultInterfaceMethod", 2, 1065, 4, "IExtendIDefaultInterface.NonUserCodeDefaultMethod")]
        [InlineData(true, "EvaluateNonUserCodeAttr", "DefaultInterfaceMethod", 2, 1114, 4, "DefaultInterfaceMethod.EvaluateNonUserCodeAttr")]
        public async Task StepThroughOrNonUserCodeAttributeStepInNoBp2(bool justMyCodeEnabled, string evalFunName, string evalClassName, int bpLine, int line, int col, string funcName="")
        {
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", evalClassName, evalFunName, bpLine);
            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] {evalClassName}:{evalFunName}'); }}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                evalClassName + "." + evalFunName
            );
            await SetJustMyCode(justMyCodeEnabled);
            await SendCommandAndCheck(null, "Debugger.stepInto", "dotnet://debugger-test.dll/debugger-test.cs", line, col, funcName);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false, "RunStepThrough", "StepThroughBp", "", 846, 8)]
        [InlineData(true, "RunStepThrough", "StepThroughBp", "RunStepThrough", 847, 8)]
        [InlineData(false, "RunNonUserCode", "NonUserCodeBp", "NonUserCodeBp", 852, 4)]
        [InlineData(true, "RunNonUserCode", "NonUserCodeBp", "RunNonUserCode", 867, 8)]
        [InlineData(false, "RunStepThroughWithNonUserCode", "StepThroughWithNonUserCodeBp", "", 932, 8)]
        [InlineData(true, "RunStepThroughWithNonUserCode", "StepThroughWithNonUserCodeBp", "RunStepThroughWithNonUserCode", 933, 8)]
        public async Task StepThroughOrNonUserCodeAttributeStepInWithBp(
            bool justMyCodeEnabled, string evalFunName, string decoratedFunName,
            string funName, int line, int col)
        {
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 1);
            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DebuggerAttribute:{evalFunName}'); }}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName
            );

            await SetJustMyCode(justMyCodeEnabled);
            if (!justMyCodeEnabled && funName == "")
            {
                var bp1_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", decoratedFunName, 1);
                var bp2_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", decoratedFunName, 3);
                var line1 = bp1_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();
                var line2 = bp2_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();
                await SendCommandAndCheck(null, "Debugger.stepInto", "dotnet://debugger-test.dll/debugger-test.cs", line1, 8, "DebuggerAttribute." + decoratedFunName);
                await SendCommandAndCheck(null, "Debugger.stepInto", "dotnet://debugger-test.dll/debugger-test.cs", line2, 8, "DebuggerAttribute." + decoratedFunName);
                funName = evalFunName;
            }
            await SendCommandAndCheck(null, "Debugger.stepInto", "dotnet://debugger-test.dll/debugger-test.cs", line, col, "DebuggerAttribute." + funName);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false, "RunStepThrough", "StepThroughBp")]
        [InlineData(true, "RunStepThrough", "StepThroughBp")]
        [InlineData(true, "RunNonUserCode", "NonUserCodeBp")]
        [InlineData(false, "RunNonUserCode", "NonUserCodeBp")]
        [InlineData(false, "RunStepThroughWithNonUserCode", "StepThroughWithNonUserCodeBp")]
        [InlineData(true, "RunStepThroughWithNonUserCode", "StepThroughWithNonUserCodeBp")]
        public async Task StepThroughOrNonUserCodeAttributeResumeWithBp(bool justMyCodeEnabled, string evalFunName, string decoratedFunName)
        {
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 1);
            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DebuggerAttribute:{evalFunName}'); }}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName
            );

            await SetJustMyCode(justMyCodeEnabled);
            if (!justMyCodeEnabled)
            {
                var bp1_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", decoratedFunName, 1);
                var line1 = bp1_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();
                await SendCommandAndCheck(null, "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", line1, 8, "DebuggerAttribute." + decoratedFunName);
            }
            var bp_outside_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 2);
            var line2 = bp_outside_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();
            await SendCommandAndCheck(null, "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", line2, 8, "DebuggerAttribute." + evalFunName);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false, "Debugger.stepInto", "RunStepThrough", "StepThroughUserBp", 841, 8, "RunStepThrough", 848, 4)]
        [InlineData(true, "Debugger.stepInto", "RunStepThrough", "RunStepThrough", -1, 8, "RunStepThrough", -1, 4)]
        [InlineData(false, "Debugger.resume", "RunStepThrough", "StepThroughUserBp", 841, 8, "RunStepThrough", 848, 4)]
        [InlineData(true, "Debugger.resume", "RunStepThrough", "RunStepThrough", -1, 8, "RunStepThrough", -1, 4)]
        [InlineData(false, "Debugger.stepInto", "RunNonUserCode",  "NonUserCodeUserBp", 860, 4, "NonUserCodeUserBp", 861, 8)]
        [InlineData(true, "Debugger.stepInto", "RunNonUserCode", "RunNonUserCode", -1, 8, "RunNonUserCode", -1, 4)]
        [InlineData(false, "Debugger.resume", "RunNonUserCode", "NonUserCodeUserBp", 861, 8, "RunNonUserCode", -1, 4)]
        [InlineData(true, "Debugger.resume", "RunNonUserCode", "RunNonUserCode", -1, 8, "RunNonUserCode", -1, 4)]
        [InlineData(false, "Debugger.stepInto", "RunStepThroughWithNonUserCode",  "StepThroughWithNonUserCodeUserBp", 927, 8, "RunStepThroughWithNonUserCode", 934, 4)]
        [InlineData(true, "Debugger.stepInto", "RunStepThroughWithNonUserCode", "RunStepThroughWithNonUserCode", -1, 8, "RunStepThroughWithNonUserCode", -1, 4)]
        [InlineData(false, "Debugger.resume", "RunStepThroughWithNonUserCode", "StepThroughWithNonUserCodeUserBp", 927, 8, "RunStepThroughWithNonUserCode", -1, 4)]
        [InlineData(true, "Debugger.resume", "RunStepThroughWithNonUserCode", "RunStepThroughWithNonUserCode", -1, 8, "RunStepThroughWithNonUserCode", -1, 4)]
        public async Task StepThroughOrNonUserCodeAttributeWithUserBp(
            bool justMyCodeEnabled, string debuggingFunction, string evalFunName,
            string functionNameCheck1, int line1, int col1,
            string functionNameCheck2, int line2, int col2)
        {
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 2);
            var bp_outside_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", evalFunName, 3);

            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DebuggerAttribute:{evalFunName}'); }}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute." + evalFunName
            );

            await SetJustMyCode(justMyCodeEnabled);
            if (line1 == -1)
                line1 = bp_outside_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>() - 1;
            if (line2 == -1)
                line2 = bp_outside_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();

            await SendCommandAndCheck(null, debuggingFunction, "dotnet://debugger-test.dll/debugger-test.cs", line1, col1, "DebuggerAttribute." + functionNameCheck1);
            await SendCommandAndCheck(null, debuggingFunction, "dotnet://debugger-test.dll/debugger-test.cs", line2, col2, "DebuggerAttribute." + functionNameCheck2);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("DebuggerAttribute", "RunNoBoundary", "DebuggerAttribute", "Debugger.stepInto", 1, 2, false)]
        [InlineData("DebuggerAttribute", "RunNoBoundary", "DebuggerAttribute", "Debugger.stepInto", 1, 2, true)]
        [InlineData("DebuggerAttribute", "RunNoBoundary", "DebuggerAttribute", "Debugger.resume", 1, 2, true)]
        [InlineData("DebuggerAttribute", "RunNoBoundary", "DebuggerAttribute", "Debugger.stepInto", 2, 3, false, true)]
        [InlineData("DebuggerAttribute", "RunNoBoundary", "DebuggerAttribute", "Debugger.resume", 2, 3, false, true)]
        [InlineData("DefaultInterfaceMethod", "EvaluateStepperBoundaryAttr", "IExtendIDefaultInterface", "Debugger.stepInto", 2, 3, false)]
        [InlineData("DefaultInterfaceMethod", "EvaluateStepperBoundaryAttr", "IExtendIDefaultInterface", "Debugger.stepInto", 2, 3, true)]
        [InlineData("DefaultInterfaceMethod", "EvaluateStepperBoundaryAttr", "IExtendIDefaultInterface", "Debugger.resume", 2, 3, true)]
        public async Task StepperBoundary(
            string className, string evalFunName, string decoratedMethodClassName, string debuggingAction, int lineBpInit, int lineBpFinal, bool hasBpInDecoratedFun, bool isTestingUserBp = false)
        {
            // behavior of StepperBoundary is the same for JMC enabled and disabled
            // but the effect of NonUserCode escape is better visible for JMC: enabled
            await SetJustMyCode(true);
            var bp_init = await SetBreakpointInMethod("debugger-test.dll", className, evalFunName, lineBpInit);
            var init_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] {className}:{evalFunName}'); }}, {lineBpInit});",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_init.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_init.Value["locations"][0]["columnNumber"].Value<int>(),
                className + "." + evalFunName
            );
            var bp_final = await SetBreakpointInMethod("debugger-test.dll", className, evalFunName, lineBpFinal);
            if (hasBpInDecoratedFun)
            {
                var bp_decorated_fun = await SetBreakpointInMethod("debugger-test.dll", decoratedMethodClassName, "BoundaryBp", 2);
                var line_decorated_fun = bp_decorated_fun.Value["locations"][0]["lineNumber"].Value<int>();
                var col_decorated_fun = bp_decorated_fun.Value["locations"][0]["columnNumber"].Value<int>();
                await SendCommandAndCheck(null, debuggingAction, "dotnet://debugger-test.dll/debugger-test.cs", line_decorated_fun, col_decorated_fun, decoratedMethodClassName + ".BoundaryBp");
            }
            if (isTestingUserBp)
                await SendCommandAndCheck(null, debuggingAction, "dotnet://debugger-test.dll/debugger-test.cs", 879, 8, decoratedMethodClassName + ".BoundaryUserBp");

            var line = bp_final.Value["locations"][0]["lineNumber"].Value<int>();
            var col = bp_final.Value["locations"][0]["columnNumber"].Value<int>();
            await SendCommandAndCheck(null, debuggingAction, "dotnet://debugger-test.dll/debugger-test.cs", line, col,  className + "." + evalFunName);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpointAndHitGoToWasmPageWithoutAssetsComeBackAndHitAgain()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd");
            Assert.Equal("other", pause_location["reason"]?.Value<string>());
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            var top_frame = pause_location["callFrames"][0];
            Assert.Equal("Math.IntAdd", top_frame["functionName"].Value<string>());
            Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

            //now check the scope
            var scope = top_frame["scopeChain"][0];
            Assert.Equal("local", scope["type"]);
            Assert.Equal("Math.IntAdd", scope["name"]);

            Assert.Equal("object", scope["object"]["type"]);
            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);

            await cli.SendCommand("Debugger.resume", null, token);

            var run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { load_wasm_page_without_assets(); }, 1);"
            });
            await cli.SendCommand("Runtime.evaluate", run_method, token);
            await insp.WaitFor(Inspector.READY);

            run_method = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { reload_wasm_page(); }, 1);"
            });
            await cli.SendCommand("Runtime.evaluate", run_method, token);
            await insp.WaitFor(Inspector.READY);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd",
                wait_for_event_fn: async (pause_location) =>
                {
                    Assert.Equal("other", pause_location["reason"]?.Value<string>());
                    Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                    var top_frame = pause_location["callFrames"][0];
                    Assert.Equal("Math.IntAdd", top_frame["functionName"].Value<string>());
                    Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);

                    //now check the scope
                    var scope = top_frame["scopeChain"][0];
                    Assert.Equal("local", scope["type"]);
                    Assert.Equal("Math.IntAdd", scope["name"]);

                    Assert.Equal("object", scope["object"]["type"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, scope["startLocation"]);
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 14, 4, scripts, scope["endLocation"]);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("IDefaultInterface", "DefaultMethod", "Evaluate", "DefaultInterfaceMethod.Evaluate", 1087, 1003, 1001, 1005)]
        [InlineData("IExtendIDefaultInterface", "IDefaultInterface.DefaultMethodToOverride", "Evaluate", "DefaultInterfaceMethod.Evaluate", 1088, 1047, 1045, 1049)]
        [InlineData("IDefaultInterface", "DefaultMethodAsync", "EvaluateAsync", "System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start<IDefaultInterface.<DefaultMethodAsync>d__3>", 37, 1016, 1014, 1018)]
        [InlineData("IDefaultInterface", "DefaultMethodStatic", "EvaluateStatic", "DefaultInterfaceMethod.EvaluateStatic", 1124, 1022, 1020, 1024)]
        [InlineData("IDefaultInterface", "DefaultMethodAsyncStatic", "EvaluateAsyncStatic", "System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start<IDefaultInterface.<DefaultMethodAsyncStatic>d__5>", 37, 1031, 1029, 1033)]
        public async Task BreakInDefaultInterfaceMethod(
            string dimClassName, string dimName, string entryMethod,  string prevFrameInDim, int evaluateAsPrevFrameLine, int dimAsPrevFrameLine, int functionLocationLine, int functionEndLine)
        {
            string assembly = "dotnet://debugger-test.dll/debugger-test.cs";
            var bpDim = await SetBreakpointInMethod("debugger-test.dll", dimClassName, dimName, 1);
            var pauseInDim = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DefaultInterfaceMethod:{entryMethod}'); }}, 1);",
                assembly,
                bpDim.Value["locations"][0]["lineNumber"].Value<int>(),
                bpDim.Value["locations"][0]["columnNumber"].Value<int>(),
                dimClassName + "." + dimName,
                wait_for_event_fn: async (pause_location) => { await CheckDefaultMethod(pause_location,  dimClassName + "." + dimName); }
            );

            // check prev frame in DIM
            Assert.Equal(pauseInDim["callFrames"][1]["functionName"].Value<string>(), prevFrameInDim);
            Assert.Equal(pauseInDim["callFrames"][1]["location"]["lineNumber"].Value<int>(), evaluateAsPrevFrameLine);

            string funCalledFromDim = "MethodForCallingFromDIM";
            var bpFunCalledFromDim = await SetBreakpointInMethod("debugger-test.dll", "DefaultInterfaceMethod", funCalledFromDim, 1);

            // check prev frame in method called from DIM
            var pauseInFunCalledFromDim = await SendCommandAndCheck(null, "Debugger.resume",
                null,
                bpFunCalledFromDim.Value["locations"][0]["lineNumber"].Value<int>(),
                bpFunCalledFromDim.Value["locations"][0]["columnNumber"].Value<int>(),
                "DefaultInterfaceMethod." + funCalledFromDim);

            string prevFrameFromDim = dimName;
            Assert.Equal(pauseInFunCalledFromDim["callFrames"][1]["functionName"].Value<string>(), dimClassName + "." + prevFrameFromDim);
            Assert.Equal(pauseInFunCalledFromDim["callFrames"][1]["location"]["lineNumber"].Value<int>(), dimAsPrevFrameLine);


            async Task CheckDefaultMethod(JObject pause_location, string methodName)
            {
                Assert.Equal("other", pause_location["reason"]?.Value<string>());
                Assert.Equal(bpDim.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                var top_frame = pause_location["callFrames"][0];
                Assert.Equal(methodName, top_frame["functionName"].Value<string>());
                Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

                CheckLocation(assembly, functionLocationLine, 4, scripts, top_frame["functionLocation"]);

                var scope = top_frame["scopeChain"][0];
                Assert.Equal("local", scope["type"]);
                Assert.Equal(methodName, scope["name"]);

                Assert.Equal("object", scope["object"]["type"]);
                CheckLocation(assembly, functionLocationLine, 4, scripts, scope["startLocation"]);
                CheckLocation(assembly, functionEndLine, 4, scripts, scope["endLocation"]);
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task CheckDefaultInterfaceMethodHiddenAttribute()
        {
            string methodName = "EvaluateHiddenAttr";
            string assembly = "dotnet://debugger-test.dll/debugger-test.cs";
            var hidden_bp = await SetBreakpointInMethod("debugger-test.dll", "IExtendIDefaultInterface", "HiddenDefaultMethod", 1);
            var final_bp = await SetBreakpointInMethod("debugger-test.dll", "DefaultInterfaceMethod", methodName, 3);
            // check if bp in hidden DIM was ignored:
            var final_location = await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] DefaultInterfaceMethod:{methodName}'); }}, 1);",
                assembly,
                final_bp.Value["locations"][0]["lineNumber"].Value<int>(),
                final_bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "DefaultInterfaceMethod." + methodName
            );
        }
    }
}
