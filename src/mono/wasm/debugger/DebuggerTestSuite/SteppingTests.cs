// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class SteppingTests : DebuggerTests
    {
        public SteppingTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        [Fact]
        public async Task TrivalStepping()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd",
                wait_for_event_fn: (pause_location) =>
                {
                    //make sure we're on the right bp
                    Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

                    var top_frame = pause_location["callFrames"][0];
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);
                    return Task.CompletedTask;
                }
            );

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 11, 8, "Math.IntAdd",
                wait_for_event_fn: (pause_location) =>
                {
                    var top_frame = pause_location["callFrames"][0];
                    CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 8, 4, scripts, top_frame["functionLocation"]);
                    return Task.CompletedTask;
                }
            );
        }

        [Fact]
        public async Task InspectLocalsDuringStepping()
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
            await SetBreakpoint(debugger_test_loc, 10, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                debugger_test_loc, 10, 8, "Math.IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 30);
                    CheckNumber(locals, "d", 0);
                    CheckNumber(locals, "e", 0);
                    await Task.CompletedTask;
                }
            );

            await StepAndCheck(StepKind.Over, debugger_test_loc, 11, 8, "Math.IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 30);
                    CheckNumber(locals, "d", 50);
                    CheckNumber(locals, "e", 0);
                    await Task.CompletedTask;
                }
            );

            //step and get locals
            await StepAndCheck(StepKind.Over, debugger_test_loc, 12, 8, "Math.IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 30);
                    CheckNumber(locals, "d", 50);
                    CheckNumber(locals, "e", 60);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInPreviousFramesDuringSteppingIn2(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            var dep_cs_loc = "dotnet://debugger-test.dll/dependency.cs";
            await SetBreakpoint(dep_cs_loc, 35, 8);

            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

            // Will stop in Complex.DoEvenMoreStuff
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_use_complex (); }, 1);",
                dep_cs_loc, 35, 8, "Simple.Complex.DoEvenMoreStuff",
                locals_fn: async (locals) =>
                {
                    Assert.Single(locals);
                    await CheckObject(locals, "this", "Simple.Complex");
                }
            );

            var props = await GetObjectOnFrame(pause_location["callFrames"][0], "this");
            Assert.Equal(4, props.Count());
            CheckNumber(props, "A", 10);
            await CheckString(props, "B", "xx");
            await CheckString(props, "c", "20_xx");

            // Check UseComplex frame
            var locals_m1 = await GetLocalsForFrame(pause_location["callFrames"][3], debugger_test_loc, 23, 8, "Math.UseComplex");
            Assert.Equal(7, locals_m1.Count());

            CheckNumber(locals_m1, "a", 10);
            CheckNumber(locals_m1, "b", 20);
            await CheckObject(locals_m1, "complex", "Simple.Complex");
            CheckNumber(locals_m1, "c", 30);
            CheckNumber(locals_m1, "d", 50);
            CheckNumber(locals_m1, "e", 60);
            CheckNumber(locals_m1, "f", 0);

            props = await GetObjectOnFrame(pause_location["callFrames"][3], "complex");
            Assert.Equal(4, props.Count());
            CheckNumber(props, "A", 10);
            await CheckString(props, "B", "xx");
            await CheckString(props, "c", "20_xx");

            pause_location = await StepAndCheck(StepKind.Over, dep_cs_loc, 25, 8, "Simple.Complex.DoStuff", times: 2);
            // Check UseComplex frame again
            locals_m1 = await GetLocalsForFrame(pause_location["callFrames"][1], debugger_test_loc, 23, 8, "Math.UseComplex");
            Assert.Equal(7, locals_m1.Count());

            CheckNumber(locals_m1, "a", 10);
            CheckNumber(locals_m1, "b", 20);
            await CheckObject(locals_m1, "complex", "Simple.Complex");
            CheckNumber(locals_m1, "c", 30);
            CheckNumber(locals_m1, "d", 50);
            CheckNumber(locals_m1, "e", 60);
            CheckNumber(locals_m1, "f", 0);

            props = await GetObjectOnFrame(pause_location["callFrames"][1], "complex");
            Assert.Equal(4, props.Count());
            CheckNumber(props, "A", 10);
            await CheckString(props, "B", "xx");
            await CheckString(props, "c", "20_xx");
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInPreviousFramesDuringSteppingIn(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
            await SetBreakpoint(debugger_test_loc, 111, 12);

            // Will stop in InnerMethod
            var wait_res = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_outer_method(); }, 1);",
                debugger_test_loc, 111, 12, "Math.NestedInMath.InnerMethod",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(4, locals.Count());
                    CheckNumber(locals, "i", 5);
                    CheckNumber(locals, "j", 24);
                    await CheckString(locals, "foo_str", "foo");
                    await CheckObject(locals, "this", "Math.NestedInMath");
                    await Task.CompletedTask;
                }
            );

            var this_props = await GetObjectOnFrame(wait_res["callFrames"][0], "this");
            Assert.Equal(2, this_props.Count());
            await CheckObject(this_props, "m", "Math");
            await CheckValueType(this_props, "SimpleStructProperty", "Math.SimpleStruct");

            var ss_props = await GetObjectOnLocals(this_props, "SimpleStructProperty");
            var dt = new DateTime(2020, 1, 2, 3, 4, 5);
            await CheckProps(ss_props, new
            {
                dt = TDateTime(dt),
                gs = TValueType("Math.GenericStruct<System.DateTime>")
            }, "ss_props");

            // Check OuterMethod frame
            var locals_m1 = await GetLocalsForFrame(wait_res["callFrames"][1], debugger_test_loc, 87, 8, "Math.OuterMethod");
            Assert.Equal(5, locals_m1.Count());
            // FIXME: Failing test CheckNumber (locals_m1, "i", 5);
            // FIXME: Failing test CheckString (locals_m1, "text", "Hello");
            CheckNumber(locals_m1, "new_i", 0);
            CheckNumber(locals_m1, "k", 0);
            await CheckObject(locals_m1, "nim", "Math.NestedInMath");

            // step back into OuterMethod
            await StepAndCheck(StepKind.Over, debugger_test_loc, 91, 8, "Math.OuterMethod", times: 6,
                locals_fn: async (locals) =>
                {
                    Assert.Equal(5, locals.Count());

                    // FIXME: Failing test CheckNumber (locals_m1, "i", 5);
                    await CheckString(locals, "text", "Hello");
                    // FIXME: Failing test CheckNumber (locals, "new_i", 24);
                    CheckNumber(locals, "k", 19);
                    await CheckObject(locals, "nim", "Math.NestedInMath");
                }
            );

            //await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 81, 2, "OuterMethod", times: 2);

            // step into InnerMethod2
            await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 96, 4, "Math.InnerMethod2",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(3, locals.Count());

                    await CheckString(locals, "s", "test string");
                    //out var: CheckNumber (locals, "k", 0);
                    CheckNumber(locals, "i", 24);
                    await Task.CompletedTask;
                }
            );

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 100, 4, "Math.InnerMethod2", times: 4,
                locals_fn: async (locals) =>
                {
                    Assert.Equal(3, locals.Count());

                    await CheckString(locals, "s", "test string");
                    // FIXME: Failing test CheckNumber (locals, "k", 34);
                    CheckNumber(locals, "i", 24);
                    await Task.CompletedTask;
                }
            );

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 92, 8, "Math.OuterMethod", times: 1,
                locals_fn: async (locals) =>
                {
                    Assert.Equal(5, locals.Count());

                    await CheckString(locals, "text", "Hello");
                    // FIXME: failing test CheckNumber (locals, "i", 5);
                    CheckNumber(locals, "new_i", 22);
                    CheckNumber(locals, "k", 34);
                    await CheckObject(locals, "nim", "Math.NestedInMath");
                }
            );
        }

        [Fact]
        public async Task InspectLocalsDuringSteppingIn()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 86, 8);

            await EvaluateAndCheck("window.setTimeout(function() { invoke_outer_method(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 86, 8, "Math.OuterMethod",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(5, locals.Count());

                    await CheckObject(locals, "nim", "Math.NestedInMath");
                    CheckNumber(locals, "i", 5);
                    CheckNumber(locals, "k", 0);
                    CheckNumber(locals, "new_i", 0);
                    await CheckString(locals, "text", null);
                }
            );

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 87, 8, "Math.OuterMethod",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(5, locals.Count());

                    await CheckObject(locals, "nim", "Math.NestedInMath");
                    // FIXME: Failing test CheckNumber (locals, "i", 5);
                    CheckNumber(locals, "k", 0);
                    CheckNumber(locals, "new_i", 0);
                    await CheckString(locals, "text", "Hello");
                    await Task.CompletedTask;
                }
            );

            // Step into InnerMethod
            await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 105, 8, "Math.NestedInMath.InnerMethod");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 110, 12, "Math.NestedInMath.InnerMethod", times: 5,
                locals_fn: async (locals) =>
                {
                    Assert.Equal(4, locals.Count());

                    CheckNumber(locals, "i", 5);
                    CheckNumber(locals, "j", 15);
                    await CheckString(locals, "foo_str", "foo");
                    await CheckObject(locals, "this", "Math.NestedInMath");
                    await Task.CompletedTask;
                }
            );

            // Step back to OuterMethod
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 90, 8, "Math.OuterMethod", times: 6,
                locals_fn: async (locals) =>
                {
                    Assert.Equal(5, locals.Count());

                    await CheckObject(locals, "nim", "Math.NestedInMath");
                    // FIXME: Failing test CheckNumber (locals, "i", 5);
                    CheckNumber(locals, "k", 0);
                    CheckNumber(locals, "new_i", 24);
                    await CheckString(locals, "text", "Hello");
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsInAsyncMethods(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

            await SetBreakpoint(debugger_test_loc, 120, 12);
            await SetBreakpoint(debugger_test_loc, 135, 12);

            // Will stop in Asyncmethod0
            var wait_res = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_async_method_with_await(); }, 1);",
                debugger_test_loc, 120, 12, "Math.NestedInMath.AsyncMethod0",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(4, locals.Count());
                    await CheckString(locals, "s", "string from js");
                    CheckNumber(locals, "i", 42);
                    await CheckString(locals, "local0", "value0");
                    await CheckObject(locals, "this", "Math.NestedInMath");
                }
            );
            _testOutput.WriteLine(wait_res.ToString());

#if false // Disabled for now, as we don't have proper async traces
            var locals = await GetProperties(wait_res["callFrames"][2]["callFrameId"].Value<string>());
            Assert.Equal(4, locals.Count());
            await CheckString(locals, "ls", "string from jstest").ConfigureAwait(false);
            CheckNumber(locals, "li", 52);
#endif

            // TODO: previous frames have async machinery details, so no point checking that right now

            var pause_loc = await SendCommandAndCheck(null, "Debugger.resume", debugger_test_loc, 135, 12, "Math.NestedInMath.AsyncMethodNoReturn",
                locals_fn: async (locals) =>
                {
                    Assert.Equal(4, locals.Count());
                    await CheckString(locals, "str", "AsyncMethodNoReturn's local");
                    await CheckObject(locals, "this", "Math.NestedInMath");
                    //FIXME: check fields
                    await CheckValueType(locals, "ss", "Math.SimpleStruct");
                    await CheckArray(locals, "ss_arr", "Math.SimpleStruct[]", "Math.SimpleStruct[0]");
                    // TODO: struct fields
                    await Task.CompletedTask;
                }
            );

            var this_props = await GetObjectOnFrame(pause_loc["callFrames"][0], "this");
            Assert.Equal(2, this_props.Count());
            await CheckObject(this_props, "m", "Math");
            await CheckValueType(this_props, "SimpleStructProperty", "Math.SimpleStruct");

            // TODO: Check `this` properties
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeMethodArgsWhileStepping(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            await SetBreakpoint(debugger_test_loc, 36, 12);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:TestStructsAsMethodArgs'); }, 1);",
                debugger_test_loc, 36, 12, "DebuggerTests.ValueTypesTest.MethodWithStructArgs");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            {
                Assert.Equal(3, locals.Count());
                await CheckString(locals, "label", "TestStructsAsMethodArgs#label");
                await CheckValueType(locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
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
                List = TObject("System.Collections.Generic.List<System.DateTime>", description: "Count = 1"),
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

            pause_location = await StepAndCheck(StepKind.Over, debugger_test_loc, 40, 8, "DebuggerTests.ValueTypesTest.MethodWithStructArgs", times: 4,
                locals_fn: async (l) => { /* non-null to make sure that locals get fetched */ await Task.CompletedTask;  });
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            {
                Assert.Equal(3, locals.Count());

                await CheckString(locals, "label", "TestStructsAsMethodArgs#label");
                await CheckValueType(locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
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
                    List = TObject("System.Collections.Generic.List<System.DateTime>", description: "Count = 1"),
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
                await CheckString(gs_props, "StringField", "ss_local#SimpleStruct#string#0#SimpleStruct#gs#StringField");
                await CheckObject(gs_props, "List", "System.Collections.Generic.List<System.DateTime>", description: "Count = 1");
            }

            // ----------- Step back to the caller ---------

            pause_location = await StepAndCheck(StepKind.Over, debugger_test_loc, 30, 12, "DebuggerTests.ValueTypesTest.TestStructsAsMethodArgs",
                times: 1, locals_fn: async (l) => { /* non-null to make sure that locals get fetched */ await Task.CompletedTask;  });
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
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CheckUpdatedValueTypeFieldsOnResume()
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            var lines = new[] { 205, 208 };
            await SetBreakpoint(debugger_test_loc, lines[0], 12);
            await SetBreakpoint(debugger_test_loc, lines[1], 12);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeMembers'); }, 1);",
                debugger_test_loc, lines[0], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingValueTypeMembers");

            await CheckLocals(pause_location, new DateTime(1, 2, 3, 4, 5, 6), new DateTime(4, 5, 6, 7, 8, 9));

            // Resume
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingValueTypeMembers");
            await CheckLocals(pause_location, new DateTime(9, 8, 7, 6, 5, 4), new DateTime(5, 1, 3, 7, 9, 10));

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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CheckUpdatedValueTypeLocalsOnResumeAsync()
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            var lines = new[] { 214, 216 };
            await SetBreakpoint(debugger_test_loc, lines[0], 12);
            await SetBreakpoint(debugger_test_loc, lines[1], 12);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeLocalsAsync'); }, 1);",
                debugger_test_loc, lines[0], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingValueTypeLocalsAsync");

            var dt = new DateTime(1, 2, 3, 4, 5, 6);
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckDateTime(locals, "dt", dt);

            // Resume
            dt = new DateTime(9, 8, 7, 6, 5, 4);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingValueTypeLocalsAsync");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckDateTime(locals, "dt", dt);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CheckUpdatedVTArrayMembersOnResume()
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            var lines = new[] { 225, 227 };
            await SetBreakpoint(debugger_test_loc, lines[0], 12);
            await SetBreakpoint(debugger_test_loc, lines[1], 12);

            var dt = new DateTime(1, 2, 3, 4, 5, 6);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingVTArrayMembers'); }, 1);",
                debugger_test_loc, lines[0], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingVTArrayMembers");
            await CheckArrayElements(pause_location, dt);

            // Resume
            dt = new DateTime(9, 8, 7, 6, 5, 4);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", debugger_test_loc, lines[1], 12, "DebuggerTests.ValueTypesTest.MethodUpdatingVTArrayMembers");
            await CheckArrayElements(pause_location, dt);

            async Task CheckArrayElements(JToken pause_location, DateTime dt)
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                await CheckProps(locals, new
                {
                    ssta = TArray("DebuggerTests.StructForToStringTests[]", "DebuggerTests.StructForToStringTests[1]")
                }, "locals");

                var ssta = await GetObjectOnLocals(locals, "ssta");
                var sst0 = await GetObjectOnLocals(ssta, "0");
                await CheckProps(sst0, new
                {
                    DT = TDateTime(dt)
                }, "dta [0]", num_fields: 5);
            }
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task SteppingIntoMscorlib()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 83, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:OuterMethod'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 83, 8,
                "Math.OuterMethod");

            //make sure we're on the right bp
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            pause_location = await SendCommandAndCheck(null, $"Debugger.stepInto", null, -1, -1, null);
            var top_frame = pause_location["callFrames"][0];

            AssertEqual("System.Console.WriteLine", top_frame["functionName"]?.Value<string>(), "Expected to be in WriteLine method");
            var script_id = top_frame["functionLocation"]["scriptId"].Value<string>();
            Assert.Matches("^dotnet://(mscorlib|System\\.Console)\\.dll/Console.cs", scripts[script_id]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpointAndHitAndRemoveAndDontHit()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); invoke_add()}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd");

            Assert.Equal("other", pause_location["reason"]?.Value<string>());
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "Math.IntAdd");
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "Math.IntAdd");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpointAndHitAndRemoveTwice()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); invoke_add()}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd");

            Assert.Equal("other", pause_location["reason"]?.Value<string>());
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
            await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CreateGoodBreakpointAndHitAndRemoveAndDontHitAndCreateAgainAndHit()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            var bp2 = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 12, 8);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); invoke_add(); invoke_add(); invoke_add()}, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8,
                "Math.IntAdd");

            Assert.Equal("other", pause_location["reason"]?.Value<string>());
            Assert.Equal(bp.Value["breakpointId"]?.ToString(), pause_location["hitBreakpoints"]?[0]?.Value<string>());

            await RemoveBreakpoint(bp.Value["breakpointId"]?.ToString());
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "Math.IntAdd");
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "Math.IntAdd");
            bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "Math.IntAdd");
        }

        // [ConditionalFact(nameof(RunningOnChrome))]
        //https://github.com/dotnet/runtime/issues/42421
        public async Task BreakAfterAwaitThenStepOverTillBackToCaller()
        {
            var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                "MoveNext");

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 23, 12, "MoveNext");

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 24, 8, "MoveNext");

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-step.cs", 15, 12, "MoveNext");
        }

        // [ConditionalFact(nameof(RunningOnChrome))]
        //[ActiveIssue("https://github.com/dotnet/runtime/issues/42421")]
        public async Task StepOutOfAsyncMethod()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                "MoveNext");

            await StepAndCheck(StepKind.Out, source_file, 15, 4, "TestAsyncStepOut");
        }

        [Fact]
        public async Task ResumeOutOfAsyncMethodToAsyncCallerWithBreakpoint()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut2", 2);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:TestAsyncStepOut'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 21, 12,
                "DebuggerTests.AsyncStepClass.TestAsyncStepOut2");

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "TestAsyncStepOut", 2);
            await SendCommandAndCheck(null, "Debugger.resume", source_file, 16, 8, "DebuggerTests.AsyncStepClass.TestAsyncStepOut");
        }

        [Fact]
        public async Task StepOutOfNonAsyncMethod()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "OtherMethod0", 1);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:SimpleMethod'); }, 1);",
                source_file, -1, -1,
                "DebuggerTests.AsyncStepClass.OtherMethod0");

            await StepAndCheck(StepKind.Out, source_file, 29, 12, "DebuggerTests.AsyncStepClass.SimpleMethod");
        }

        [Fact]
        public async Task BreakOnAwaitThenStepOverToNextAwaitCall()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 2);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 53, 12,
                "DebuggerTests.AsyncStepClass.MethodWithTwoAwaitsAsync");

            await StepAndCheck(StepKind.Over, source_file, 54, 12, "DebuggerTests.AsyncStepClass.MethodWithTwoAwaitsAsync");
        }

        [Fact]
        public async Task BreakOnAwaitThenStepOverToNextLine()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 1);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 46, 12,
                "DebuggerTests.AsyncStepClass.StepOverTestAsync");

            // BUG: chrome: not able to show any bp line indicator
            await StepAndCheck(StepKind.Over, source_file, 47, 12, "DebuggerTests.AsyncStepClass.StepOverTestAsync");
        }

        [Fact]
        public async Task BreakOnAwaitThenResumeToNextBreakpoint()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 1);
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "StepOverTestAsync", 3);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 46, 12,
                "DebuggerTests.AsyncStepClass.StepOverTestAsync");

            await StepAndCheck(StepKind.Resume, source_file, 48, 8, "DebuggerTests.AsyncStepClass.StepOverTestAsync");
        }

        [Fact]
        public async Task BreakOnAwaitThenResumeToNextBreakpointAfterSecondAwaitInSameMethod()
        {
            string source_file = "dotnet://debugger-test.dll/debugger-async-step.cs";

            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 1);
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.AsyncStepClass", "MethodWithTwoAwaitsAsync", 5);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async('[debugger-test] DebuggerTests.AsyncStepClass:StepOverTestAsync'); }, 1);",
                "dotnet://debugger-test.dll/debugger-async-step.cs", 52, 12,
                "DebuggerTests.AsyncStepClass.MethodWithTwoAwaitsAsync");

            await StepAndCheck(StepKind.Resume, source_file, 56, 12, "DebuggerTests.AsyncStepClass.MethodWithTwoAwaitsAsync");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BreakOnMethodCalledFromHiddenLine()
        {
            await SetBreakpointInMethod("debugger-test.dll", "HiddenSequencePointTest", "StepOverHiddenSP2", 0);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] HiddenSequencePointTest:StepOverHiddenSP'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 546, 4,
                "HiddenSequencePointTest.StepOverHiddenSP2");

            // Check previous frame
            var top_frame = pause_location["callFrames"][1];
            Assert.Equal("HiddenSequencePointTest.StepOverHiddenSP", top_frame["functionName"].Value<string>());
            Assert.Contains("debugger-test.cs", top_frame["url"].Value<string>());

            CheckLocation("dotnet://debugger-test.dll/debugger-test.cs", 537, 8, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task StepOverHiddenLinesShouldResumeAtNextAvailableLineInTheMethod()
        {
            string source_loc = "dotnet://debugger-test.dll/debugger-test.cs";
            await SetBreakpoint(source_loc, 537, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] HiddenSequencePointTest:StepOverHiddenSP'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 537, 8,
                "HiddenSequencePointTest.StepOverHiddenSP");

            await StepAndCheck(StepKind.Over, source_loc, 542, 8, "HiddenSequencePointTest.StepOverHiddenSP");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        async Task StepOverHiddenLinesInMethodWithNoNextAvailableLineShouldResumeAtCallSite()
        {
            string source_loc = "dotnet://debugger-test.dll/debugger-test.cs";
            await SetBreakpoint(source_loc, 552, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] HiddenSequencePointTest:StepOverHiddenSP'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 552, 8,
                "HiddenSequencePointTest.MethodWithHiddenLinesAtTheEnd");

            await StepAndCheck(StepKind.Over, source_loc, 544, 4, "HiddenSequencePointTest.StepOverHiddenSP");
        }

        // [ConditionalFact(nameof(RunningOnChrome))]
        // Issue: https://github.com/dotnet/runtime/issues/42704
        async Task BreakpointOnHiddenLineShouldStopAtEarliestNextAvailableLine()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 539, 8);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] HiddenSequencePointTest:StepOverHiddenSP'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 546, 4,
                "StepOverHiddenSP2");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BreakpointOnHiddenLineOfMethodWithNoNextVisibleLineShouldNotPause()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 554, 12);

            string expression = "window.setTimeout(function() { invoke_static_method ('[debugger-test] HiddenSequencePointTest:StepOverHiddenSP'); }, 1);";
            await cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression }), token);

            Task pause_task = insp.WaitFor(Inspector.PAUSE);
            Task t = await Task.WhenAny(pause_task, Task.Delay(2000));
            Assert.True(t != pause_task, "Debugger unexpectedly paused");
        }

        [Fact]
        public async Task SimpleStep_RegressionTest_49141()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 674, 0);

            string expression = "window.setTimeout(function() { invoke_static_method ('[debugger-test] Foo:RunBart'); }, 1);";
            await EvaluateAndCheck(
                expression,
                "dotnet://debugger-test.dll/debugger-test.cs", 674, 12,
                "Foo.Bart");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 677, 8, "Foo.Bart");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 678, 4, "Foo.Bart");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task StepAndEvaluateExpression()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 682, 0);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Foo:RunBart'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 682, 8,
                "Foo.RunBart");
            var pause_location = await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 671, 4, "Foo.Bart");
            var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
            await EvaluateOnCallFrameAndCheck(id, ("this.Bar", TString("Same of something")));
            pause_location = await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 673, 8, "Foo.Bart");
            id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
            await EvaluateOnCallFrameAndCheck(id, ("this.Bar", TString("Same of something")));
        }

        [Fact]
        public async Task StepOverWithMoreThanOneCommandInSameLine()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 693, 0);

            string expression = "window.setTimeout(function() { invoke_static_method ('[debugger-test] Foo:RunBart'); }, 1);";
            await EvaluateAndCheck(
                expression,
                "dotnet://debugger-test.dll/debugger-test.cs", 693, 8,
                "Foo.OtherBar");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 694, 8, "Foo.OtherBar");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 696, 8, "Foo.OtherBar");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 699, 8, "Foo.OtherBar");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 701, 8, "Foo.OtherBar");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 702, 4, "Foo.OtherBar");
        }

        [Fact]
        public async Task StepOverWithMoreThanOneCommandInSameLineAsync()
        {
            await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 710, 0);

            string expression = "window.setTimeout(function() { invoke_static_method ('[debugger-test] Foo:RunBart'); }, 1);";
            await EvaluateAndCheck(
                expression,
                "dotnet://debugger-test.dll/debugger-test.cs", 710, 8,
                "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 711, 8, "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 713, 8, "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 716, 8, "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 718, 8, "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 719, 8, "Foo.OtherBarAsync");
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 720, 4, "Foo.OtherBarAsync");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CheckResetFrameNumberForEachStep()
        {
            var bp_conditional = await SetBreakpointInMethod("debugger-test.dll", "SteppingInto", "MethodToStep", 1);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] SteppingInto:MethodToStep'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp_conditional.Value["locations"][0]["lineNumber"].Value<int>(),
                bp_conditional.Value["locations"][0]["columnNumber"].Value<int>(),
                "SteppingInto.MethodToStep"
            );
            var pause_location = await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 799, 4, "MyIncrementer.Increment");
            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 800, 8, "MyIncrementer.Increment");
            Assert.Equal(pause_location["callFrames"][0]["callFrameId"], "dotnet:scope:1");
            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 801, 8, "MyIncrementer.Increment");
            Assert.Equal(pause_location["callFrames"][0]["callFrameId"], "dotnet:scope:1");
            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 806, 8, "MyIncrementer.Increment");
            Assert.Equal(pause_location["callFrames"][0]["callFrameId"], "dotnet:scope:1");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebuggerHiddenIgnoreStepInto()
        {
            var pause_location = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", "RunDebuggerHidden", 1);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerAttribute:RunDebuggerHidden'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                pause_location.Value["locations"][0]["lineNumber"].Value<int>(),
                pause_location.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute.RunDebuggerHidden"
            );
            var step_into = await SendCommandAndCheck(null, $"Debugger.stepInto", null, -1, -1, null);
            Assert.Equal(
                step_into["callFrames"][0]["location"]["lineNumber"].Value<int>(),
                pause_location.Value["locations"][0]["lineNumber"].Value<int>() + 1
                );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("Debugger.stepInto")]
        [InlineData("Debugger.stepOver")]
        public async Task DebuggerHiddenIgnoreStepUserBreakpoint(string steppingFunction)
        {
            var pause_location = await SetBreakpointInMethod("debugger-test.dll", "DebuggerAttribute", "RunDebuggerHidden", 1);
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerAttribute:RunDebuggerHidden'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                pause_location.Value["locations"][0]["lineNumber"].Value<int>(),
                pause_location.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerAttribute.RunDebuggerHidden"
            );
            // stepOver HiddenMethod:
            var step_into1 = await SendCommandAndCheck(null, steppingFunction, null, -1, -1, null);
            Assert.Equal(
                pause_location.Value["locations"][0]["lineNumber"].Value<int>() + 1,
                step_into1["callFrames"][0]["location"]["lineNumber"].Value<int>()
                );

            // freeze on HiddenMethodUserBreak:
            var step_into2 = await SendCommandAndCheck(null, steppingFunction, null, -1, -1, null);
            Assert.Equal(
                pause_location.Value["locations"][0]["lineNumber"].Value<int>() + 1,
                step_into2["callFrames"][0]["location"]["lineNumber"].Value<int>()
                );
        }
    }
}
