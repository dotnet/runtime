// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace DebuggerTests
{

    public class MiscTests : DebuggerTests
    {
        public MiscTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        [Fact]
        public void CheckThatAllSourcesAreSent()
        {
            Assert.Contains("dotnet://debugger-test.dll/debugger-test.cs", scripts.Values);
            Assert.Contains("dotnet://debugger-test.dll/debugger-test2.cs", scripts.Values);
            Assert.Contains("dotnet://debugger-test.dll/dependency.cs", scripts.Values);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionThrownInJS()
        {
            var eval_req = JObject.FromObject(new
            {
                expression = "invoke_bad_js_test();"
            });

            var eval_res = await cli.SendCommand("Runtime.evaluate", eval_req, token);
            Assert.False(eval_res.IsOk);
            Assert.Equal("Uncaught", eval_res.Error["exceptionDetails"]?["text"]?.Value<string>());
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionThrownInJSOutOfBand()
        {
            await SetBreakpoint("/debugger-driver.html", 27, 2);

            var eval_req = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_bad_js_test(); }, 1);",
            });

            var task = insp.WaitFor("Runtime.exceptionThrown");
            var eval_res = await cli.SendCommand("Runtime.evaluate", eval_req, token);
            // Response here will be the id for the timer from JS!
            Assert.True(eval_res.IsOk);

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await task);
            var ex_json = JObject.Parse(ex.Message);
            Assert.Equal(dicFileToUrl["/debugger-driver.html"], ex_json["exceptionDetails"]?["url"]?.Value<string>());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsAtBreakpointSite(bool use_cfo) =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "Math.IntAdd",
                "window.setTimeout(function() { invoke_add(); }, 1);",
                use_cfo: use_cfo,
                test_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 30);
                    CheckNumber(locals, "d", 0);
                    CheckNumber(locals, "e", 0);
                    await Task.CompletedTask;
                }
            );

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task InspectPrimitiveTypeLocalsAtBreakpointSite() =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 154, 8, "Math.PrimitiveTypesTest",
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:PrimitiveTypesTest'); }, 1);",
                test_fn: async (locals) =>
                {
                    await CheckSymbol(locals, "c0", '€');
                    await CheckSymbol(locals, "c1", 'A');
                    await Task.CompletedTask;
                }
            );

        [Fact]
        public async Task InspectLocalsTypesAtBreakpointSite() =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test2.cs", 50, 8, "Fancy.Types",
                "window.setTimeout(function() { invoke_static_method (\"[debugger-test] Fancy:Types\")(); }, 1);",
                use_cfo: false,
                test_fn: async (locals) =>
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
                    await Task.CompletedTask;
                }
            );

        [Fact]
        public async Task InspectSimpleStringLocals() =>
            await CheckInspectLocalsAtBreakpointSite(
                "Math", "TestSimpleStrings", 13, "Math.TestSimpleStrings",
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

                        strings = TArray("string[]", "string[4]")
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

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("TestNullableLocal", false)]
        [InlineData("TestNullableLocalAsync", true)]
        public async Task InspectNullableLocals(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.NullableTests",
            method_name,
            10,
            $"DebuggerTests.NullableTests.{method_name}",
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.NullableTests:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                await CheckProps(locals, new
                {
                    n_int = TNumber(5),
                    n_int_null = TObject("System.Nullable<int>", null),

                    n_dt = TDateTime(dt),
                    n_dt_null = TObject("System.Nullable<System.DateTime>", null),

                    n_gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    n_gs_null = TObject("System.Nullable<DebuggerTests.ValueTypesTest.GenericStruct<int>>", null),
                }, "locals");

                // check gs

                var n_gs = GetAndAssertObjectWithName(locals, "n_gs");
                var n_gs_props = await GetProperties(n_gs["value"]?["objectId"]?.Value<string>());
                await CheckProps(n_gs_props, new
                {
                    List = TObject("System.Collections.Generic.List<int>", is_null: true),
                    StringField = TString("n_gs#StringField"),
                    Options = TEnum("DebuggerTests.Options", "None")
                }, nameof(n_gs));
            });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithGenericTypesAtBreakpointSite(bool use_cfo) =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", 74, 8, "Math.GenericTypesTest",
                "window.setTimeout(function() { invoke_generic_types_test (); }, 1);",
                use_cfo: use_cfo,
                test_fn: async (locals) =>
                {
                    await CheckObject(locals, "list", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", description: "Count = 0");
                    await CheckObject(locals, "list_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

                    await CheckArray(locals, "list_arr", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[1]");
                    await CheckObject(locals, "list_arr_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);

                    // Unused locals
                    await CheckObject(locals, "list_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", description: "Count = 0");
                    await CheckObject(locals, "list_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

                    await CheckArray(locals, "list_arr_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[1]");
                    await CheckObject(locals, "list_arr_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);
                    await Task.CompletedTask;
                }
            );

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task RuntimeGetPropertiesWithInvalidScopeIdTest()
        {
            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 49, 8);

            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_delegates_test (); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 49, 8,
                "Math.DelegatesTest",
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
                   Assert.False(frame_props.IsOk);
               }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithStructs(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            await SetBreakpoint(debugger_test_loc, 24, 8);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_method_with_structs(); }, 1);",
                debugger_test_loc, 24, 8, "DebuggerTests.ValueTypesTest.MethodWithLocalStructs");

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

            await CheckString(vt_local_props, "StringField", "string#0");
            await CheckValueType(vt_local_props, "SimpleStructField", "DebuggerTests.ValueTypesTest.SimpleStruct");
            await CheckValueType(vt_local_props, "SimpleStructProperty", "DebuggerTests.ValueTypesTest.SimpleStruct");
            await CheckDateTime(vt_local_props, "DT", new DateTime(2020, 1, 2, 3, 4, 5));
            await CheckEnum(vt_local_props, "RGB", "DebuggerTests.RGB", "Blue");

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
                await CheckString(gs_props, "StringField", "set in MethodWithLocalStructs#SimpleStruct#gs#StringField");
                await CheckObject(gs_props, "List", "System.Collections.Generic.List<System.DateTime>", description: "Count = 1");
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
        }

        [Theory]
        [InlineData("BoxingTest", false)]
        [InlineData("BoxingTestAsync", true)]
        public async Task InspectBoxedLocals(string method_name, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTest",
            method_name,
            17,
            $"DebuggerTest.{method_name}",
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTest:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);
                await CheckProps(locals, new
                {
                    n_i = TNumber(5),
                    o_i = TNumber(5),
                    o_n_i = TNumber(5),
                    o_s = TString("foobar"),
                    o_obj = TObject("Math"),

                    n_gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    o_gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
                    o_n_gs = TValueType("DebuggerTests.ValueTypesTest.GenericStruct<int>"),

                    n_dt = TDateTime(dt),
                    o_dt = TDateTime(dt),
                    o_n_dt = TDateTime(dt),

                    o_null = TObject("object", is_null: true),
                    o_ia = TArray("int[]", "int[2]"),
                }, "locals");

                foreach (var name in new[] { "n_gs", "o_gs", "o_n_gs" })
                {
                    var gs = GetAndAssertObjectWithName(locals, name);
                    var gs_props = await GetProperties(gs["value"]?["objectId"]?.Value<string>());
                    await CheckProps(gs_props, new
                    {
                        List = TObject("System.Collections.Generic.List<int>", is_null: true),
                        StringField = TString("n_gs#StringField"),
                        Options = TEnum("DebuggerTests.Options", "None")
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
            $"DebuggerTest.{method_name}",
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
            $"DebuggerTest.{method_name}",
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTest:{method_name}'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var dt = new DateTime(2310, 1, 2, 3, 4, 5);

                await CheckProps(locals, new
                {
                    vt_dt = TDateTime(new DateTime(4819, 5, 6, 7, 8, 9)),
                    vt_gs = TValueType("Math.GenericStruct<string>"),
                    e = TEnum("System.IO.FileMode", "0"),
                    ee = TEnum("System.IO.FileMode", "Append")
                }, "locals");
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsWithStructsStaticAsync(bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            await SetBreakpoint(debugger_test_loc, 54, 12);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method_async (" +
                "'[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalStructsStaticAsync'" +
                "); }, 1);",
                debugger_test_loc, 54, 12, "DebuggerTests.ValueTypesTest.MethodWithLocalStructsStaticAsync");

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
                        List = TObject("System.Collections.Generic.List<System.DateTime>", description: "Count = 1"),
                        Options = TEnum("DebuggerTests.Options", "Option1")
                    }
                );
            }

            // Check gs_local's properties
            var gs_local_props = await GetObjectOnFrame(pause_location["callFrames"][0], "gs_local");
            await CheckProps(gs_local_props, new
            {
                StringField = TString("gs_local#GenericStruct<ValueTypesTest>#StringField"),
                List = TObject("System.Collections.Generic.List<int>", description: "Count = 2"),
                Options = TEnum("DebuggerTests.Options", "Option2")
            }, "gs_local");

            // FIXME: check ss_local.gs.List's members
        }

        [Theory]
        [InlineData(137, 12, "MethodWithLocalsForToStringTest", false, false)]
        /*[InlineData(147, 12, "MethodWithArgumentsForToStringTest", true, false)]
        [InlineData(192, 12, "MethodWithArgumentsForToStringTestAsync", true, true)]
        [InlineData(182, 12, "MethodWithArgumentsForToStringTestAsync", false, true)]*/
        public async Task InspectLocalsForToStringDescriptions(int line, int col, string method_name, bool call_other, bool invoke_async)
        {
            string entry_method_name = $"[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalsForToStringTest{(invoke_async ? "Async" : String.Empty)}";
            int frame_idx = 0;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() {" +
                (invoke_async ? "invoke_static_method_async (" : "invoke_static_method (") +
                $"'{entry_method_name}'," +
                (call_other ? "true" : "false") +
                "); }, 1);";
            _testOutput.WriteLine($"{eval_expr}");

            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, $"DebuggerTests.ValueTypesTest.{method_name}{(invoke_async ? "Async" : String.Empty)}");

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
                dts = TArray("System.DateTime[]", "System.DateTime[2]"),
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
                }, "ts_props", skip_num_fields_check: true);

            // DateTimeOffset
            await CompareObjectPropertiesFor(frame_locals, "dto",
                new
                {
                    Day = TNumber(2),
                    Year = TNumber(2020),
                    DayOfWeek = TEnum("System.DayOfWeek", "Thursday")
                }, "dto_props", skip_num_fields_check: true);

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
        }

        [Fact]
        public async Task InspectLocals()
        {
            var wait_res = await RunUntil("locals_inner");
            var locals = await GetProperties(wait_res["callFrames"][1]["callFrameId"].Value<string>());
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectLocalsForStructInstanceMethod(bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-array-test.cs", 258, 12,
            "DebuggerTests.Point.GenericInstanceMethod<DebuggerTests.SimpleClass>",
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
        public async Task MulticastDelegateTest() => await CheckInspectLocalsAtBreakpointSite(
            "MulticastDelegateTestClass", "Test", 5, "MulticastDelegateTestClass.Test",
            "window.setTimeout(function() { invoke_static_method('[debugger-test] MulticastDelegateTestClass:run'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var this_props = await GetObjectOnLocals(frame_locals, "this");
                await CheckProps(this_props, new
                {
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
            $"{type_name}.StaticMethodWithNoLocals{ (is_async ? "Async" : "") }",
            $"window.setTimeout(function() {{ invoke_static_method('[debugger-test] {type_name}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                AssertEqual(0, frame_locals.Values<JToken>().Count(), "locals");
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StaticMethodWithLocalEmptyStructThatWillGetExpanded(bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            "EmptyStruct",
            $"StaticMethodWithLocalEmptyStruct{ (is_async ? "Async" : "") }",
            1,
            $"EmptyStruct.StaticMethodWithLocalEmptyStruct{ (is_async ? "Async" : "") }",
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
        public async Task PreviousFrameForAReflectedCall() => await CheckInspectLocalsAtBreakpointSite(
             "DebuggerTests.GetPropertiesTests.CloneableStruct", "SimpleStaticMethod", 1, "DebuggerTests.GetPropertiesTests.CloneableStruct.SimpleStaticMethod",
             "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.GetPropertiesTests.TestWithReflection:run'); })",
             wait_for_event_fn: async (pause_location) =>
             {
                 var frame = FindFrame(pause_location, "DebuggerTests.GetPropertiesTests.TestWithReflection.InvokeReflectedStaticMethod");
                 Assert.NotNull(frame);

                 var frame_locals = await GetProperties(frame["callFrameId"].Value<string>());

                 await CheckProps(frame_locals, new
                 {
                     mi = TObject("System.Reflection.RuntimeMethodInfo"), //this is what is returned when debugging desktop apps using VS
                     dt = TDateTime(new DateTime(4210, 3, 4, 5, 6, 7)),
                     i = TNumber(4),
                     strings = TArray("string[]", "string[1]"),
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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugLazyLoadedAssemblyWithPdb()
        {
            Task<JObject> bpResolved = WaitForBreakpointResolvedEvent();
            int line = 9;
            await SetBreakpoint(".*/lazy-debugger-test.cs$", line, 0, use_regex: true);
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.dll"),
                    Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.pdb"));

            var source_location = "dotnet://lazy-debugger-test.dll/lazy-debugger-test.cs";
            Assert.Contains(source_location, scripts.Values);

            await bpResolved;

            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function () { invoke_static_method('[lazy-debugger-test] LazyMath:IntAdd', 5, 10); }, 1);",
               source_location, line, 8,
               "LazyMath.IntAdd");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 5);
            CheckNumber(locals, "b", 10);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugLazyLoadedAssemblyWithEmbeddedPdb()
        {
            Task<JObject> bpResolved = WaitForBreakpointResolvedEvent();
            int line = 9;
            await SetBreakpoint(".*/lazy-debugger-test-embedded.cs$", line, 0, use_regex: true);
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "lazy-debugger-test-embedded.dll"),
                    null);

            var source_location = "dotnet://lazy-debugger-test-embedded.dll/lazy-debugger-test-embedded.cs";
            Assert.Contains(source_location, scripts.Values);

            await bpResolved;

            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function () { invoke_static_method('[lazy-debugger-test-embedded] LazyMath:IntAdd', 5, 10); }, 1);",
               source_location, line, 8,
               "LazyMath.IntAdd");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 5);
            CheckNumber(locals, "b", 10);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugLazyLoadedAssemblyWithEmbeddedPdbALC()
        {
            int line = 9;
            await SetBreakpoint(".*/lazy-debugger-test-embedded.cs$", line, 0, use_regex: true);
            var pause_location = await LoadAssemblyDynamicallyALCAndRunMethod(
                    Path.Combine(DebuggerTestAppPath, "lazy-debugger-test-embedded.dll"),
                    null, "LazyMath", "IntAdd");

            var source_location = "dotnet://lazy-debugger-test-embedded.dll/lazy-debugger-test-embedded.cs";
            Assert.Contains(source_location, scripts.Values);

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 5);
            CheckNumber(locals, "b", 10);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task CannotDebugLazyLoadedAssemblyWithoutPdb()
        {
            int line = 9;
            await SetBreakpoint(".*/lazy-debugger-test.cs$", line, 0, use_regex: true);
            await LoadAssemblyDynamically(
                    Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.dll"),
                    null);

            // wait to bit to catch if the event might be raised a bit late
            await Task.Delay(1000);

            var source_location = "dotnet://lazy-debugger-test.dll/lazy-debugger-test.cs";
            Assert.DoesNotContain(source_location, scripts.Values);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task GetSourceUsingSourceLink()
        {
            var bp = await SetBreakpointInMethod("debugger-test-with-source-link.dll", "DebuggerTests.ClassToBreak", "TestBreakpoint", 0);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test-with-source-link] DebuggerTests.ClassToBreak:TestBreakpoint'); }, 1);",
                "dotnet://debugger-test-with-source-link.dll/test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "DebuggerTests.ClassToBreak.TestBreakpoint");

            var sourceToGet = JObject.FromObject(new
            {
                scriptId = pause_location["callFrames"][0]["functionLocation"]["scriptId"].Value<string>()
            });

            var source = await cli.SendCommand("Debugger.getScriptSource", sourceToGet, token);
            Assert.True(source.IsOk, $"Failed to getScriptSource: {source}");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task GetSourceEmbeddedSource()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp = await SetBreakpoint(".*/MethodBody1.cs$", 48, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1");

            var sourceToGet = JObject.FromObject(new
            {
                scriptId = pause_location["callFrames"][0]["functionLocation"]["scriptId"].Value<string>()
            });

            var source = await cli.SendCommand("Debugger.getScriptSource", sourceToGet, token);
            Assert.False(source.Value["scriptSource"].Value<string>().Contains("// Unable to read document"));
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task InspectTaskAtLocals() => await CheckInspectLocalsAtBreakpointSite(
            "InspectTask",
            "RunInspectTask",
            10,
            "InspectTask.RunInspectTask.AnonymousMethod__0" ,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] InspectTask:RunInspectTask'); }}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                CheckNumber(locals, "a", 10);

                var t_props = await GetObjectOnLocals(locals, "t");
                await CheckProps(t_props, new
                    {
                        Status = TGetter("Status")
                    }, "t_props", num_fields: 58);
            });


        [Fact]
        public async Task InspectLocalsWithIndexAndPositionWithDifferentValues() //https://github.com/xamarin/xamarin-android/issues/6161
        {
            await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method('[debugger-test] MainPage:CallSetValue'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 758, 16,
                "MainPage.set_SomeValue",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "view", 150);
                    await Task.CompletedTask;
                }
            );
        }

        [Fact]
        public async Task MallocUntilReallocate() //https://github.com/xamarin/xamarin-android/issues/6161
        {
            string eval_expr = "window.setTimeout(function() { malloc_to_reallocate_test (); }, 1)";

            var result = await Evaluate(eval_expr);

            var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 10, 8);

            var eval_req = JObject.FromObject(new
            {
                expression = "window.setTimeout(function() { invoke_add(); }, 1);",
            });

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

        [Fact]
        public async Task InspectLocalsUsingClassFromLibraryUsingDebugTypeFull()
        {
            var expression = $"{{ invoke_static_method('[debugger-test] DebugTypeFull:CallToEvaluateLocal'); }}";

            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 965, 8,
                "DebugTypeFull.CallToEvaluateLocal",
                wait_for_event_fn: async (pause_location) =>
                {
                    var a_props = await GetObjectOnFrame(pause_location["callFrames"][0], "a");
                    await CheckProps(a_props, new
                    {
                        a = TNumber(10),
                        b = TNumber(20),
                        c = TNumber(30)
                    }, "a");
                }
            );
        }
        //TODO add tests covering basic stepping behavior as step in/out/over

        [Theory]
        [InlineData(
            "DebuggerTests.CheckSpecialCharactersInPath",
            "dotnet://debugger-test-special-char-in-path.dll/test#.cs")]
        [InlineData(
            "DebuggerTests.CheckSNonAsciiCharactersInPath",
            "dotnet://debugger-test-special-char-in-path.dll/non-ascii-test-ął.cs")]
        public async Task SetBreakpointInProjectWithSpecialCharactersInPath(
            string classWithNamespace, string expectedFileLocation)
        {
            var bp = await SetBreakpointInMethod("debugger-test-special-char-in-path.dll", classWithNamespace, "Evaluate", 1);
            await EvaluateAndCheck(
                $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test-special-char-in-path] {classWithNamespace}:Evaluate'); }}, 1);",
                expectedFileLocation,
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                $"{classWithNamespace}.Evaluate");
        }

        [Theory]
        [InlineData(
            "DebugWithDeletedPdb",
            1146)]
        [InlineData(
            "DebugWithoutDebugSymbols",
            1158)]
        public async Task InspectPropertiesOfObjectFromExternalLibrary(string className, int line)
        {
            var expression = $"{{ invoke_static_method('[debugger-test] {className}:Run'); }}";

            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", line, 8,
                $"{className}.Run",
                wait_for_event_fn: async (pause_location) =>
                {
                    var exc_props = await GetObjectOnFrame(pause_location["callFrames"][0], "exc");
                    await CheckProps(exc_props, new
                    {
                        propA = TNumber(10),
                        propB = TNumber(20),
                        propC = TNumber(30),
                        d = TNumber(40)
                    }, "exc");
                }
            );
        }

        [Theory]
        [InlineData("TestAsyncGeneric1Parm", "AsyncGeneric.GetAsyncMethod<int>")]
        [InlineData("TestKlassGenericAsyncGeneric", "AsyncGeneric.MyKlass<bool, char>.GetAsyncMethod<int>")]
        [InlineData("TestKlassGenericAsyncGeneric2", "AsyncGeneric.MyKlass<bool>.GetAsyncMethod<int>")]
        [InlineData("TestKlassGenericAsyncGeneric3", "AsyncGeneric.MyKlass<bool>.GetAsyncMethod2<int, char>")]
        [InlineData("TestKlassGenericAsyncGeneric4", "AsyncGeneric.MyKlass<bool, double>.GetAsyncMethod2<int, char>")]
        [InlineData("TestKlassGenericAsyncGeneric5", "AsyncGeneric.MyKlass<bool>.MyKlassNested<int>.GetAsyncMethod<char>")]
        [InlineData("TestKlassGenericAsyncGeneric6", "AsyncGeneric.MyKlass<AsyncGeneric.MyKlass<int>>.GetAsyncMethod<char>")]
        public async Task CheckCallStackOfAsyncGenericMethods(string method_name, string method_name_call_stack)
        {
            var expression = $"{{ invoke_static_method_async ('[debugger-test] AsyncGeneric:{method_name}'); }}";

            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", -1, -1,
                method_name_call_stack
            );
        }

        [Fact]
        public async Task InspectLocalRecursiveFieldValue()
        {
            var expression = $"{{ invoke_static_method('[debugger-test] InspectIntPtr:Run'); }}";

            await EvaluateAndCheck(
                "window.setTimeout(function() {" + expression + "; }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 1256, 8,
                $"InspectIntPtr.Run",
                locals_fn: async (locals) =>
                {
                    await CheckValueType(locals, "myInt", "System.IntPtr");
                    await CheckValueType(locals, "myInt2", "System.IntPtr");
                }
            );
        }
    }
}
