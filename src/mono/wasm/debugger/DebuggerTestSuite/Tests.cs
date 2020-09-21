// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace DebuggerTests
{

    public class SourceList : DebuggerTestBase
    {

        [Fact]
        public async Task CheckThatAllSourcesAreSent()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            //all sources are sent before runtime ready is sent, nothing to check
            await insp.Ready();
            Assert.Contains("dotnet://debugger-test.dll/debugger-test.cs", scripts.Values);
            Assert.Contains("dotnet://debugger-test.dll/debugger-test2.cs", scripts.Values);
            Assert.Contains("dotnet://debugger-test.dll/dependency.cs", scripts.Values);
        }

        [Fact]
        public async Task CreateGoodBreakpoint()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp1_res = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

                Assert.EndsWith("debugger-test.cs", bp1_res.Value["breakpointId"].ToString());
                Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

                var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

                Assert.NotNull(loc["scriptId"]);
                Assert.Equal("dotnet://debugger-test.dll/debugger-test.cs", scripts[loc["scriptId"]?.Value<string>()]);
                Assert.Equal(10, loc["lineNumber"]);
                Assert.Equal(8, loc["columnNumber"]);
            });
        }

        [Fact]
        public async Task CreateJSBreakpoint()
        {
            // Test that js breakpoints get set correctly
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
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
            });
        }

        [Fact]
        public async Task CreateJS0Breakpoint()
        {
            // Test that js column 0 does as expected
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
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
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        public async Task CheckMultipleBreakpointsOnSameLine(int col)
        {
            var insp = new Inspector();

            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

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
            });
        }

        [Fact]
        public async Task CreateBadBreakpoint()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
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
            });
        }

        [Fact]
        public async Task CreateGoodBreakpointAndHit()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

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

            });
        }

        [Fact]
        public async Task ExceptionThrownInJS()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                var eval_req = JObject.FromObject(new
                {
                    expression = "invoke_bad_js_test();"
                });

                var eval_res = await cli.SendCommand("Runtime.evaluate", eval_req, token);
                Assert.True(eval_res.IsErr);
                Assert.Equal("Uncaught", eval_res.Error["exceptionDetails"]?["text"]?.Value<string>());
            });
        }

        [Fact]
        public async Task ExceptionThrownInJSOutOfBand()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                await SetBreakpoint("/debugger-driver.html", 27, 2);

                var eval_req = JObject.FromObject(new
                {
                    expression = "window.setTimeout(function() { invoke_bad_js_test(); }, 1);",
                });

                var eval_res = await cli.SendCommand("Runtime.evaluate", eval_req, token);
                // Response here will be the id for the timer from JS!
                Assert.True(eval_res.IsOk);

                var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await insp.WaitFor("Runtime.exceptionThrown"));
                var ex_json = JObject.Parse(ex.Message);
                Assert.Equal(dicFileToUrl["/debugger-driver.html"], ex_json["exceptionDetails"]?["url"]?.Value<string>());
            });

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsAtBreakpointSite(bool use_cfo) =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "IntAdd",
                "window.setTimeout(function() { invoke_add(); }, 1);",
                use_cfo: use_cfo,
                test_fn: (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 30);
                    CheckNumber(locals, "d", 0);
                    CheckNumber(locals, "e", 0);
                }
            );

        [Fact]
        public async Task InspectPrimitiveTypeLocalsAtBreakpointSite() =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 154, 8, "PrimitiveTypesTest",
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:PrimitiveTypesTest'); }, 1);",
                test_fn: (locals) =>
                {
                    CheckSymbol(locals, "c0", "8364 '€'");
                    CheckSymbol(locals, "c1", "65 'A'");
                }
            );

        [Fact]
        public async Task InspectLocalsTypesAtBreakpointSite() =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test2.cs", 48, 8, "Types",
                "window.setTimeout(function() { invoke_static_method (\"[debugger-test] Fancy:Types\")(); }, 1);",
                use_cfo: false,
                test_fn: (locals) =>
                {
                    CheckNumber(locals, "dPI", Math.PI);
                    CheckNumber(locals, "fPI", (float)Math.PI);
                    CheckNumber(locals, "iMax", int.MaxValue);
                    CheckNumber(locals, "iMin", int.MinValue);
                    CheckNumber(locals, "uiMax", uint.MaxValue);
                    CheckNumber(locals, "uiMin", uint.MinValue);

                    CheckNumber(locals, "l", uint.MaxValue * (long)2);
                    //CheckNumber (locals, "lMax", long.MaxValue); // cannot be represented as double
                    //CheckNumber (locals, "lMin", long.MinValue); // cannot be represented as double

                    CheckNumber(locals, "sbMax", sbyte.MaxValue);
                    CheckNumber(locals, "sbMin", sbyte.MinValue);
                    CheckNumber(locals, "bMax", byte.MaxValue);
                    CheckNumber(locals, "bMin", byte.MinValue);

                    CheckNumber(locals, "sMax", short.MaxValue);
                    CheckNumber(locals, "sMin", short.MinValue);
                    CheckNumber(locals, "usMin", ushort.MinValue);
                    CheckNumber(locals, "usMax", ushort.MaxValue);
                }
            );

        [Fact]
        public async Task InspectSimpleStringLocals() =>
            await CheckInspectLocalsAtBreakpointSite(
                "Math", "TestSimpleStrings", 13, "TestSimpleStrings",
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:TestSimpleStrings')(); }, 1);",
                wait_for_event_fn: async (pause_location) =>
                {
                    var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

                    var str_null = TString(null);
                    var str_empty = TString(String.Empty);
                    var str_spaces = TString(" ");
                    var str_esc = TString("\\");

                    await CheckProps(locals, new
                    {
                        str_null,
                        str_empty,
                        str_spaces,
                        str_esc,

                        strings = TArray("string[]", 4)
                    }, "locals");

                    var strings_arr = await GetObjectOnLocals(locals, "strings");
                    await CheckProps(strings_arr, new[]
                    {
                        str_null,
                        str_empty,
                        str_spaces,
                        str_esc
                    }, "locals#strings");
                }
            );

        [Theory]
        [InlineData("TestNullableLocal", false)]
        [InlineData("TestNullableLocalAsync", true)]
        public async Task InspectNullableLocals(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.NullableTests",
            method_name,
            10,
            is_async ? "MoveNext" : method_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.NullableTests:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                await CheckProps(locals, new
                {
                    n_int       = TNumber(5),
                    n_int_null  = TObject("System.Nullable<int>", null),

                    n_dt        = TDateTime(dt),
                    n_dt_null   = TObject("System.Nullable<System.DateTime>", null),

                    n_gs        = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    n_gs_null   = TObject("System.Nullable<DebuggerTests.ValueTypesTest.GenericStruct<int>>", null),
                }, "locals");

                // check gs

                var n_gs = GetAndAssertObjectWithName(locals, "n_gs");
                var n_gs_props = await GetProperties(n_gs["value"]?["objectId"]?.Value<string> ());
                await CheckProps(n_gs_props, new
                {
                    List        = TObject("System.Collections.Generic.List<int>", is_null: true),
                    StringField = TString("n_gs#StringField"),
                    Options     = TEnum  ("DebuggerTests.Options", "None")
                }, nameof(n_gs));
            });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithGenericTypesAtBreakpointSite(bool use_cfo) =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 74, 8, "GenericTypesTest",
                "window.setTimeout(function() { invoke_generic_types_test (); }, 1);",
                use_cfo: use_cfo,
                test_fn: (locals) =>
                {
                    CheckObject(locals, "list", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>");
                    CheckObject(locals, "list_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

                    CheckArray(locals, "list_arr", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", 1);
                    CheckObject(locals, "list_arr_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);

                    // Unused locals
                    CheckObject(locals, "list_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>");
                    CheckObject(locals, "list_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

                    CheckArray(locals, "list_arr_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", 1);
                    CheckObject(locals, "list_arr_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);
                }
            );

        [Fact]
        public async Task RuntimeGetPropertiesWithInvalidScopeIdTest()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 49, 8);

                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_delegates_test (); }, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs", 49, 8,
                    "DelegatesTest",
                    wait_for_event_fn: async (pause_location) =>
                   {
                       //make sure we're on the right bp
                       Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                       var top_frame = pause_location["callFrames"][0];

                       var scope = top_frame["scopeChain"][0];

                       // Try to get an invalid scope!
                       var get_prop_req = JObject.FromObject(new
                       {
                           objectId = "dotnet:scope:23490871",
                       });

                       var frame_props = await cli.SendCommand("Runtime.getProperties", get_prop_req, token);
                       Assert.True(frame_props.IsErr);
                   }
                );
            });
        }

        [Fact]
        public async Task TrivalStepping()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_add(); }, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                    "IntAdd",
                    wait_for_event_fn: (pause_location) =>
                    {
                        //make sure we're on the right bp
                        Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                        var top_frame = pause_location["callFrames"][0];
                        CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);
                        return Task.CompletedTask;
                    }
                );

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 11, 8, "IntAdd",
                    wait_for_event_fn: (pause_location) =>
                    {
                        var top_frame = pause_location["callFrames"][0];
                        CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);
                        return Task.CompletedTask;
                    }
                );
            });
        }

        [Fact]
        public async Task InspectLocalsDuringStepping()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
                await SetBreakpoint(debugger_test_loc, 10, 8);

                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_add(); }, 1);",
                    debugger_test_loc, 10, 8, "IntAdd",
                    locals_fn: (locals) =>
                    {
                        CheckNumber(locals, "a", 10);
                        CheckNumber(locals, "b", 20);
                        CheckNumber(locals, "c", 30);
                        CheckNumber(locals, "d", 0);
                        CheckNumber(locals, "e", 0);
                    }
                );

                await StepAndCheck(StepKind.Over, debugger_test_loc, 11, 8, "IntAdd",
                    locals_fn: (locals) =>
                    {
                        CheckNumber(locals, "a", 10);
                        CheckNumber(locals, "b", 20);
                        CheckNumber(locals, "c", 30);
                        CheckNumber(locals, "d", 50);
                        CheckNumber(locals, "e", 0);
                    }
                );

                //step and get locals
                await StepAndCheck(StepKind.Over, debugger_test_loc, 12, 8, "IntAdd",
                    locals_fn: (locals) =>
                    {
                        CheckNumber(locals, "a", 10);
                        CheckNumber(locals, "b", 20);
                        CheckNumber(locals, "c", 30);
                        CheckNumber(locals, "d", 50);
                        CheckNumber(locals, "e", 60);
                    }
                );
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInPreviousFramesDuringSteppingIn2(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

                var dep_cs_loc = "dotnet://debugger-test.dll/dependency.cs";
                await SetBreakpoint(dep_cs_loc, 33, 8);

                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

                // Will stop in Complex.DoEvenMoreStuff
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_use_complex (); }, 1);",
                    dep_cs_loc, 33, 8, "DoEvenMoreStuff",
                    locals_fn: (locals) =>
                    {
                        Assert.Single(locals);
                        CheckObject(locals, "this", "Simple.Complex");
                    }
                );

                var props = await GetObjectOnFrame(pause_location["callFrames"][0], "this");
                Assert.Equal(3, props.Count());
                CheckNumber(props, "A", 10);
                CheckString(props, "B", "xx");
                CheckString(props, "c", "20_xx");

                // Check UseComplex frame
                var locals_m1 = await GetLocalsForFrame(pause_location["callFrames"][3], debugger_test_loc, 23, 8, "UseComplex");
                Assert.Equal(7, locals_m1.Count());

                CheckNumber(locals_m1, "a", 10);
                CheckNumber(locals_m1, "b", 20);
                CheckObject(locals_m1, "complex", "Simple.Complex");
                CheckNumber(locals_m1, "c", 30);
                CheckNumber(locals_m1, "d", 50);
                CheckNumber(locals_m1, "e", 60);
                CheckNumber(locals_m1, "f", 0);

                props = await GetObjectOnFrame(pause_location["callFrames"][3], "complex");
                Assert.Equal(3, props.Count());
                CheckNumber(props, "A", 10);
                CheckString(props, "B", "xx");
                CheckString(props, "c", "20_xx");

                pause_location = await StepAndCheck(StepKind.Over, dep_cs_loc, 23, 8, "DoStuff", times: 2);
                // Check UseComplex frame again
                locals_m1 = await GetLocalsForFrame(pause_location["callFrames"][1], debugger_test_loc, 23, 8, "UseComplex");
                Assert.Equal(7, locals_m1.Count());

                CheckNumber(locals_m1, "a", 10);
                CheckNumber(locals_m1, "b", 20);
                CheckObject(locals_m1, "complex", "Simple.Complex");
                CheckNumber(locals_m1, "c", 30);
                CheckNumber(locals_m1, "d", 50);
                CheckNumber(locals_m1, "e", 60);
                CheckNumber(locals_m1, "f", 0);

                props = await GetObjectOnFrame(pause_location["callFrames"][1], "complex");
                Assert.Equal(3, props.Count());
                CheckNumber(props, "A", 10);
                CheckString(props, "B", "xx");
                CheckString(props, "c", "20_xx");
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInPreviousFramesDuringSteppingIn(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
                await SetBreakpoint(debugger_test_loc, 111, 12);

                // Will stop in InnerMethod
                var wait_res = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_outer_method(); }, 1);",
                    debugger_test_loc, 111, 12, "InnerMethod",
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(4, locals.Count());
                        CheckNumber(locals, "i", 5);
                        CheckNumber(locals, "j", 24);
                        CheckString(locals, "foo_str", "foo");
                        CheckObject(locals, "this", "Math.NestedInMath");
                    }
                );

                var this_props = await GetObjectOnFrame(wait_res["callFrames"][0], "this");
                Assert.Equal(2, this_props.Count());
                CheckObject(this_props, "m", "Math");
                CheckValueType(this_props, "SimpleStructProperty", "Math.SimpleStruct");

                var ss_props = await GetObjectOnLocals(this_props, "SimpleStructProperty");
                var dt = new DateTime(2020, 1, 2, 3, 4, 5);
                await CheckProps(ss_props, new
                {
                    dt = TDateTime(dt),
                    gs = TValueType("Math.GenericStruct<System.DateTime>")
                }, "ss_props");

                // Check OuterMethod frame
                var locals_m1 = await GetLocalsForFrame(wait_res["callFrames"][1], debugger_test_loc, 87, 8, "OuterMethod");
                Assert.Equal(5, locals_m1.Count());
                // FIXME: Failing test CheckNumber (locals_m1, "i", 5);
                // FIXME: Failing test CheckString (locals_m1, "text", "Hello");
                CheckNumber(locals_m1, "new_i", 0);
                CheckNumber(locals_m1, "k", 0);
                CheckObject(locals_m1, "nim", "Math.NestedInMath");

                // step back into OuterMethod
                await StepAndCheck(StepKind.Over, debugger_test_loc, 91, 8, "OuterMethod", times: 9,
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(5, locals.Count());

                        // FIXME: Failing test CheckNumber (locals_m1, "i", 5);
                        CheckString(locals, "text", "Hello");
                        // FIXME: Failing test CheckNumber (locals, "new_i", 24);
                        CheckNumber(locals, "k", 19);
                        CheckObject(locals, "nim", "Math.NestedInMath");
                    }
                );

                //await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 81, 2, "OuterMethod", times: 2);

                // step into InnerMethod2
                await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 96, 4, "InnerMethod2",
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(3, locals.Count());

                        CheckString(locals, "s", "test string");
                        //out var: CheckNumber (locals, "k", 0);
                        CheckNumber(locals, "i", 24);
                    }
                );

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 100, 4, "InnerMethod2", times: 4,
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(3, locals.Count());

                        CheckString(locals, "s", "test string");
                        // FIXME: Failing test CheckNumber (locals, "k", 34);
                        CheckNumber(locals, "i", 24);
                    }
                );

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 92, 8, "OuterMethod", times: 2,
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(5, locals.Count());

                        CheckString(locals, "text", "Hello");
                        // FIXME: failing test CheckNumber (locals, "i", 5);
                        CheckNumber(locals, "new_i", 22);
                        CheckNumber(locals, "k", 34);
                        CheckObject(locals, "nim", "Math.NestedInMath");
                    }
                );
            });
        }

        [Fact]
        public async Task InspectLocalsDuringSteppingIn()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 86, 8);

                await EvaluateAndCheck("window.setTimeout(function() { invoke_outer_method(); }, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs", 86, 8, "OuterMethod",
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(5, locals.Count());

                        CheckObject(locals, "nim", "Math.NestedInMath");
                        CheckNumber(locals, "i", 5);
                        CheckNumber(locals, "k", 0);
                        CheckNumber(locals, "new_i", 0);
                        CheckString(locals, "text", null);
                    }
                );

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 87, 8, "OuterMethod",
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(5, locals.Count());

                        CheckObject(locals, "nim", "Math.NestedInMath");
                        // FIXME: Failing test CheckNumber (locals, "i", 5);
                        CheckNumber(locals, "k", 0);
                        CheckNumber(locals, "new_i", 0);
                        CheckString(locals, "text", "Hello");
                    }
                );

                // Step into InnerMethod
                await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 105, 8, "InnerMethod");
                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 109, 12, "InnerMethod", times: 5,
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(4, locals.Count());

                        CheckNumber(locals, "i", 5);
                        CheckNumber(locals, "j", 15);
                        CheckString(locals, "foo_str", "foo");
                        CheckObject(locals, "this", "Math.NestedInMath");
                    }
                );

                // Step back to OuterMethod
                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 88, 8, "OuterMethod", times: 6,
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(5, locals.Count());

                        CheckObject(locals, "nim", "Math.NestedInMath");
                        // FIXME: Failing test CheckNumber (locals, "i", 5);
                        CheckNumber(locals, "k", 0);
                        CheckNumber(locals, "new_i", 24);
                        CheckString(locals, "text", "Hello");
                    }
                );
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInAsyncMethods(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

                await SetBreakpoint(debugger_test_loc, 120, 12);
                await SetBreakpoint(debugger_test_loc, 135, 12);

                // Will stop in Asyncmethod0
                var wait_res = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_async_method_with_await(); }, 1);",
                    debugger_test_loc, 120, 12, "MoveNext", //FIXME:
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(4, locals.Count());
                        CheckString(locals, "s", "string from js");
                        CheckNumber(locals, "i", 42);
                        CheckString(locals, "local0", "value0");
                        CheckObject(locals, "this", "Math.NestedInMath");
                    }
                );
                Console.WriteLine(wait_res);

#if false // Disabled for now, as we don't have proper async traces
                var locals = await GetProperties(wait_res["callFrames"][2]["callFrameId"].Value<string>());
                Assert.Equal(4, locals.Count());
                CheckString(locals, "ls", "string from jstest");
                CheckNumber(locals, "li", 52);
#endif

                // TODO: previous frames have async machinery details, so no point checking that right now

                var pause_loc = await SendCommandAndCheck(null, "Debugger.resume", debugger_test_loc, 135, 12, /*FIXME: "AsyncMethodNoReturn"*/ "MoveNext",
                    locals_fn: (locals) =>
                    {
                        Assert.Equal(4, locals.Count());
                        CheckString(locals, "str", "AsyncMethodNoReturn's local");
                        CheckObject(locals, "this", "Math.NestedInMath");
                        //FIXME: check fields
                        CheckValueType(locals, "ss", "Math.SimpleStruct");
                        CheckArray(locals, "ss_arr", "Math.SimpleStruct[]", 0);
                        // TODO: struct fields
                    }
                );

                var this_props = await GetObjectOnFrame(pause_loc["callFrames"][0], "this");
                Assert.Equal(2, this_props.Count());
                CheckObject(this_props, "m", "Math");
                CheckValueType(this_props, "SimpleStructProperty", "Math.SimpleStruct");

                // TODO: Check `this` properties
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithStructs(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                await SetBreakpoint(debugger_test_loc, 24, 8);

                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_method_with_structs(); }, 1);",
                    debugger_test_loc, 24, 8, "MethodWithLocalStructs");

                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    ss_local = TValueType("DebuggerTests.ValueTypesTest.SimpleStruct"),
                    gs_local = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<DebuggerTests.ValueTypesTest>"),
                    vt_local = TObject("DebuggerTests.ValueTypesTest")
                }, "locals");

                var dt = new DateTime(2021, 2, 3, 4, 6, 7);
                var vt_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "vt_local");
                Assert.Equal(5, vt_local_props.Count());

                CheckString(vt_local_props, "StringField", "string#0");
                CheckValueType(vt_local_props, "SimpleStructField", "DebuggerTests.ValueTypesTest.SimpleStruct");
                CheckValueType(vt_local_props, "SimpleStructProperty", "DebuggerTests.ValueTypesTest.SimpleStruct");
                await CheckDateTime(vt_local_props, "DT", new DateTime(2020, 1, 2, 3, 4, 5));
                CheckEnum(vt_local_props, "RGB", "DebuggerTests.RGB", "Blue");

                // Check ss_local's properties
                var ss_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ss_local");
                await CheckProps(ss_local_props, new
                {
                    V = TGetter("V"),
                    str_member = TString("set in MethodWithLocalStructs#SimpleStruct#str_member"),
                    dt = TDateTime(dt),
                    gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
                    Kind = TEnum("System.DateTimeKind", "Utc")
                }, "ss_local");

                {
                    var gres = await InvokeGetter(GetAndAssertObjectWithName(locals, "ss_local"), "V");
                    await CheckValue(gres.Value["result"], TNumber(0xDEADBEEF + 2), $"ss_local#V");

                    // Check ss_local.gs
                    var gs_props = await GetObjectOnLocals(ss_local_props, "gs");
                    CheckString(gs_props, "StringField", "set in MethodWithLocalStructs#SimpleStruct#gs#StringField");
                    CheckObject(gs_props, "List", "System.Collections.Generic.List<System.DateTime>");
                }

                // Check gs_local's properties
                var gs_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "gs_local");
                await CheckProps(gs_local_props, new
                {
                    StringField = TString("gs_local#GenericStruct<ValueTypesTest>#StringField"),
                    List = TObject("System.Collections.Generic.List<DebuggerTests.ValueTypesTest>", is_null: true),
                    Options = TEnum("DebuggerTests.Options", "None")
                }, "gs_local");

                // Check vt_local's properties

                var exp = new[]
                {
                    ("SimpleStructProperty", 2, "Utc"),
                    ("SimpleStructField", 5, "Local")
                };

                foreach (var (name, bias, dt_kind) in exp)
                {
                    dt = new DateTime(2020 + bias, 1 + bias, 2 + bias, 3 + bias, 5 + bias, 6 + bias);
                    await CompareObjectPropertiesFor(vt_local_props, name,
                        new
                        {
                            V = TGetter("V"),
                            str_member = TString($"{name}#string#0#SimpleStruct#str_member"),
                            dt = TDateTime(dt),
                            gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
                            Kind = TEnum("System.DateTimeKind", dt_kind)
                        },
                        label: $"vt_local_props.{name}");

                    var gres = await InvokeGetter(GetAndAssertObjectWithName(vt_local_props, name), "V");
                    await CheckValue(gres.Value["result"], TNumber(0xDEADBEEF + (uint)dt.Month), $"{name}#V");
                }

                // FIXME: check ss_local.gs.List's members
            });
        }

        [Theory]
        [InlineData("BoxingTest", false)]
        [InlineData("BoxingTestAsync", true)]
        public async Task InspectBoxedLocals(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTest",
            method_name,
            17,
            is_async ? "MoveNext" : method_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTest:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                await CheckProps(locals, new
                {
                    n_i    = TNumber(5),
                    o_i    = TNumber(5),
                    o_n_i  = TNumber(5),
                    o_s    = TString("foobar"),
                    o_obj  = TObject("Math"),

                    n_gs   = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    o_gs   = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    o_n_gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),

                    n_dt   = TDateTime(dt),
                    o_dt   = TDateTime(dt),
                    o_n_dt = TDateTime(dt),

                    o_null = TObject("object", is_null: true),
                    o_ia   = TArray("int[]", 2),
                }, "locals");

                foreach (var name in new[] { "n_gs", "o_gs", "o_n_gs" })
                {
                    var gs = GetAndAssertObjectWithName(locals, name);
                    var gs_props = await GetProperties(gs["value"]?["objectId"]?.Value<string> ());
                    await CheckProps(gs_props, new
                    {
                        List        = TObject("System.Collections.Generic.List<int>", is_null: true),
                        StringField = TString("n_gs#StringField"),
                        Options     = TEnum  ("DebuggerTests.Options", "None")
                    }, name);
                }

                var o_ia_props = await GetObjectOnLocals(locals, "o_ia");
                await CheckProps(o_ia_props, new[]
                {
                    TNumber(918),
                    TNumber(58971)
                }, nameof(o_ia_props));
            });

        [Theory]
        [InlineData("BoxedTypeObjectTest", false)]
        [InlineData("BoxedTypeObjectTestAsync", true)]
        public async Task InspectBoxedTypeObject(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTest",
            method_name,
            9,
            is_async ? "MoveNext" : method_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTest:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                await CheckProps(locals, new
                {
                    i = TNumber(5),
                    o0 = TNumber(5),
                    o1 = TNumber(5),
                    o2 = TNumber(5),
                    o3 = TNumber(5),

                    oo = TObject("object"),
                    oo0 = TObject("object"),
                }, "locals");
            });

        [Theory]
        [InlineData("BoxedAsClass", false)]
        [InlineData("BoxedAsClassAsync", true)]
        public async Task InspectBoxedAsClassLocals(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTest",
            method_name,
            6,
            is_async ? "MoveNext" : method_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTest:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                Console.WriteLine (locals);

                await CheckProps(locals, new
                {
                    vt_dt = TDateTime(new DateTime(4819, 5, 6, 7, 8, 9)),
                    vt_gs = TValueType("Math.GenericStruct<string>"),
                    e     = TEnum("System.IO.FileMode", "0"),
                    ee    = TEnum("System.IO.FileMode", "Append")
                }, "locals");
            });


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeMethodArgs(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                await SetBreakpoint(debugger_test_loc, 36, 12);

                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:TestStructsAsMethodArgs'); }, 1);",
                    debugger_test_loc, 36, 12, "MethodWithStructArgs");
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                {
                    Assert.Equal(3, locals.Count());
                    CheckString(locals, "label", "TestStructsAsMethodArgs#label");
                    CheckValueType(locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
                    CheckNumber(locals, "x", 3);
                }

                var dt = new DateTime(2025, 6, 7, 8, 10, 11);
                var ss_local_as_ss_arg = new
                {
                    V = TGetter("V"),
                    str_member = TString("ss_local#SimpleStruct#string#0#SimpleStruct#str_member"),
                    dt = TDateTime(dt),
                    gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
                    Kind = TEnum("System.DateTimeKind", "Local")
                };
                var ss_local_gs = new
                {
                    StringField = TString("ss_local#SimpleStruct#string#0#SimpleStruct#gs#StringField"),
                    List = TObject("System.Collections.Generic.List<System.DateTime>"),
                    Options = TEnum("DebuggerTests.Options", "Option1")
                };

                // Check ss_arg's properties
                var ss_arg_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ss_arg");
                await CheckProps(ss_arg_props, ss_local_as_ss_arg, "ss_arg");

                var res = await InvokeGetter(GetAndAssertObjectWithName(locals, "ss_arg"), "V");
                await CheckValue(res.Value["result"], TNumber(0xDEADBEEF + (uint)dt.Month), "ss_arg#V");

                {
                    // Check ss_local.gs
                    await CompareObjectPropertiesFor(ss_arg_props, "gs", ss_local_gs);
                }

                pause_location = await StepAndCheck(StepKind.Over, debugger_test_loc, 40, 8, "MethodWithStructArgs", times: 4,
                    locals_fn: (l) => { /* non-null to make sure that locals get fetched */ });
                locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                {
                    Assert.Equal(3, locals.Count());

                    CheckString(locals, "label", "TestStructsAsMethodArgs#label");
                    CheckValueType(locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
                    CheckNumber(locals, "x", 3);
                }

                var ss_arg_updated = new
                {
                    V = TGetter("V"),
                    str_member = TString("ValueTypesTest#MethodWithStructArgs#updated#ss_arg#str_member"),
                    dt = TDateTime(dt),
                    gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
                    Kind = TEnum("System.DateTimeKind", "Utc")
                };

                ss_arg_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ss_arg");
                await CheckProps(ss_arg_props, ss_arg_updated, "ss_arg");

                res = await InvokeGetter(GetAndAssertObjectWithName(locals, "ss_arg"), "V");
                await CheckValue(res.Value["result"], TNumber(0xDEADBEEF + (uint)dt.Month), "ss_arg#V");

                {
                    // Check ss_local.gs
                    await CompareObjectPropertiesFor(ss_arg_props, "gs", new
                    {
                        StringField = TString("ValueTypesTest#MethodWithStructArgs#updated#gs#StringField#3"),
                        List = TObject("System.Collections.Generic.List<System.DateTime>"),
                        Options = TEnum("DebuggerTests.Options", "Option1")
                    });
                }

                // Check locals on previous frame, same as earlier in this test
                ss_arg_props = await GetObjectOnFrame(pause_location["callFrames"][1], "ss_local");
                await CheckProps(ss_arg_props, ss_local_as_ss_arg, "ss_local");

                {
                    // Check ss_local.dt
                    await CheckDateTime(ss_arg_props, "dt", dt);

                    // Check ss_local.gs
                    var gs_props = await GetObjectOnLocals(ss_arg_props, "gs");
                    CheckString(gs_props, "StringField", "ss_local#SimpleStruct#string#0#SimpleStruct#gs#StringField");
                    CheckObject(gs_props, "List", "System.Collections.Generic.List<System.DateTime>");
                }

                // ----------- Step back to the caller ---------

                pause_location = await StepAndCheck(StepKind.Over, debugger_test_loc, 30, 12, "TestStructsAsMethodArgs",
                    times: 2, locals_fn: (l) => { /* non-null to make sure that locals get fetched */ });
                locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    ss_local = TValueType("DebuggerTests.ValueTypesTest.SimpleStruct"),
                    ss_ret = TValueType("DebuggerTests.ValueTypesTest.SimpleStruct")
                },
                    "locals#0");

                ss_arg_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ss_local");
                await CheckProps(ss_arg_props, ss_local_as_ss_arg, "ss_local");

                {
                    // Check ss_local.gs
                    await CompareObjectPropertiesFor(ss_arg_props, "gs", ss_local_gs, label: "ss_local_gs");
                }

                // FIXME: check ss_local.gs.List's members
            });
        }

        [Fact]
        public async Task CheckUpdatedValueTypeFieldsOnResume()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                var lines = new[] { 205, 208 };
                await SetBreakpoint(debugger_test_loc, lines[0], 12);
                await SetBreakpoint(debugger_test_loc, lines[1], 12);

                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeMembers'); }, 1);",
                    debugger_test_loc, lines[0], 12, "MethodUpdatingValueTypeMembers");

                await CheckLocals(pause_location, new DateTime(1, 2, 3, 4, 5, 6), new DateTime(4, 5, 6, 7, 8, 9));

                // Resume
                pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "MethodUpdatingValueTypeMembers");
                await CheckLocals(pause_location, new DateTime(9, 8, 7, 6, 5, 4), new DateTime(5, 1, 3, 7, 9, 10));
            });

            async Task CheckLocals(JToken pause_location, DateTime obj_dt, DateTime vt_dt)
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    obj = TObject("DebuggerTests.ClassForToStringTests"),
                    vt = TObject("DebuggerTests.StructForToStringTests")
                }, "locals");

                var obj_props = await GetObjectOnLocals(locals, "obj");
                {
                    await CheckProps(obj_props, new
                    {
                        DT = TDateTime(obj_dt)
                    }, "locals#obj.DT", num_fields: 5);
                }

                var vt_props = await GetObjectOnLocals(locals, "vt");
                {
                    await CheckProps(vt_props, new
                    {
                        DT = TDateTime(vt_dt)
                    }, "locals#obj.DT", num_fields: 5);
                }
            }
        }

        [Fact]
        public async Task CheckUpdatedValueTypeLocalsOnResumeAsync()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                var lines = new[] { 214, 216 };
                await SetBreakpoint(debugger_test_loc, lines[0], 12);
                await SetBreakpoint(debugger_test_loc, lines[1], 12);

                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeLocalsAsync'); }, 1);",
                    debugger_test_loc, lines[0], 12, "MoveNext");

                var dt = new DateTime(1, 2, 3, 4, 5, 6);
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckDateTime(locals, "dt", dt);

                // Resume
                dt = new DateTime(9, 8, 7, 6, 5, 4);
                pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "MoveNext");
                locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckDateTime(locals, "dt", dt);
            });
        }

        [Fact]
        public async Task CheckUpdatedVTArrayMembersOnResume()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                var lines = new[] { 225, 227 };
                await SetBreakpoint(debugger_test_loc, lines[0], 12);
                await SetBreakpoint(debugger_test_loc, lines[1], 12);

                var dt = new DateTime(1, 2, 3, 4, 5, 6);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingVTArrayMembers'); }, 1);",
                    debugger_test_loc, lines[0], 12, "MethodUpdatingVTArrayMembers");
                await CheckArrayElements(pause_location, dt);

                // Resume
                dt = new DateTime(9, 8, 7, 6, 5, 4);
                pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "MethodUpdatingVTArrayMembers");
                await CheckArrayElements(pause_location, dt);
            });

            async Task CheckArrayElements(JToken pause_location, DateTime dt)
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    ssta = TArray("DebuggerTests.StructForToStringTests[]", 1)
                }, "locals");

                var ssta = await GetObjectOnLocals(locals, "ssta");
                var sst0 = await GetObjectOnLocals(ssta, "0");
                await CheckProps(sst0, new
                {
                    DT = TDateTime(dt)
                }, "dta [0]", num_fields: 5);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithStructsStaticAsync(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                await SetBreakpoint(debugger_test_loc, 54, 12);

                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async (" +
                    "'[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalStructsStaticAsync'" +
                    "); }, 1);",
                    debugger_test_loc, 54, 12, "MoveNext"); //BUG: method name

                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    ss_local = TObject("DebuggerTests.ValueTypesTest.SimpleStruct"),
                    gs_local = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    result = TBool(true)
                },
                    "locals#0");

                var dt = new DateTime(2021, 2, 3, 4, 6, 7);
                // Check ss_local's properties
                var ss_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ss_local");
                await CheckProps(ss_local_props, new
                {
                    V = TGetter("V"),
                    str_member = TString("set in MethodWithLocalStructsStaticAsync#SimpleStruct#str_member"),
                    dt = TDateTime(dt),
                    gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
                    Kind = TEnum("System.DateTimeKind", "Utc")
                }, "ss_local");

                {
                    var gres = await InvokeGetter(GetAndAssertObjectWithName(locals, "ss_local"), "V");
                    await CheckValue(gres.Value["result"], TNumber(0xDEADBEEF + 2), $"ss_local#V");

                    // Check ss_local.gs
                    await CompareObjectPropertiesFor(ss_local_props, "gs",
                        new
                        {
                            StringField = TString("set in MethodWithLocalStructsStaticAsync#SimpleStruct#gs#StringField"),
                            List = TObject("System.Collections.Generic.List<System.DateTime>"),
                            Options = TEnum("DebuggerTests.Options", "Option1")
                        }
                    );
                }

                // Check gs_local's properties
                var gs_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "gs_local");
                await CheckProps(gs_local_props, new
                {
                    StringField = TString("gs_local#GenericStruct<ValueTypesTest>#StringField"),
                    List = TObject("System.Collections.Generic.List<int>"),
                    Options = TEnum("DebuggerTests.Options", "Option2")
                }, "gs_local");

                // FIXME: check ss_local.gs.List's members
            });
        }

        [Theory]
        [InlineData(137, 12, "MethodWithLocalsForToStringTest", false, false)]
        [InlineData(147, 12, "MethodWithArgumentsForToStringTest", true, false)]
        [InlineData(192, 12, "MethodWithArgumentsForToStringTestAsync", true, true)]
        [InlineData(182, 12, "MethodWithArgumentsForToStringTestAsync", false, true)]
        public async Task InspectLocalsForToStringDescriptions(int line, int col, string method_name, bool call_other, bool invoke_async)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            string entry_method_name = $"[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalsForToStringTest{(invoke_async ? "Async" : String.Empty)}";
            int frame_idx = 0;

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() {" +
                    (invoke_async ? "invoke_static_method_async (" : "invoke_static_method (") +
                    $"'{entry_method_name}'," +
                    (call_other ? "true" : "false") +
                    "); }, 1);";
                Console.WriteLine($"{eval_expr}");

                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, invoke_async ? "MoveNext" : method_name);

                var dt0 = new DateTime(2020, 1, 2, 3, 4, 5);
                var dt1 = new DateTime(2010, 5, 4, 3, 2, 1);
                var ts = dt0 - dt1;
                var dto = new DateTimeOffset(dt0, new TimeSpan(4, 5, 0));

                var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    call_other = TBool(call_other),
                    dt0 = TDateTime(dt0),
                    dt1 = TDateTime(dt1),
                    dto = TValueType("System.DateTimeOffset", dto.ToString()),
                    ts = TValueType("System.TimeSpan", ts.ToString()),
                    dec = TValueType("System.Decimal", "123987123"),
                    guid = TValueType("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014"),
                    dts = TArray("System.DateTime[]", 2),
                    obj = TObject("DebuggerTests.ClassForToStringTests"),
                    sst = TObject("DebuggerTests.StructForToStringTests")
                }, "locals#0");

                var dts_0 = new DateTime(1983, 6, 7, 5, 6, 10);
                var dts_1 = new DateTime(1999, 10, 15, 1, 2, 3);
                var dts_elements = await GetObjectOnLocals(frame_locals, "dts");
                await CheckDateTime(dts_elements, "0", dts_0);
                await CheckDateTime(dts_elements, "1", dts_1);

                // TimeSpan
                await CompareObjectPropertiesFor(frame_locals, "ts",
                    new
                    {
                        Days = TNumber(3530),
                        Minutes = TNumber(2),
                        Seconds = TNumber(4),
                    }, "ts_props", num_fields: 12);

                // DateTimeOffset
                await CompareObjectPropertiesFor(frame_locals, "dto",
                    new
                    {
                        Day = TNumber(2),
                        Year = TNumber(2020),
                        DayOfWeek = TEnum("System.DayOfWeek", "Thursday")
                    }, "dto_props", num_fields: 22);

                var DT = new DateTime(2004, 10, 15, 1, 2, 3);
                var DTO = new DateTimeOffset(dt0, new TimeSpan(2, 14, 0));

                await CompareObjectPropertiesFor(frame_locals, "obj",
                    new
                    {
                        DT = TDateTime(DT),
                        DTO = TValueType("System.DateTimeOffset", DTO.ToString()),
                        TS = TValueType("System.TimeSpan", ts.ToString()),
                        Dec = TValueType("System.Decimal", "1239871"),
                        Guid = TValueType("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014")
                    }, "obj_props");

                DTO = new DateTimeOffset(dt0, new TimeSpan(3, 15, 0));
                var sst_props = await CompareObjectPropertiesFor(frame_locals, "sst",
                    new
                    {
                        DT = TDateTime(DT),
                        DTO = TValueType("System.DateTimeOffset", DTO.ToString()),
                        TS = TValueType("System.TimeSpan", ts.ToString()),
                        Dec = TValueType("System.Decimal", "1239871"),
                        Guid = TValueType("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014")
                    }, "sst_props");
            });
        }

        [Fact]
        public async Task InspectLocals()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var wait_res = await RunUntil("locals_inner");
                var locals = await GetProperties(wait_res["callFrames"][1]["callFrameId"].Value<string>());
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsForStructInstanceMethod(bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-array-test.cs", 258, 12,
            "GenericInstanceMethod<DebuggerTests.SimpleClass>",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EntryClass:run'); })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               await CheckProps(frame_locals, new
               {
                   sc_arg = TObject("DebuggerTests.SimpleClass"),
                   @this = TValueType("DebuggerTests.Point"),
                   local_gs = TValueType("DebuggerTests.SimpleGenericStruct<int>")
               },
                   "locals#0");

               await CompareObjectPropertiesFor(frame_locals, "local_gs",
                   new
                   {
                       Id = TString("local_gs#Id"),
                       Color = TEnum("DebuggerTests.RGB", "Green"),
                       Value = TNumber(4)
                   },
                   label: "local_gs#0");

               await CompareObjectPropertiesFor(frame_locals, "sc_arg",
                   TSimpleClass(10, 45, "sc_arg#Id", "Blue"),
                   label: "sc_arg#0");

               await CompareObjectPropertiesFor(frame_locals, "this",
                   TPoint(90, -4, "point#Id", "Green"),
                   label: "this#0");

           });

        [Fact]
        public async Task SteppingIntoMscorlib()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 83, 8);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:OuterMethod'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs", 83, 8,
                    "OuterMethod");

                //make sure we're on the right bp
                Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                pause_location = await SendCommandAndCheck(null, $"Debugger.stepInto", null, -1, -1, null);
                var top_frame = pause_location["callFrames"][0];

                AssertEqual("WriteLine", top_frame["functionName"]?.Value<string>(), "Expected to be in WriteLine method");
                var script_id = top_frame["functionLocation"]["scriptId"].Value<string>();
                Assert.Matches ("^dotnet://(mscorlib|System\\.Console)\\.dll/Console.cs", scripts[script_id]);
            });
        }

        [Fact]
        public async Task InvalidValueTypeData()
        {
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 85, 8,
                "OuterMethod",
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:OuterMethod'); })",
                wait_for_event_fn: async (pause_location) =>
               {
                   var new_id = await CreateNewId(@"MONO._new_or_add_id_props ({ scheme: 'valuetype', idArgs: { containerId: 1 }, props: { klass: 3, value64: 4 }});");
                   await _invoke_getter(new_id, "NonExistant", expect_ok: false);

                   new_id = await CreateNewId(@"MONO._new_or_add_id_props ({ scheme: 'valuetype', idArgs: { containerId: 1 }, props: { klass: 3 }});");
                   await _invoke_getter(new_id, "NonExistant", expect_ok: false);

                   new_id = await CreateNewId(@"MONO._new_or_add_id_props ({ scheme: 'valuetype', idArgs: { containerId: 1 }, props: { klass: 3, value64: 'AA' }});");
                   await _invoke_getter(new_id, "NonExistant", expect_ok: false);
               });

            async Task<string> CreateNewId(string expr)
            {
                var res = await ctx.cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = expr }), ctx.token);
                Assert.True(res.IsOk, "Expected Runtime.evaluate to succeed");
                AssertEqual("string", res.Value["result"]?["type"]?.Value<string>(), "Expected Runtime.evaluate to return a string type result");
                return res.Value["result"]?["value"]?.Value<string>();
            }

            async Task<Result> _invoke_getter(string obj_id, string property_name, bool expect_ok)
            {
                var expr = $"MONO._invoke_getter ('{obj_id}', '{property_name}')";
                var res = await ctx.cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = expr }), ctx.token);
                AssertEqual(expect_ok, res.IsOk, "Runtime.evaluate result not as expected for {expr}");

                return res;
            }
        }

        [Fact]
        public async Task CreateGoodBreakpointAndHitAndRemoveAndDontHit()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
                var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_add(); invoke_add()}, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs",  10, 8,
                    "IntAdd");

                Assert.Equal("other", pause_location["reason"]?.Value<string>());
                Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
                await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
                await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
            });
        }

        [Fact]
        public async Task CreateGoodBreakpointAndHitAndRemoveTwice()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
                var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_add(); invoke_add()}, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs",  10, 8,
                    "IntAdd");

                Assert.Equal("other", pause_location["reason"]?.Value<string>());
                Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
                await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
            });
        }

        [Fact]
        public async Task CreateGoodBreakpointAndHitAndRemoveAndDontHitAndCreateAgainAndHit()
        {
            var insp = new Inspector();

            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
                var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_add(); invoke_add(); invoke_add(); invoke_add()}, 1);",
                    "dotnet://debugger-test.dll/debugger-test.cs",  10, 8,
                    "IntAdd");

                Assert.Equal("other", pause_location["reason"]?.Value<string>());
                Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
                await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
                await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd");
                bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
                await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "IntAdd");

            });
        }

        [Fact]
        public async Task MulticastDelegateTest() => await CheckInspectLocalsAtBreakpointSite(
            "MulticastDelegateTestClass", "Test", 5, "Test",
            "window.setTimeout(function() { invoke_static_method('[debugger-test] MulticastDelegateTestClass:run'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var this_props = await GetObjectOnLocals(frame_locals, "this");
                await CheckProps(this_props, new
                {
                    TestEvent = TSymbol("System.EventHandler<string>"),
                    Delegate = TSymbol("System.MulticastDelegate")
                }, "this_props");
            });

        [Theory]
        [InlineData("EmptyClass", false)]
        [InlineData("EmptyClass", true)]
        [InlineData("EmptyStruct", false)]
        [InlineData("EmptyStruct", true)]
        public async Task EmptyTypeWithNoLocalsOrParams(string type_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            type_name,
            $"StaticMethodWithNoLocals{ (is_async ? "Async" : "") }",
            1,
            is_async ? "MoveNext" : "StaticMethodWithNoLocals",
            $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] {type_name}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                AssertEqual(0, frame_locals.Values<JToken>().Count(), "locals");
            });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StaticMethodWithLocalEmptyStructThatWillGetExpanded(bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "EmptyStruct",
            $"StaticMethodWithLocalEmptyStruct{ (is_async ? "Async" : "") }",
            1,
            is_async ? "MoveNext" : "StaticMethodWithLocalEmptyStruct",
            $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] EmptyStruct:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    es = TValueType("EmptyStruct")
                }, "locals");

                var es = GetAndAssertObjectWithName(frame_locals, "es");
                var es_props = await GetProperties(es["value"]["objectId"]?.Value<string>());
                AssertEqual(0, es_props.Values<JToken>().Count(), "es_props");
            });

        [Fact]
        public async Task StepOverAsyncMethod()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpointInMethod("debugger-test.dll", "AsyncStepClass", "TestAsyncStepOut2", 2);
                System.Console.WriteLine(bp);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] AsyncStepClass:TestAsyncStepOut'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 19, 8,
                    "MoveNext");

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 8, "MoveNext");

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 22, 4, "MoveNext");

                await StepAndCheck(StepKind.Over, null, 0, 0, "get_IsCompletedSuccessfully"); //not check the line number and the file name because this can be changed

            });
        }

        [Fact]
        public async Task PreviousFrameForAReflectedCall() => await CheckInspectLocalsAtBreakpointSite(
             "DebuggerTests.GetPropertiesTests.CloneableStruct", "SimpleStaticMethod", 1, "SimpleStaticMethod",
             "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.GetPropertiesTests.TestWithReflection:run'); })",
             wait_for_event_fn: async (pause_location) =>
             {
                 var frame = FindFrame(pause_location, "InvokeReflectedStaticMethod");
                 Assert.NotNull(frame);

                 var frame_locals = await GetProperties(frame["callFrameId"].Value<string>());

                 await CheckProps(frame_locals, new
                 {
                     mi = TObject("System.Reflection.MethodInfo"),
                     dt = TDateTime(new DateTime(4210, 3, 4, 5, 6, 7)),
                     i = TNumber(4),
                     strings = TArray("string[]", 1),
                     cs = TValueType("DebuggerTests.GetPropertiesTests.CloneableStruct"),

                     num = TNumber(10),
                     name = TString("foobar"),
                     some_date = TDateTime(new DateTime(1234, 6, 7, 8, 9, 10)),
                     num1 = TNumber(100),
                     str2 = TString("xyz"),
                     num3 = TNumber(345),
                     str3 = TString("abc")
                 }, "InvokeReflectedStaticMethod#locals");
             });

        JObject FindFrame(JObject pause_location, string function_name)
            => pause_location["callFrames"]
                    ?.Values<JObject>()
                    ?.Where(f => f["functionName"]?.Value<string>() == function_name)
                    ?.FirstOrDefault();

        [Fact]
        public async Task DebugLazyLoadedAssemblyWithPdb()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);
            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                int line = 9;
                await SetBreakpoint(".*/lazy-debugger-test.cs$", line, 0, use_regex: true);
                await LoadAssemblyDynamically(
                        Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.dll"),
                        Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.pdb"));

                var source_location = "dotnet://lazy-debugger-test.dll/lazy-debugger-test.cs";
                Assert.Contains(source_location, scripts.Values);

                var pause_location = await EvaluateAndCheck(
                   "window.setTimeout(function () { invoke_static_method('[lazy-debugger-test] LazyMath:IntAdd', 5, 10); }, 1);",
                   source_location, line, 8,
                   "IntAdd");
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                CheckNumber(locals, "a", 5);
                CheckNumber(locals, "b", 10);
            });
        }

        [Fact]
        public async Task DebugLazyLoadedAssemblyWithEmbeddedPdb()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);
            await Ready();

            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                int line = 9;
                await SetBreakpoint(".*/lazy-debugger-test-embedded.cs$", line, 0, use_regex: true);
                await LoadAssemblyDynamically(
                        Path.Combine(DebuggerTestAppPath, "lazy-debugger-test-embedded.dll"),
                        null);

                var source_location = "dotnet://lazy-debugger-test-embedded.dll/lazy-debugger-test-embedded.cs";
                Assert.Contains(source_location, scripts.Values);

                var pause_location = await EvaluateAndCheck(
                   "window.setTimeout(function () { invoke_static_method('[lazy-debugger-test-embedded] LazyMath:IntAdd', 5, 10); }, 1);",
                   source_location, line, 8,
                   "IntAdd");
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                CheckNumber(locals, "a", 5);
                CheckNumber(locals, "b", 10);
            });
        }

        [Fact]
        public async Task CannotDebugLazyLoadedAssemblyWithoutPdb()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);
            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                int line = 9;
                await SetBreakpoint(".*/lazy-debugger-test.cs$", line, 0, use_regex: true);
                await LoadAssemblyDynamically(
                        Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.dll"),
                        null);

                // wait to bit to catch if the event might be raised a bit late
                await Task.Delay(1000);

                var source_location = "dotnet://lazy-debugger-test.dll/lazy-debugger-test.cs";
                Assert.DoesNotContain(source_location, scripts.Values);
            });
        }

        async Task LoadAssemblyDynamically(string asm_file, string pdb_file)
        {
            // Simulate loading an assembly into the framework
            byte[] bytes = File.ReadAllBytes(asm_file);
            string asm_base64 = Convert.ToBase64String(bytes);

            string pdb_base64 = null;
            if (pdb_file != null) {
                bytes = File.ReadAllBytes(pdb_file);
                pdb_base64 = Convert.ToBase64String(bytes);
            }

            var load_assemblies = JObject.FromObject(new
            {
                expression = $"{{ let asm_b64 = '{asm_base64}'; let pdb_b64 = '{pdb_base64}'; invoke_static_method('[debugger-test] LoadDebuggerTest:LoadLazyAssembly', asm_b64, pdb_b64); }}"
            });

            Result load_assemblies_res = await ctx.cli.SendCommand("Runtime.evaluate", load_assemblies, ctx.token);
            Assert.True(load_assemblies_res.IsOk);
        }

        //TODO add tests covering basic stepping behavior as step in/out/over
    }
}
