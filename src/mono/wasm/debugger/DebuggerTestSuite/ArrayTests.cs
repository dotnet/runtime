// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.WebAssembly.Diagnostics;
using Xunit;

namespace DebuggerTests
{

    public class ArrayTests : DebuggerTestBase
    {

        [Theory]
        [InlineData(19, 8, "PrimitiveTypeLocals", false, 0, false)]
        [InlineData(19, 8, "PrimitiveTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectPrimitiveTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:PrimitiveTypeLocals",
            method_name : method_name,
            etype_name: "int",
            local_var_name_prefix: "int",
            array : new [] { TNumber(4), TNumber(70), TNumber(1) },
            array_elements : null,
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        [Theory]
        [InlineData(36, 8, "ValueTypeLocals", false, 0, false)]
        [InlineData(36, 8, "ValueTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectValueTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocals",
            method_name : method_name,
            etype_name: "DebuggerTests.Point",
            local_var_name_prefix: "point",
            array : new []
            {
                TValueType("DebuggerTests.Point"),
                    TValueType("DebuggerTests.Point"),
            },
            array_elements : new []
            {
                TPoint(5, -2, "point_arr#Id#0", "Green"),
                    TPoint(123, 0, "point_arr#Id#1", "Blue")
            },
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        [Theory]
        [InlineData(54, 8, "ObjectTypeLocals", false, 0, false)]
        [InlineData(54, 8, "ObjectTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectObjectArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:ObjectTypeLocals",
            method_name : method_name,
            etype_name: "DebuggerTests.SimpleClass",
            local_var_name_prefix: "class",
            array : new []
            {
                TObject("DebuggerTests.SimpleClass"),
                    TObject("DebuggerTests.SimpleClass", is_null : true),
                    TObject("DebuggerTests.SimpleClass")
            },
            array_elements : new []
            {
                TSimpleClass(5, -2, "class_arr#Id#0", "Green"),
                    null, // Element is null
                    TSimpleClass(123, 0, "class_arr#Id#2", "Blue")
            },
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        [Theory]
        [InlineData(72, 8, "GenericTypeLocals", false, 0, false)]
        [InlineData(72, 8, "GenericTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericTypeLocals",
            method_name : method_name,
            etype_name: "DebuggerTests.GenericClass<int>",
            local_var_name_prefix: "gclass",
            array : new []
            {
                TObject("DebuggerTests.GenericClass<int>", is_null : true),
                    TObject("DebuggerTests.GenericClass<int>"),
                    TObject("DebuggerTests.GenericClass<int>")
            },
            array_elements : new []
            {
                null, // Element is null
                new
                {
                    Id = TString("gclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = TNumber(5)
                },
                new
                {
                    Id = TString("gclass_arr#2#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = TNumber(-12)
                }
            },
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        [Theory]
        [InlineData(89, 8, "GenericValueTypeLocals", false, 0, false)]
        [InlineData(89, 8, "GenericValueTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericValueTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericValueTypeLocals",
            method_name : method_name,
            etype_name: "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>",
            local_var_name_prefix: "gvclass",
            array : new []
            {
                TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                    TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>")
            },
            array_elements : new []
            {
                new
                {
                    Id = TString("gvclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                },
                new
                {
                    Id = TString("gvclass_arr#2#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = TPoint(10, 20, "gvclass_arr#2#Value#Id", "Green")
                }
            },
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        [Theory]
        [InlineData(213, 8, "GenericValueTypeLocals2", false, 0, false)]
        [InlineData(213, 8, "GenericValueTypeLocals2", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericValueTypeArrayLocals2(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericValueTypeLocals2",
            method_name : method_name,
            etype_name: "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>",
            local_var_name_prefix: "gvclass",
            array : new []
            {
                TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>"),
                    TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>")
            },
            array_elements : new []
            {
                new
                {
                    Id = TString("gvclass_arr#0#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = new []
                        {
                            TPoint(100, 200, "gvclass_arr#0#0#Value#Id", "Red"),
                                TPoint(100, 200, "gvclass_arr#0#1#Value#Id", "Green")
                        }
                },
                new
                {
                    Id = TString("gvclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = new []
                        {
                            TPoint(100, 200, "gvclass_arr#1#0#Value#Id", "Green"),
                                TPoint(100, 200, "gvclass_arr#1#1#Value#Id", "Blue")
                        }
                }
            },
            test_prev_frame : test_prev_frame,
            frame_idx : frame_idx,
            use_cfo : use_cfo);

        async Task TestSimpleArrayLocals(int line, int col, string entry_method_name, string method_name, string etype_name,
            string local_var_name_prefix, object[] array, object[] array_elements,
            bool test_prev_frame = false, int frame_idx = 0, bool use_cfo = false)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                    $"'{entry_method_name}', { (test_prev_frame ? "true" : "false") }" +
                    "); }, 1);";

                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);

                var locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                Assert.Equal(4, locals.Count());
                CheckArray(locals, $"{local_var_name_prefix}_arr", $"{etype_name}[]");
                CheckArray(locals, $"{local_var_name_prefix}_arr_empty", $"{etype_name}[]");
                CheckObject(locals, $"{local_var_name_prefix}_arr_null", $"{etype_name}[]", is_null : true);
                CheckBool(locals, "call_other", test_prev_frame);

                var local_arr_name = $"{local_var_name_prefix}_arr";

                JToken prefix_arr;
                if (use_cfo)
                { // Use `Runtime.callFunctionOn` to get the properties
                    var frame = pause_location["callFrames"][frame_idx];
                    var name = local_arr_name;
                    var fl = await GetProperties(frame["callFrameId"].Value<string>());
                    var l_obj = GetAndAssertObjectWithName(locals, name);
                    var l_objectId = l_obj["value"]["objectId"]?.Value<string>();

                    Assert.True(!String.IsNullOrEmpty(l_objectId), $"No objectId found for {name}");

                    prefix_arr = await GetObjectWithCFO(l_objectId);
                }
                else
                {
                    prefix_arr = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], local_arr_name);
                }

                await CheckProps(prefix_arr, array, local_arr_name);

                if (array_elements?.Length > 0)
                {
                    for (int i = 0; i < array_elements.Length; i++)
                    {
                        var i_str = i.ToString();
                        var label = $"{local_var_name_prefix}_arr[{i}]";
                        if (array_elements[i] == null)
                        {
                            var act_i = prefix_arr.FirstOrDefault(jt => jt["name"]?.Value<string>() == i_str);
                            Assert.True(act_i != null, $"[{label}] Couldn't find array element [{i_str}]");

                            await CheckValue(act_i["value"], TObject(etype_name, is_null : true), label);
                        }
                        else
                        {
                            await CompareObjectPropertiesFor(prefix_arr, i_str, array_elements[i], label : label);
                        }
                    }
                }

                var props = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], $"{local_var_name_prefix}_arr_empty");
                await CheckProps(props, new object[0], "${local_var_name_prefix}_arr_empty");
            });

            async Task<JToken> GetObjectWithCFO(string objectId, JObject fn_args = null)
            {
                var fn_decl = "function () { return this; }";
                var cfo_args = JObject.FromObject(new
                {
                    functionDeclaration = fn_decl,
                        objectId = objectId
                });

                if (fn_args != null)
                    cfo_args["arguments"] = fn_args;

                // callFunctionOn
                var result = await ctx.cli.SendCommand("Runtime.callFunctionOn", cfo_args, ctx.token);

                return await GetProperties(result.Value["result"]["objectId"]?.Value<string>(), fn_args);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectObjectArrayMembers(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            int line = 227;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers";
            string method_name = "PlaceholderMethod";
            int frame_idx = 1;

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                    $"'{entry_method_name}'" +
                    "); }, 1);";

                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);
                var locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                Assert.Single(locals);
                CheckObject(locals, "c", "DebuggerTests.Container");

                var c_props = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], "c");
                await CheckProps(c_props, new
                    {
                        id = TString("c#id"),
                            ClassArrayProperty = TArray("DebuggerTests.SimpleClass[]", 3),
                            ClassArrayField = TArray("DebuggerTests.SimpleClass[]", 3),
                            PointsProperty = TArray("DebuggerTests.Point[]", 2),
                            PointsField = TArray("DebuggerTests.Point[]", 2)
                    },
                    "c"
                );

                await CompareObjectPropertiesFor(c_props, "ClassArrayProperty",
                    new []
                    {
                        TSimpleClass(5, -2, "ClassArrayProperty#Id#0", "Green"),
                            TSimpleClass(30, 1293, "ClassArrayProperty#Id#1", "Green"),
                            TObject("DebuggerTests.SimpleClass", is_null : true)
                    },
                    label: "InspectLocalsWithStructsStaticAsync");

                await CompareObjectPropertiesFor(c_props, "ClassArrayField",
                    new []
                    {
                        TObject("DebuggerTests.SimpleClass", is_null : true),
                            TSimpleClass(5, -2, "ClassArrayField#Id#1", "Blue"),
                            TSimpleClass(30, 1293, "ClassArrayField#Id#2", "Green")
                    },
                    label: "c#ClassArrayField");

                await CompareObjectPropertiesFor(c_props, "PointsProperty",
                    new []
                    {
                        TPoint(5, -2, "PointsProperty#Id#0", "Green"),
                            TPoint(123, 0, "PointsProperty#Id#1", "Blue"),
                    },
                    label: "c#PointsProperty");

                await CompareObjectPropertiesFor(c_props, "PointsField",
                    new []
                    {
                        TPoint(5, -2, "PointsField#Id#0", "Green"),
                            TPoint(123, 0, "PointsField#Id#1", "Blue"),
                    },
                    label: "c#PointsField");
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsStaticAsync(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            int line = 157;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocalsAsync";
            string method_name = "MoveNext"; // BUG: this should be ValueTypeLocalsAsync
            int frame_idx = 0;

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                    $"'{entry_method_name}', false" // *false* here keeps us only in the static method
                    +
                    "); }, 1);";

                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);
                var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    call_other = TBool(false),
                        gvclass_arr = TArray("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]", 2),
                        gvclass_arr_empty = TArray("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]"),
                        gvclass_arr_null = TObject("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]", is_null : true),
                        gvclass = TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                        // BUG: this shouldn't be null!
                        points = TObject("DebuggerTests.Point[]", is_null : true)
                }, "ValueTypeLocalsAsync#locals");

                var local_var_name_prefix = "gvclass";
                await CompareObjectPropertiesFor(frame_locals, local_var_name_prefix, new
                {
                    Id = TString(null),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = TPoint(0, 0, null, "Red")
                });

                await CompareObjectPropertiesFor(frame_locals, $"{local_var_name_prefix}_arr",
                    new []
                    {
                        new
                        {
                            Id = TString("gvclass_arr#1#Id"),
                                Color = TEnum("DebuggerTests.RGB", "Red"),
                                Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                        },
                        new
                        {
                            Id = TString("gvclass_arr#2#Id"),
                                Color = TEnum("DebuggerTests.RGB", "Blue"),
                                Value = TPoint(10, 20, "gvclass_arr#2#Value#Id", "Green")
                        }
                    }
                );
                await CompareObjectPropertiesFor(frame_locals, $"{local_var_name_prefix}_arr_empty",
                    new object[0]);
            });
        }

        // TODO: Check previous frame too
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInstanceAsync(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            int line = 170;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocalsAsync";
            int frame_idx = 0;

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                    $"'{entry_method_name}', true" +
                    "); }, 1);";

                // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

                var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    t1 = TObject("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                        @this = TObject("DebuggerTests.ArrayTestsClass"),
                        point_arr = TArray("DebuggerTests.Point[]", 2),
                        point = TValueType("DebuggerTests.Point")
                }, "InspectValueTypeArrayLocalsInstanceAsync#locals");

                await CompareObjectPropertiesFor(frame_locals, "t1",
                    new
                    {
                        Id = TString("gvclass_arr#1#Id"),
                            Color = TEnum("DebuggerTests.RGB", "Red"),
                            Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                    });

                await CompareObjectPropertiesFor(frame_locals, "point_arr",
                    new []
                    {
                        TPoint(5, -2, "point_arr#Id#0", "Red"),
                            TPoint(123, 0, "point_arr#Id#1", "Blue"),
                    }
                );

                await CompareObjectPropertiesFor(frame_locals, "point",
                    TPoint(45, 51, "point#Id", "Green"));
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInAsyncStaticStructMethod(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            int line = 244;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod";
            int frame_idx = 0;

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);
                //await SetBreakpoint (debugger_test_loc, 143, 3);

                var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                    $"'{entry_method_name}', false" +
                    "); }, 1);";

                // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

                var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
                await CheckProps(frame_locals, new
                {
                    call_other = TBool(false),
                        local_i = TNumber(5),
                        sc = TSimpleClass(10, 45, "sc#Id", "Blue")
                }, "InspectValueTypeArrayLocalsInAsyncStaticStructMethod#locals");
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInAsyncInstanceStructMethod(bool use_cfo)
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);
            int line = 251;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod";
            int frame_idx = 0;

            await Ready();
            await insp.Ready(async(cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
                var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

                await SetBreakpoint(debugger_test_loc, line, col);

                var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                    $"'{entry_method_name}', true" +
                    "); }, 1);";

                // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
                var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

                var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
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
        }

    }
}