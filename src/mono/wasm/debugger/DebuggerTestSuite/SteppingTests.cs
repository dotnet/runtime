using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class SteppingTests : DebuggerTestBase
    {
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
        public async Task InspectValueTypeMethodArgsWhileStepping(bool use_cfo)
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

        // [Fact]
        //https://github.com/dotnet/runtime/issues/42421
        public async Task BreakAfterAwaitThenStepOverTillBackToCaller()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                    "MoveNext");

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 23, 12, "MoveNext");

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 24, 8, "MoveNext");

                await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 15, 12, "MoveNext");
            });
        }

        // [Fact]
        //[ActiveIssue("https://github.com/dotnet/runtime/issues/42421")]
        public async Task StepOutOfAsyncMethod()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                    "MoveNext");

                await StepAndCheck(StepKind.Out, source_file, 15, 4, "TestAsyncStepOut");
            });
        }

        [Fact]
        public async Task ResumeOutOfAsyncMethodToAsyncCallerWithBreakpoint()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                    "MoveNext");

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut", 2);
                await SendCommandAndCheck(null, "Debugger.resume", source_file, 16, 8, "MoveNext");
            });
        }

        [Fact]
        public async Task StepOutOfNonAsyncMethod()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "OtherMethod0", 1);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:SimpleMethod'); }, 1);",
                    source_file, -1, -1,
                    "OtherMethod0");

                await StepAndCheck(StepKind.Out, source_file, 29, 12, "SimpleMethod");
            });
        }

        // [Fact]
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/42424")]
        public async Task BreakOnAwaitThenStepOverToNextAwaitCall()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 2);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 53, 12,
                    "MoveNext");

                await StepAndCheck(StepKind.Over, source_file, 54, 12, "MoveNext");
            });
        }

        // [Fact]
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/42424")]
        public async Task BreakOnAwaitThenStepOverToNextLine()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 1);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 46, 12,
                    "MoveNext");

                // BUG: chrome: not able to show any bp line indicator
                await StepAndCheck(StepKind.Over, source_file, 47, 12, "MoveNext");
            });
        }

        [Fact]
        public async Task BreakOnAwaitThenResumeToNextBreakpoint()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 1);
                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 3);

                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 46, 12,
                    "MoveNext");

                await StepAndCheck(StepKind.Resume, source_file, 48, 8, "MoveNext");
            });
        }

        [Fact]
        public async Task BreakOnAwaitThenResumeToNextBreakpointAfterSecondAwaitInSameMethod()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 1);
                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 5);

                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                    "dotnet://debugger-test.dll/debugger-async-step.cs", 52, 12,
                    "MoveNext");

                await StepAndCheck(StepKind.Resume, source_file, 56, 12, "MoveNext");
            });
        }
    }
}
